# J2P

## Integrants
- Javier Aguilera Pérez

## Nom del projecte
**J2P**

## Descripció
J2P és un videojoc 2D **top-down arena shooter** desenvolupat amb **Unity** com a client i **Node.js + Express + WebSocket + MongoDB** com a backend. El jugador controla un tanc en una arena amb obstacles, bots i altres jugadors, amb moviment twin-stick, sistema de vida, trets, recàrrega, puntuació, powerups i menús de login.

El projecte integra:
- client 2D jugable amb Unity
- backend propi per autenticació, estadístiques i multijugador
- bots locals amb IA per regles
- base preparada per **Unity ML-Agents**
- treball guiat per especificació amb **OpenSpec**

## Gestor de tasques
- [Taiga del projecte](https://tree.taiga.io/project/javaguper-j/backlog)

## Estat actual del projecte
Actualment el projecte ja disposa d’una base funcional jugable i no es troba només en fase inicial. A dia d’avui inclou:

- menú principal amb login i registre
- partida local de supervivència
- mode online PvP connectat a backend
- bots de suport quan no hi ha prou jugadors online
- HUD, marcador, game over i sistema de puntuació
- powerups al mapa (`Heal`, `Speed Boost`, `Rapid Fire`)
- escena d’entrenament separada per ML-Agents
- backend amb persistència d’usuaris i estadístiques

## Funcionalitats principals

### Client Unity
- moviment del jugador amb `WASD`
- apuntat amb ratolí i tret en temps real
- recàrrega automàtica
- vida, morts, respawn i game over
- obstacles amb col·lisions 2D
- noms sobre jugadors i bots
- top 5 en partida
- powerups de mapa
- menú principal, pausa i pantalla final

### Multijugador i backend
- registre i login amb usuari i contrasenya
- autenticació amb JWT
- sincronització online per WebSocket
- leaderboard online
- persistència d’estadístiques amb MongoDB

### IA
- bots locals amb IA basada en regles
- detecció per raycasts
- agent preparat per **Unity ML-Agents**
- escena d’entrenament independent

## Tecnologies utilitzades
- **Unity 6**
- **C#**
- **Node.js**
- **Express**
- **WebSocket (`ws`)**
- **MongoDB + Mongoose**
- **Unity ML-Agents**
- **OpenSpec**

## Estructura del repositori

```text
/
├── README.md
├── J2P/                         # Projecte Unity
│   ├── Assets/
│   ├── Builds/
│   ├── ML-Agents/
│   ├── Packages/
│   └── ProjectSettings/
├── backend/                     # Backend Node.js
│   ├── deploy/
│   ├── src/
│   └── package.json
├── docs/                        # Documentació d'entrega i traçabilitat
├── doc/                         # Documentació auxiliar
├── openspec/                    # Especificacions OpenSpec
└── specs/                       # Especificacions adaptades a l'entrega
```

## Estructura principal del client

```text
J2P/Assets/Scripts/
├── AI/
├── Bootstrap/
├── Combat/
├── Core/
├── Gameplay/
└── Networking/
```

## Escenes principals
- `J2P/Assets/Scenes/MainMenu.unity`: menú principal, login i accés als modes de joc
- `J2P/Assets/Game_v0.1.unity`: escena principal jugable
- `J2P/Assets/Scenes/SampleScene.unity`: escena de joc alternativa
- `J2P/Assets/Scenes/TrainingArena.unity`: escena d’entrenament per ML-Agents

## Execució del projecte

### Client Unity
1. Obrir el projecte `J2P` amb Unity.
2. Carregar `J2P/Assets/Scenes/MainMenu.unity`.
3. Executar des de l’editor o generar una build.

### Backend
Des de la carpeta `backend/`:

```bash
npm install
npm start
```

Per desenvolupament:

```bash
npm run dev
```

## OpenSpec
La funcionalitat de powerups s’ha treballat amb desenvolupament guiat per especificació mitjançant OpenSpec. La change principal es troba a:

```text
openspec/changes/add-map-powerups/
```

I la documentació adaptada per a l’entrega es troba a:

```text
specs/powerups-map/
docs/prompts-log.md
```

## Estat de lliurament
El repositori inclou el client Unity, el backend Node.js, la configuració d’OpenSpec, la documentació de suport i una build Linux del joc dins de `J2P/Builds/`.
