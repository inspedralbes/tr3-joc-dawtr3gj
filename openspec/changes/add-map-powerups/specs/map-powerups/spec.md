## ADDED Requirements

### Requirement: Powerup spawn cycle
The system SHALL attempt to spawn map powerups automatically every 12 seconds during gameplay while respecting a maximum of 3 active powerups at the same time.

#### Scenario: Spawn occurs when capacity is available
- **WHEN** 12 seconds have elapsed and fewer than 3 powerups are currently active
- **THEN** the system spawns one new powerup on the map

#### Scenario: Spawn is skipped at active limit
- **WHEN** 12 seconds have elapsed and 3 powerups are already active
- **THEN** the system does not spawn an additional powerup in that cycle

### Requirement: Powerup spawn positions must be valid
The system SHALL only spawn powerups in valid map positions that are outside walls, boundaries, and blocking obstacles.

#### Scenario: Valid position is accepted
- **WHEN** the spawn system finds a candidate position that does not overlap blocked geometry and lies within the playable area
- **THEN** the powerup is instantiated at that position

#### Scenario: Invalid position is rejected
- **WHEN** a candidate position overlaps a wall, boundary, or obstacle
- **THEN** the system rejects that position and does not place a powerup there

### Requirement: Only the player can collect map powerups
The system SHALL allow only the player entity to collect spawned powerups in this version.

#### Scenario: Player collects a powerup
- **WHEN** the player collides with an active powerup
- **THEN** the powerup is consumed and its effect is applied

#### Scenario: Non-player entity touches a powerup
- **WHEN** a non-player entity collides with an active powerup
- **THEN** the powerup remains active and no effect is applied

### Requirement: Heal powerup restores health immediately
The `Heal` powerup SHALL restore 25 health points instantly without allowing the player's current health to exceed maximum health.

#### Scenario: Heal restores missing health
- **WHEN** the player with missing health collects a `Heal` powerup
- **THEN** the player's health increases by 25 or until reaching maximum health, whichever comes first

#### Scenario: Heal at full health
- **WHEN** the player at maximum health collects a `Heal` powerup
- **THEN** the player's health remains at maximum health

### Requirement: Speed Boost temporarily increases movement speed
The `Speed Boost` powerup SHALL increase the player's movement speed by 35% for 6 seconds.

#### Scenario: Speed boost is activated
- **WHEN** the player collects a `Speed Boost` powerup with no active speed boost
- **THEN** the player's movement speed is multiplied by 1.35 for 6 seconds

#### Scenario: Speed boost expires
- **WHEN** 6 seconds pass after the most recent `Speed Boost` activation
- **THEN** the player's movement speed returns to its base value

### Requirement: Rapid Fire temporarily reduces shot cooldown
The `Rapid Fire` powerup SHALL reduce the player's shot cooldown by 35% for 6 seconds.

#### Scenario: Rapid fire is activated
- **WHEN** the player collects a `Rapid Fire` powerup with no active rapid fire effect
- **THEN** the player's shot cooldown is multiplied by 0.65 for 6 seconds

#### Scenario: Rapid fire expires
- **WHEN** 6 seconds pass after the most recent `Rapid Fire` activation
- **THEN** the player's shot cooldown returns to its base value

### Requirement: Re-collecting a temporary powerup refreshes its duration
The system SHALL refresh the remaining duration of a temporary powerup effect when the player collects another powerup of the same type while that effect is active, instead of stacking its duration or intensity.

#### Scenario: Active speed boost is refreshed
- **WHEN** the player collects a second `Speed Boost` while a `Speed Boost` effect is already active
- **THEN** the effect duration is reset to 6 seconds from the new pickup time and the movement multiplier remains 1.35

#### Scenario: Active rapid fire is refreshed
- **WHEN** the player collects a second `Rapid Fire` while a `Rapid Fire` effect is already active
- **THEN** the effect duration is reset to 6 seconds from the new pickup time and the cooldown multiplier remains 0.65
