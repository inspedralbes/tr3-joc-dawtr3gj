# Plan: Powerups en el mapa

## Estrategia de implementación

La implementación debe dividirse en bloques pequeños y desacoplados para minimizar regresiones y permitir que la funcionalidad crezca más adelante. La estrategia recomendada es separar responsabilidades entre generación de powerups, representación del pickup y aplicación de efectos sobre el jugador.

## Fases de trabajo

### 1. Base del sistema

- Definir una estructura común para los tipos de powerup y sus parámetros.
- Crear el componente responsable de representar un powerup recogible en el mapa.
- Preparar los recursos visuales mínimos para distinguir `Heal`, `Speed Boost` y `Rapid Fire`.

### 2. Gestión de aparición

- Implementar un gestor de aparición que controle el temporizador de 12 segundos.
- Hacer que el gestor respete el límite máximo de 3 powerups activos.
- Incorporar un mecanismo de selección de posiciones válidas dentro del mapa.
- Validar cada posición candidata para evitar apariciones en muros, límites u obstáculos.

### 3. Recogida y aplicación de efectos

- Conectar la detección de recogida para que solo el jugador pueda activar los powerups.
- Aplicar `Heal` como efecto instantáneo con límite de vida máxima.
- Aplicar `Speed Boost` y `Rapid Fire` como efectos temporales de 6 segundos.
- Gestionar los efectos temporales por tipo para que una nueva recogida del mismo tipo reinicie la duración.

### 4. Integración con sistemas existentes

- Añadir únicamente los puntos de extensión necesarios sobre salud, movimiento y disparo.
- Evitar reescribir sistemas base ya existentes.
- Mantener el cambio encapsulado para facilitar futuras ampliaciones.

### 5. Validación

- Verificar el ciclo de aparición y el límite de powerups activos.
- Verificar que las posiciones de spawn siempre sean válidas.
- Verificar que solo el jugador pueda recogerlos.
- Verificar que cada tipo de powerup aplica el efecto correcto y que los efectos temporales expiran y se restauran correctamente.

## Decisiones técnicas clave

- Separar el sistema en tres responsabilidades principales:
  - gestor de spawn;
  - objeto recogible;
  - controlador de efectos sobre el jugador.
- Modelar los powerups como datos configurables para facilitar ajustes y ampliaciones.
- Usar validación de posiciones basada en la geometría o colisiones reales del mapa para no duplicar lógica espacial.
- Gestionar los buffs temporales con un estado por tipo y una expiración asociada, en lugar de acumular efectos.

## Riesgos a controlar

- Que no exista todavía una forma clara de identificar zonas válidas de aparición en el mapa.
- Que los efectos temporales no restauren correctamente los valores base del jugador al expirar.
- Que el contador de powerups activos se desincronice si un pickup desaparece fuera del flujo esperado.
- Que en mapas muy densos no se encuentren posiciones válidas con facilidad y un ciclo de spawn quede sin generar powerup.
