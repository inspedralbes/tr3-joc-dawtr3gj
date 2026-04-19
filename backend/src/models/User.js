const mongoose = require("mongoose");

const userSchema = new mongoose.Schema(
  {
    username: {
      type: String,
      required: true,
      unique: true,
      trim: true,
      minlength: 3,
      maxlength: 24,
    },
    passwordHash: {
      type: String,
      required: true,
    },
    lastLoginAt: {
      type: Date,
      default: null,
    },
    stats: {
      matchesPlayed: {
        type: Number,
        default: 0,
      },
      totalKills: {
        type: Number,
        default: 0,
      },
      bestKillsInMatch: {
        type: Number,
        default: 0,
      },
      lastKills: {
        type: Number,
        default: 0,
      },
      bestScore: {
        type: Number,
        default: 0,
      },
      lastScore: {
        type: Number,
        default: 0,
      },
      totalPlayTime: {
        type: Number,
        default: 0,
      },
      bestSurvivalTime: {
        type: Number,
        default: 0,
      },
      lastSurvivalTime: {
        type: Number,
        default: 0,
      },
    },
  },
  {
    timestamps: true,
  }
);

module.exports = mongoose.model("User", userSchema);
