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
      },
    });
  } catch (error) {
    console.error("me error", error);
    res.status(500).json({ message: "No se pudo recuperar la sesion." });
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
