# Foundations: Powerups en el mapa

## Contexto

J2P es un videojuego Unity 2D top-down que actualmente no incluye objetos coleccionables dinámicos distribuidos por el mapa. Esto hace que el espacio jugable se use principalmente para desplazamiento y combate, sin recompensas intermedias que incentiven reposicionamiento, control de zonas o decisiones tácticas adicionales.

La propuesta introduce powerups recogibles como una nueva funcionalidad jugable. Estos elementos deben aparecer automáticamente durante la partida en posiciones válidas del mapa y aplicar beneficios concretos al jugador cuando los recoge. La primera iteración debe integrarse de forma modular, con impacto acotado sobre los sistemas existentes y dejando el diseño preparado para futuras ampliaciones.

## Objetivos

- Añadir una mecánica de powerups coleccionables que aporte variedad táctica durante la partida.
- Generar powerups automáticamente en el mapa con una frecuencia fija de 12 segundos.
- Garantizar que nunca haya más de 3 powerups activos al mismo tiempo.
- Asegurar que los powerups solo aparezcan en posiciones válidas, fuera de muros, límites y obstáculos.
- Incorporar tres tipos de powerup en esta primera versión:
  - `Heal`: recupera 25 puntos de vida sin superar la vida máxima.
  - `Speed Boost`: aumenta la velocidad de movimiento un 35% durante 6 segundos.
  - `Rapid Fire`: reduce el cooldown de disparo un 35% durante 6 segundos.
- Permitir que, al recoger de nuevo un powerup temporal del mismo tipo, su duración se reinicie en lugar de acumularse.
- Mantener una presentación visual simple, clara y suficiente para distinguir cada tipo de powerup.

## Restricciones

- La funcionalidad debe mantener una arquitectura modular.
- No se deben modificar sistemas no relacionados con la nueva mecánica.
- En esta primera versión solo el jugador puede recoger powerups.
- La solución debe quedar preparada para añadir más tipos de powerup o más entidades capaces de recogerlos en el futuro.
- No se debe implementar todavía lógica fuera del alcance definido por la práctica.
- La entrega debe centrarse en especificación y planificación, no en implementación de código.
