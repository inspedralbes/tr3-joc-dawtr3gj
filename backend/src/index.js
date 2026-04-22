const http = require("http");
const express = require("express");
const cors = require("cors");
const mongoose = require("mongoose");
const { createGameServer } = require("./gameServer");
const User = require("./models/User");
const { port, mongoUri, corsOrigin } = require("./config");
const {
  validateCredentials,
  hashPassword,
  verifyPassword,
  signToken,
  verifyToken,
} = require("./auth");

const app = express();
app.use(cors({ origin: corsOrigin === "*" ? true : corsOrigin.split(",") }));
app.use(express.json());

function mapStats(stats = {}) {
  return {
    matchesPlayed: Number(stats.matchesPlayed || 0),
    totalKills: Number(stats.totalKills || 0),
    bestKillsInMatch: Number(stats.bestKillsInMatch || 0),
    lastKills: Number(stats.lastKills || 0),
    bestScore: Number(stats.bestScore || 0),
    lastScore: Number(stats.lastScore || 0),
    totalPlayTime: Number(stats.totalPlayTime || 0),
    bestSurvivalTime: Number(stats.bestSurvivalTime || 0),
    lastSurvivalTime: Number(stats.lastSurvivalTime || 0),
  };
}

function authMiddleware(req, res, next) {
  const authorization = req.headers.authorization || "";
  const token = authorization.startsWith("Bearer ") ? authorization.slice(7) : "";

  if (!token) {
    return res.status(401).json({ message: "Falta token." });
  }

  try {
    req.auth = verifyToken(token);
    next();
  } catch (error) {
    return res.status(401).json({ message: "Token invalido o expirado." });
  }
}

app.get("/api/health", (_req, res) => {
  res.json({ ok: true, timestamp: Date.now() });
});

app.post("/api/auth/register", async (req, res) => {
  try {
    const { username, password } = req.body || {};
    const validation = validateCredentials(username, password);

    if (!validation.ok) {
      return res.status(400).json({ message: validation.message });
    }

    const existing = await User.findOne({ username: validation.username }).lean();

    if (existing) {
      return res.status(409).json({ message: "Ese usuario ya existe." });
    }

    const passwordHash = await hashPassword(password);
    const user = await User.create({
      username: validation.username,
      passwordHash,
      lastLoginAt: new Date(),
    });

    const token = signToken(user);

    res.status(201).json({
      token,
      user: {
        id: String(user._id),
        username: user.username,
      },
    });
  } catch (error) {
    console.error("register error", error);
    res.status(500).json({ message: "No se pudo registrar el usuario." });
  }
});

app.post("/api/auth/login", async (req, res) => {
  try {
    const { username, password } = req.body || {};
    const validation = validateCredentials(username, password);

    if (!validation.ok) {
      return res.status(400).json({ message: validation.message });
    }

    const user = await User.findOne({ username: validation.username });

    if (!user) {
      return res.status(401).json({ message: "Credenciales incorrectas." });
    }

    const passwordOk = await verifyPassword(password, user.passwordHash);

    if (!passwordOk) {
      return res.status(401).json({ message: "Credenciales incorrectas." });
    }

    user.lastLoginAt = new Date();
    await user.save();

    const token = signToken(user);

    res.json({
      token,
      user: {
        id: String(user._id),
        username: user.username,
      },
    });
  } catch (error) {
    console.error("login error", error);
    res.status(500).json({ message: "No se pudo iniciar sesion." });
  }
});

app.get("/api/auth/me", authMiddleware, async (req, res) => {
  try {
    const user = await User.findById(req.auth.sub).lean();

    if (!user) {
      return res.status(404).json({ message: "Usuario no encontrado." });
    }

    res.json({
      user: {
        id: String(user._id),
        username: user.username,
        createdAt: user.createdAt,
        lastLoginAt: user.lastLoginAt,
        stats: mapStats(user.stats),
      },
    });
  } catch (error) {
    console.error("me error", error);
    res.status(500).json({ message: "No se pudo recuperar la sesion." });
  }
});

app.get("/api/stats/leaderboard", async (_req, res) => {
  try {
    const users = await User.find({}, { username: 1, stats: 1 })
      .sort({
        "stats.bestScore": -1,
        "stats.bestKillsInMatch": -1,
        "stats.bestSurvivalTime": -1,
      })
      .limit(10)
      .lean();

    res.json({
      leaderboard: users.map((user) => ({
        userId: String(user._id),
        username: user.username,
        stats: mapStats(user.stats),
      })),
    });
  } catch (error) {
    console.error("leaderboard error", error);
    res.status(500).json({ message: "No se pudo recuperar el ranking." });
  }
});

app.get("/api/stats/me", authMiddleware, async (req, res) => {
  try {
    const user = await User.findById(req.auth.sub, { username: 1, stats: 1 }).lean();

    if (!user) {
      return res.status(404).json({ message: "Usuario no encontrado." });
    }

    res.json({
      user: {
        id: String(user._id),
        username: user.username,
        stats: mapStats(user.stats),
      },
    });
  } catch (error) {
    console.error("stats me error", error);
    res.status(500).json({ message: "No se pudieron recuperar las estadisticas." });
  }
});

app.post("/api/stats/match", authMiddleware, async (req, res) => {
  try {
    const score = Math.max(0, Number(req.body?.score || 0));
    const kills = Math.max(0, Number(req.body?.kills || 0));
    const survivalTime = Math.max(0, Number(req.body?.survivalTime || 0));

    const user = await User.findById(req.auth.sub);

    if (!user) {
      return res.status(404).json({ message: "Usuario no encontrado." });
    }

    user.stats.matchesPlayed += 1;
    user.stats.totalKills += kills;
    user.stats.lastKills = kills;
    user.stats.bestKillsInMatch = Math.max(user.stats.bestKillsInMatch, kills);
    user.stats.lastScore = score;
    user.stats.bestScore = Math.max(user.stats.bestScore, score);
    user.stats.lastSurvivalTime = survivalTime;
    user.stats.bestSurvivalTime = Math.max(user.stats.bestSurvivalTime, survivalTime);
    user.stats.totalPlayTime += survivalTime;

    await user.save();

    res.json({
      ok: true,
      stats: mapStats(user.stats),
    });
  } catch (error) {
    console.error("match stats error", error);
    res.status(500).json({ message: "No se pudieron guardar las estadisticas." });
  }
});

async function start() {
  if (!mongoUri) {
    throw new Error("MONGO_URI no configurado.");
  }

  await mongoose.connect(mongoUri);
  const server = http.createServer(app);
  createGameServer(server);

  server.listen(port, () => {
    console.log(`Backend listening on :${port}`);
  });
}

start().catch((error) => {
  console.error("fatal startup error", error);
  process.exit(1);
});
