const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const { jwtSecret, jwtExpiresIn } = require("./config");

function normalizeUsername(username) {
  return typeof username === "string" ? username.trim() : "";
}

function validateCredentials(username, password) {
  const normalized = normalizeUsername(username);

  if (normalized.length < 3 || normalized.length > 24) {
    return { ok: false, message: "El nombre debe tener entre 3 y 24 caracteres." };
  }

  if (!/^[a-zA-Z0-9_]+$/.test(normalized)) {
    return { ok: false, message: "El nombre solo puede contener letras, numeros y guion bajo." };
  }

  if (typeof password !== "string" || password.length < 6 || password.length > 72) {
    return { ok: false, message: "La contrasena debe tener entre 6 y 72 caracteres." };
  }

  return { ok: true, username: normalized };
}

async function hashPassword(password) {
  return bcrypt.hash(password, 12);
}

async function verifyPassword(password, hash) {
  return bcrypt.compare(password, hash);
}

function signToken(user) {
  return jwt.sign(
    {
      sub: String(user._id),
      username: user.username,
    },
    jwtSecret,
    { expiresIn: jwtExpiresIn }
  );
}

function verifyToken(token) {
  return jwt.verify(token, jwtSecret);
}

module.exports = {
  normalizeUsername,
  validateCredentials,
  hashPassword,
  verifyPassword,
  signToken,
  verifyToken,
};
