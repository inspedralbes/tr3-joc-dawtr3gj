# J2P

## Integrants
- Javier Aguilera Pérez

## Nom del projecte
**J2P**

## Descripció
J2P és un projecte transversal de videojoc 2D multijugador desenvolupat amb **Unity** com a client i **Node.js** com a backend. El joc es planteja com una arena **top-down** on hi poden participar jugadors reals i agents controlats per IA.

El projecte inclou comunicació **client-servidor**, funcionalitat **multijugador**, persistència de dades i una part d’**intel·ligència artificial** amb **Unity ML-Agents**, començant per una escena d’entrenament simple i evolucionant cap a un entorn més complex amb obstacles i detecció mitjançant raycasts.

## Gestor de tasques
- [Taiga del projecte](https://tree.taiga.io/project/javaguper-j/backlog)

## Prototip gràfic
- Pendent d’afegir

## URL de producció
- Pendent de desplegament

## Estat del projecte
Actualment el projecte es troba en fase inicial de desenvolupament. Ja s’ha creat l’estructura base del repositori, el projecte de Unity integrat amb GitHub i el gestor de tasques a Taiga.

També s’ha començat a definir el backlog amb user stories i s’ha desenvolupat la base de l’entrenament de la IA amb Unity ML-Agents. Aquesta primera fase inclou una escena simple d’entrenament, moviment de l’agent, observacions bàsiques, accions de moviment, validació manual i primers passos d’entrenament i inferència.

Com a següent pas, es treballarà en la millora de l’entorn d’entrenament amb un mapa més gran, obstacles i percepció mitjançant raycasts, per acostar el comportament de la IA al joc final.

## Estructura mínima del projecte
Aquesta és l’estructura mínima de carpetes del projecte transversal, que es podrà ampliar segons les necessitats del desenvolupament:

```text
/
├── README.md
├── docs/
├── client/
├── server/
├── assets/
└── specs/
