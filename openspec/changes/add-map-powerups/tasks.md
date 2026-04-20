## 1. Base del sistema de powerups

- [x] 1.1 Crear la estructura modular de powerups (tipos, datos configurables y componente de pickup) sin acoplarla a sistemas no relacionados.
- [x] 1.2 Añadir el controlador del jugador para aplicar efectos instantáneos y temporales usando puntos de extensión mínimos sobre salud, movimiento y disparo.
- [x] 1.3 Preparar los assets/prefabs visuales mínimos para distinguir claramente `Heal`, `Speed Boost` y `Rapid Fire`.

## 2. Spawn y recogida en mapa

- [x] 2.1 Implementar el `PowerupSpawnManager` con intervalo de 12 segundos y límite de 3 powerups activos simultáneos.
- [x] 2.2 Implementar la selección y validación de posiciones de spawn para impedir apariciones dentro de muros, límites u obstáculos.
- [x] 2.3 Conectar la recogida para que solo el jugador pueda consumir powerups y que cada pickup notifique correctamente su eliminación del conteo activo.

## 3. Comportamiento de efectos y verificación

- [x] 3.1 Implementar el efecto `Heal` con curación instantánea de 25 puntos y límite a la vida máxima.
- [x] 3.2 Implementar `Speed Boost` y `Rapid Fire` con duración de 6 segundos y multiplicadores de 1.35 y 0.65 respectivamente.
- [x] 3.3 Asegurar que recoger un powerup temporal del mismo tipo refresca su duración sin acumular intensidad ni tiempo adicional.
- [ ] 3.4 Validar en escena o pruebas de juego que el ciclo de spawn, el límite de activos y la expiración/restauración de efectos cumplen la spec.
