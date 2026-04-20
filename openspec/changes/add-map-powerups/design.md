## Context

J2P es un juego Unity 2D top-down que actualmente no cuenta con coleccionables dinámicos sobre el mapa. La funcionalidad solicitada introduce un nuevo flujo de gameplay que toca varias piezas: selección de posiciones válidas, ciclo de spawn con límite de instancias activas, detección de recogida por el jugador y aplicación de efectos instantáneos o temporales sobre estadísticas existentes.

La restricción principal es mantener una arquitectura modular y evitar cambios en sistemas no relacionados. Además, la primera versión solo permite que el jugador recoja powerups, pero la solución debe quedar preparada para soportar más tipos de powerup o más receptores en el futuro.

## Goals / Non-Goals

**Goals:**
- Añadir un sistema autónomo de spawn de powerups con intervalo fijo de 12 segundos y máximo de 3 instancias activas.
- Garantizar que los powerups aparezcan únicamente en posiciones válidas del mapa, fuera de muros, límites y obstáculos.
- Definir una representación común para powerups con comportamiento configurable por tipo.
- Aplicar tres efectos iniciales: curación instantánea, aumento temporal de velocidad y reducción temporal del cooldown de disparo.
- Hacer que los efectos temporales del mismo tipo se refresquen al volver a recogerse, sin acumulación.

**Non-Goals:**
- Permitir que enemigos u otras entidades recojan powerups en esta iteración.
- Rediseñar sistemas base de movimiento, disparo o salud más allá de los puntos de extensión necesarios.
- Introducir presentación visual compleja, UI avanzada o efectos artísticos elaborados.
- Cambiar el ritmo de spawn de otros elementos del mapa.

## Decisions

### 1. Separar el sistema en tres responsabilidades
Se implementará con tres bloques desacoplados:
- un `PowerupSpawnManager` para controlar temporizador, límite de instancias y selección de posiciones válidas;
- una entidad/prefab `PowerupPickup` que represente un objeto recogible del mapa;
- un `PlayerPowerupController` o equivalente para aplicar y expirar efectos sobre el jugador.

Rationale: evita mezclar lógica de aparición, representación y aplicación del efecto en un único script, y deja puntos claros de extensión para nuevos tipos o nuevos receptores.

Alternatives considered:
- Centralizar todo en un único manager global: más rápido de implementar, pero peor para ampliar y probar.
- Incrustar la lógica de efecto directamente en el jugador: reduce clases ahora, pero acopla demasiado las reglas de powerups al personaje.

### 2. Modelar los tipos de powerup como datos configurables
Los tipos `Heal`, `Speed Boost` y `Rapid Fire` se representarán con un identificador común y parámetros asociados, idealmente mediante datos serializables o `ScriptableObject` si el proyecto ya usa este patrón.

Rationale: separar configuración de comportamiento facilita ajustar valores sin reescribir lógica y permite añadir más tipos con cambios mínimos.

Alternatives considered:
- Usar condicionales hardcodeados en el pickup: válido para tres tipos, pero escala mal.

### 3. Validar el spawn mediante muestreo de posiciones y comprobación física
El manager obtendrá candidatos dentro del área jugable y validará cada punto contra colisiones o capas del entorno antes de instanciar el powerup. El sistema debe reintentar con un número acotado de intentos por ciclo para evitar bucles infinitos cuando el mapa esté saturado.

Rationale: la validación física reutiliza la verdad del mapa ya existente y evita duplicar geometría navegable.

Alternatives considered:
- Definir manualmente puntos de spawn fijos: más simple, pero menos flexible y menos reutilizable entre mapas.
- Usar cualquier posición aleatoria sin validación: incumple el requisito de no aparecer dentro de obstáculos.

### 4. Gestionar efectos temporales con estado por tipo y tiempo de expiración
Los efectos temporales se almacenarán por tipo en el controlador del jugador con su expiración actual. Al recoger un powerup temporal del mismo tipo, se actualizará la expiración a `now + duration` en vez de sumar duración o multiplicador.

Rationale: cumple exactamente el comportamiento pedido y evita stacking accidental sobre movimiento o disparo.

Alternatives considered:
- Acumular duraciones: contradice el alcance.
- Permitir múltiples buffs idénticos con stacking de multiplicadores: complica el balance y la implementación sin necesidad.

### 5. Integrar cambios sobre sistemas existentes mediante puntos de extensión mínimos
La salud solo necesita una operación de curación con clamp a vida máxima. El movimiento y el disparo deben exponer un multiplicador o modificador temporal consumido por sus scripts actuales, sin reescribir sus bucles principales.

Rationale: minimiza el riesgo de regresiones en sistemas no relacionados y mantiene el cambio encapsulado.

## Risks / Trade-offs

- [No existir una API clara para consultar posiciones válidas del mapa] → Mitigación: encapsular la comprobación en un servicio simple basado en `Physics2D` y capas configurables, sin imponer cambios globales.
- [Los buffs temporales pueden dejar valores persistentes si expiran mal] → Mitigación: centralizar la activación y expiración en un único controlador y restaurar siempre desde valores base.
- [El límite de 3 activos puede desincronizarse si un powerup se destruye fuera del flujo normal] → Mitigación: hacer que el manager contabilice instancias vivas reales o reciba eventos de recogida/destrucción.
- [La validación aleatoria puede fallar repetidamente en mapas pequeños] → Mitigación: usar varios intentos por ciclo y omitir el spawn de ese intervalo si no se encuentra un punto válido.

## Migration Plan

No requiere migración de datos ni cambios externos. La activación consiste en añadir los nuevos prefabs/scripts a la escena o al bootstrap de gameplay correspondiente. Si aparece una regresión, el rollback es desactivar el manager de powerups y retirar sus prefabs sin afectar al resto de sistemas.

## Open Questions

- Qué componente actual del jugador debe ser el punto oficial para aplicar modificadores temporales de velocidad y disparo.
- Si el mapa ya dispone de una capa o volumen explícito que delimite zonas válidas de spawn, o si habrá que inferirlas con colisiones existentes.
