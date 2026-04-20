# Spec: Powerups en el mapa

## Descripción general

El sistema debe introducir powerups coleccionables que aparezcan de forma automática dentro del mapa durante la partida. Su comportamiento debe ser predecible, verificable y coherente con las reglas definidas para aparición, recogida y aplicación de efectos.

## Requisitos funcionales

### 1. Ciclo de aparición

- El sistema debe intentar generar un nuevo powerup cada 12 segundos mientras la partida esté activa.
- El sistema no debe superar nunca el límite de 3 powerups activos simultáneamente.
- Si al cumplirse el intervalo ya hay 3 powerups activos, en ese ciclo no debe generarse uno nuevo.

### 2. Validez de las posiciones de aparición

- Cada powerup debe aparecer únicamente en una posición válida del mapa.
- Una posición válida es aquella que está dentro de la zona jugable y no invade muros, límites ni obstáculos bloqueantes.
- Si una posición candidata no es válida, debe descartarse y no debe usarse para instanciar un powerup.

### 3. Recogida

- Solo el jugador puede recoger los powerups en esta versión.
- Cuando el jugador entra en contacto con un powerup activo, este debe consumirse y aplicar su efecto correspondiente.
- Si otra entidad entra en contacto con el powerup, este debe permanecer activo y no debe aplicar ningún efecto.

### 4. Tipos de powerup

#### 4.1 Heal

- Al recoger `Heal`, el jugador debe recuperar 25 puntos de vida de forma instantánea.
- La curación no puede superar la vida máxima del jugador.
- Si el jugador ya tiene la vida al máximo, el valor de vida final debe seguir siendo el máximo permitido.

#### 4.2 Speed Boost

- Al recoger `Speed Boost`, la velocidad de movimiento del jugador debe aumentar un 35%.
- El efecto debe durar 6 segundos.
- Al terminar la duración, la velocidad del jugador debe volver a su valor base.

#### 4.3 Rapid Fire

- Al recoger `Rapid Fire`, el cooldown de disparo del jugador debe reducirse un 35%.
- El efecto debe durar 6 segundos.
- Al terminar la duración, el cooldown de disparo debe volver a su valor base.

### 5. Recolección repetida de powerups temporales

- Si el jugador recoge un `Speed Boost` mientras ya tiene activo otro `Speed Boost`, la duración debe reiniciarse a 6 segundos desde la nueva recogida.
- Si el jugador recoge un `Rapid Fire` mientras ya tiene activo otro `Rapid Fire`, la duración debe reiniciarse a 6 segundos desde la nueva recogida.
- En ningún caso la duración debe acumularse ni el multiplicador debe aumentar por recoger varias veces el mismo powerup temporal.

## Criterios de verificación

- Tras 12 segundos y con menos de 3 powerups activos, aparece exactamente un nuevo powerup.
- Tras 12 segundos y con 3 powerups activos, no aparece ningún powerup adicional.
- Ningún powerup aparece dentro de muros, obstáculos o fuera de la zona jugable.
- El jugador puede recoger cualquier powerup activo y este desaparece tras aplicar su efecto.
- Las entidades que no sean el jugador no pueden consumir powerups.
- `Heal` restaura vida sin superar el máximo.
- `Speed Boost` aplica un aumento del 35% durante 6 segundos y luego revierte correctamente.
- `Rapid Fire` aplica una reducción del 35% durante 6 segundos y luego revierte correctamente.
- Recoger un powerup temporal del mismo tipo reinicia la duración en vez de acumularla.
