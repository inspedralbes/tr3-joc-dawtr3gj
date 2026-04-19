const crypto = require("crypto");
const { WebSocketServer } = require("ws");
const url = require("url");
const { verifyToken } = require("./auth");
const { defaultPlayerHp } = require("./config");

function createGameServer(server) {
  const wss = new WebSocketServer({ noServer: true });
  const players = new Map();

  server.on("upgrade", (request, socket, head) => {
    const parsed = url.parse(request.url, true);

    if (parsed.pathname !== "/ws") {
      socket.destroy();
      return;
    }

    try {
      const token = parsed.query.token;
      const payload = verifyToken(token);
      request.user = {
        id: payload.sub,
        username: payload.username,
      };
    } catch (error) {
      socket.write("HTTP/1.1 401 Unauthorized\r\n\r\n");
      socket.destroy();
      return;
    }

    wss.handleUpgrade(request, socket, head, (ws) => {
      wss.emit("connection", ws, request);
    });
  });

  function serializePlayers() {
    return Array.from(players.values()).map(toPublicPlayer);
  }

  function toPublicPlayer(player) {
    return {
      id: player.id,
      userId: player.userId,
      username: player.username,
      x: player.x,
      y: player.y,
      bodyAngle: player.bodyAngle,
      turretAngle: player.turretAngle,
      hp: player.hp,
      maxHp: player.maxHp,
      alive: player.alive,
      kills: player.kills,
      score: player.score,
      lastUpdateAt: player.lastUpdateAt,
    };
  }

  function send(ws, type, payload) {
    if (ws.readyState !== ws.OPEN) {
      return;
    }

    ws.send(JSON.stringify({ type, payload }));
  }

  function broadcast(type, payload, excludeId = null) {
    for (const [playerId, player] of players.entries()) {
      if (excludeId !== null && playerId === excludeId) {
        continue;
      }

      send(player.socket, type, payload);
    }
  }

  wss.on("connection", (ws, request) => {
    const playerId = crypto.randomUUID();
    const player = {
      id: playerId,
      userId: request.user.id,
      username: request.user.username,
      x: 0,
      y: 0,
      bodyAngle: 0,
      turretAngle: 0,
      hp: defaultPlayerHp,
      maxHp: defaultPlayerHp,
      alive: true,
      kills: 0,
      score: 0,
      lastUpdateAt: Date.now(),
      socket: ws,
    };

    players.set(playerId, player);

    send(ws, "welcome", {
      selfId: playerId,
      players: serializePlayers(),
      serverTime: Date.now(),
    });

    broadcast("playerJoined", { player: toPublicPlayer(player) }, playerId);

    ws.on("message", (buffer) => {
      let message;

      try {
        message = JSON.parse(buffer.toString());
      } catch (_) {
        return;
      }

      handleMessage(playerId, message);
    });

    ws.on("close", () => {
      players.delete(playerId);
      broadcast("playerLeft", { playerId });
    });
  });

  function handleMessage(playerId, message) {
    const player = players.get(playerId);

    if (!player || !message || typeof message.type !== "string") {
      return;
    }

    switch (message.type) {
      case "playerState":
        applyPlayerState(player, message.payload);
        broadcast("playerState", { player: toPublicPlayer(player) }, playerId);
        break;
      case "fire":
        broadcast("fire", { playerId, ...sanitizeFire(message.payload) }, playerId);
        break;
      case "damage":
        applyDamage(player, message.payload);
        break;
      case "respawn":
        applyRespawn(player, message.payload);
        broadcast("respawn", { player: toPublicPlayer(player) }, playerId);
        break;
      default:
        break;
    }
  }

  function applyPlayerState(player, payload) {
    if (!payload) {
      return;
    }

    player.x = finiteOr(player.x, payload.x);
    player.y = finiteOr(player.y, payload.y);
    player.bodyAngle = finiteOr(player.bodyAngle, payload.bodyAngle);
    player.turretAngle = finiteOr(player.turretAngle, payload.turretAngle);
    player.hp = finiteOr(player.hp, payload.hp);
    player.maxHp = finiteOr(player.maxHp, payload.maxHp);
    player.alive = typeof payload.alive === "boolean" ? payload.alive : player.alive;
    player.lastUpdateAt = Date.now();
  }

  function sanitizeFire(payload) {
    return {
      projectileId: payload?.projectileId || crypto.randomUUID(),
      x: finiteOr(0, payload?.x),
      y: finiteOr(0, payload?.y),
      dirX: finiteOr(1, payload?.dirX),
      dirY: finiteOr(0, payload?.dirY),
      speed: finiteOr(0, payload?.speed),
      damage: finiteOr(0, payload?.damage),
      ttl: finiteOr(0, payload?.ttl),
      createdAt: Date.now(),
    };
  }

  function applyDamage(attacker, payload) {
    if (!payload || typeof payload.targetId !== "string") {
      return;
    }

    const target = players.get(payload.targetId);

    if (!target || target.id === attacker.id) {
      return;
    }

    const amount = Math.max(0, finiteOr(0, payload.amount));
    target.hp = Math.max(0, target.hp - amount);
    target.alive = target.hp > 0;

    if (!target.alive) {
      attacker.kills += 1;
      attacker.score += Math.max(1, Math.round(amount * 5));
    }

    broadcast("damage", {
      attackerId: attacker.id,
      targetId: target.id,
      amount,
      projectileId: payload.projectileId || "",
      targetHp: target.hp,
      targetAlive: target.alive,
      attackerKills: attacker.kills,
      attackerScore: attacker.score,
    });
  }

  function applyRespawn(player, payload) {
    player.x = finiteOr(player.x, payload?.x);
    player.y = finiteOr(player.y, payload?.y);
    player.hp = player.maxHp;
    player.alive = true;
    player.lastUpdateAt = Date.now();
  }

  return { wss };
}

function finiteOr(fallback, value) {
  return Number.isFinite(value) ? value : fallback;
}

module.exports = { createGameServer };
