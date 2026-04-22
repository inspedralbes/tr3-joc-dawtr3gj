## Why

El juego no ofrece actualmente objetos coleccionables dinámicos en el mapa, lo que reduce la variedad táctica durante la partida. Añadir powerups recogibles introduce decisiones de posicionamiento y control del espacio sin alterar los sistemas base del combate.

## What Changes

- Añadir generación automática de powerups coleccionables en posiciones válidas del mapa con un intervalo fijo de 12 segundos.
- Limitar a 3 el número de powerups activos simultáneamente y evitar apariciones dentro de muros, límites u obstáculos.
- Incorporar tres tipos de powerup para el jugador: `Heal`, `Speed Boost` y `Rapid Fire`.
- Hacer que los efectos temporales duren 6 segundos y que, al recoger uno del mismo tipo, su duración se reinicie en lugar de acumularse.
- Mantener una presentación visual simple y una arquitectura modular preparada para ampliar tipos de powerup o receptores en el futuro.

## Capabilities

### New Capabilities
- `map-powerups`: Gestiona la aparición, representación, recogida y aplicación de powerups coleccionables dentro del mapa de juego.

### Modified Capabilities

Ninguna.

## Impact

- Código de gameplay relacionado con spawn de objetos en mapa, detección de recogida por el jugador y aplicación de efectos temporales.
- Posibles prefabs, sprites simples y configuración de escena o managers dedicados a powerups.
- Sin cambios previstos en APIs externas ni dependencias nuevas.
