# Tank Arena MVP

## Resumen de arquitectura

El proyecto queda montado con una arquitectura orientada a componentes reutilizables:

- `TankMovement2D` resuelve locomoción física 2D.
- `TurretAim` resuelve apuntado del cañón.
- `Weapon` y `Projectile` resuelven disparo y daño.
- `Health` y `FactionMember` resuelven vida y equipos.
- `PlayerController` consume movimiento, apuntado y arma desde input.
- `EnemyAI` consume movimiento, apuntado y arma desde reglas simples.
- `SpawnManager` gestiona spawns válidos y oleadas.
- `GameManager` resuelve flujo general, muertes, oleadas y respawn del jugador.
- `CameraFollow2D` sigue al jugador dentro de los límites.
- `RuntimeGameBootstrap` construye automáticamente la arena jugable al entrar en `SampleScene`.

La separación importante para futuras extensiones es esta:

- El jugador y los bots no implementan movimiento ni disparo directamente.
- Ambos usan los mismos componentes base (`TankMovement2D`, `TurretAim`, `Weapon`, `Health`).
- Eso permite sustituir `EnemyAI` por un agente de ML-Agents más adelante sin rehacer locomoción, proyectiles, salud, cámara o sistema de oleadas.

## Scripts creados

Ruta base: `Assets/Scripts`

- `Core/DamageInfo.cs`
- `Core/FactionMember.cs`
- `Core/Health.cs`
- `Core/ArenaBounds.cs`
- `Core/TankMovement2D.cs`
- `Core/TurretAim.cs`
- `Combat/Projectile.cs`
- `Combat/Weapon.cs`
- `Gameplay/PlayerController.cs`
- `Gameplay/SpawnManager.cs`
- `Gameplay/GameManager.cs`
- `Gameplay/CameraFollow2D.cs`
- `Gameplay/GameHud.cs`
- `AI/EnemyAI.cs`
- `Bootstrap/ProceduralSpriteLibrary.cs`
- `Bootstrap/RuntimeGameBootstrap.cs`

## Qué hace el bootstrap

Al pulsar Play en `Assets/Scenes/SampleScene.unity`, el sistema crea automáticamente:

- una arena grande de `42 x 28`
- suelo con patrón simple
- límites sólidos con colliders 2D
- múltiples obstáculos de cobertura
- jugador con cuerpo circular y cañón separado
- prefab runtime de proyectil
- prefab runtime de enemigo
- sistema de oleadas
- HUD básico
- cámara ortográfica con seguimiento
- luz global 2D

No depende de assets externos complejos ni de prefabs creados manualmente.

## Jerarquía runtime generada

La escena crea esta jerarquía al empezar:

- `TankArenaMVP`
- `Environment`
- `ArenaBounds`
- `Floor`
- `Bounds`
- `Obstacles`
- `Actors`
- `Player`
- `Enemies`
- `Projectiles`
- `Systems`
- `SpawnManager`
- `GameManager`
- `GameHud`
- `RuntimePrefabs`

## Componentes por GameObject principal

### Player

Objeto raíz `Player`:

- `Rigidbody2D`
- `CircleCollider2D`
- `FactionMember`
- `Health`
- `TankMovement2D`
- `TurretAim`
- `Weapon`
- `PlayerController`

Hijos:

- `Body` con `SpriteRenderer` circular
- `Turret`
- `Barrel` con `SpriteRenderer` rectangular
- `Muzzle` como punto de salida del proyectil

### EnemyPrefab

Objeto raíz `EnemyPrefab`:

- `Rigidbody2D`
- `CircleCollider2D`
- `FactionMember`
- `Health`
- `TankMovement2D`
- `TurretAim`
- `Weapon`
- `EnemyAI`

Hijos:

- `Body`
- `Turret`
- `Barrel`
- `Muzzle`

### ProjectilePrefab

Objeto raíz `ProjectilePrefab`:

- `SpriteRenderer`
- `Rigidbody2D`
- `CircleCollider2D` en trigger
- `Projectile`

### Walls y Obstacles

Cada pared y obstáculo usa:

- `SpriteRenderer`
- `BoxCollider2D`

## Valores iniciales usados

### Arena

- tamaño jugable: `42 x 28`
- tamaño cámara ortográfica: `10`
- grosor de muros: `1.5`
- inset seguro interior: `1.6`

### Player

- vida: `100`
- velocidad: `7.4`
- aceleración: `36`
- desaceleración: `40`
- cooldown disparo: `0.22`
- velocidad proyectil: `20`
- daño proyectil: `22`
- vida proyectil: `2.2`

### Enemy

- vida: `65`
- velocidad: `5.8`
- cooldown disparo: `0.55`
- velocidad proyectil: `17`
- daño proyectil: `14`
- detección: `26`
- rango disparo: `13.5`
- distancia preferida: `8`
- distancia de retirada: `4.8`

### Oleadas

- primera oleada: `4` bots
- incremento por oleada: `+2`
- tiempo entre oleadas: `2.5s`
- respawn jugador: `3s`
- distancia mínima de spawn respecto al jugador: `9`

## Cómo probarlo

1. Abre el proyecto en Unity 6.
2. Abre `Assets/Scenes/SampleScene.unity`.
3. Pulsa Play.

Controles:

- `WASD` mover
- ratón apuntar
- clic izquierdo mantener o pulsar para disparar

## Montaje manual recomendado si quieres dejarlo persistente en escena

Aunque el bootstrap ya lo monta todo, si después quieres pasar de runtime a escena editada, usa esta estructura:

- crea un root `TankArenaMVP`
- separa `Environment`, `Actors` y `Systems`
- deja `ArenaBounds`, `SpawnManager`, `GameManager` y `GameHud` en `Systems`
- deja `Player`, `Enemies` y `Projectiles` en `Actors`
- deja suelo, muros y cobertura en `Environment`
- mantén `TurretAim`, `TankMovement2D`, `Weapon` y `Health` en el root del actor
- mantén `Body`, `Turret`, `Barrel` y `Muzzle` como hijos visuales

## Tags y layers

El MVP no depende de tags ni layers personalizados para funcionar.

Si más adelante quieres optimizar raycasts o preparar multijugador, la recomendación es crear estas layers opcionales:

- `Actors`
- `Projectiles`
- `Obstacles`
- `SpawnBlockers`

## Preparado para ML-Agents

La ruta recomendada para evolucionar la IA es:

- conservar `TankMovement2D`, `TurretAim`, `Weapon`, `Projectile` y `Health`
- reemplazar `EnemyAI` por un agente ML
- exponer observaciones con raycasts, distancia al objetivo, cooldown, vida y cobertura visible
- mantener `SpawnManager` y `GameManager` como orquestadores del episodio

## Preparado para multijugador

La base deja separadas las responsabilidades necesarias para red futura:

- entrada desacoplada en `PlayerController`
- locomoción aislada en `TankMovement2D`
- combate aislado en `Weapon` y `Projectile`
- facciones en `FactionMember`
- reglas globales en `GameManager`

Eso facilita sustituir input local por input de red y autoridad de servidor más adelante.

## Mejoras siguientes recomendadas

- pool de proyectiles para reducir instanciación
- barra de vida sobre jugador y bots
- percepción IA por raycasts dedicados
- comportamiento de flanqueo y búsqueda de cobertura
- prefabs persistentes editados en escena en lugar de bootstrap runtime
- integración de ML-Agents
- minimapa
- pickups
- matchmaking o red local
