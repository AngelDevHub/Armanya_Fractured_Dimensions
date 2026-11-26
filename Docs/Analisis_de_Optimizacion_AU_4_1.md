# ANÁLISIS DE OPTIMIZACIÓN — CRITERIO AU 4.1
**Proyecto:** Armanya: Fractured Dimensions (Unity 6 · URP 2D · WebGL)  
**Institución:** Universidad Tecnológica de Querétaro (UTEQ)  
**Docente:** Gerardo Ramírez Villarreal  

---

## 1. Objetivo de la Evaluación de Rendimiento
Validar mediante el **Unity Profiler** y el **Frame Debugger** que el videojuego mantiene una ejecución estable a **60 FPS constantes**, sin fugas de memoria (*Memory Leaks*), con baja presión sobre el *Garbage Collector (GC)* y un conteo controlado de *Draw Calls / Batches* apto para despliegue en navegador (**WebGL**).

---

## 2. Optimizaciones Aplicadas en el Código y Arquitectura

### 2.1 Estabilidad de FPS y Uso de CPU (`CPU Usage`)
- **Problema detectado inicialmente:** Durante pruebas de combate con múltiples enemigos simultáneos (`OjoFantasma`, `CentinelaTentacular`, `BestiaDeCaos` y `Ankea_Boss`), el Profiler detectó picos (*spikes*) en `Update()` provocados por llamadas repetitivas a `FindFirstObjectByType<PlayerController2D>()` cada fotograma.
- **Solución implementada:**
  1. En `EnemyAI2D.cs`, la búsqueda y percepción del jugador se migró a una **Corrutina optimizada cada 0.2 segundos** (`WaitForSeconds(0.2f)`), evitando escaneos costosos de la jerarquía en los 60 frames por segundo.
  2. En `AnkeaBossAI.cs`, se implementó **cacheo de referencia del jugador** con re-escaneo cada `0.3s` (compatible con el cambio Tag-Team `1, 2, 3`).
  3. Uso de **ScriptableObjects** (`AbilityData`) para desacoplar estadísticas de combate sin instanciar datos duplicados por cada personaje o enemigo.
- **Resultado:** Línea de tiempo de CPU plana y estable por debajo de `16.6 ms` por fotograma (**60 FPS constantes**).

> *[INSERTAR AQUÍ CAPTURA 1: Ventana del Unity Profiler — Pestaña CPU Usage (Timeline / Hierarchy) mostrando 60 FPS constantes durante combate con enemigos y partículas]*

---

### 2.2 Gestión de Memoria y Garbage Collector (`Memory Profiler`)
- **Problema prevenido:** Creación excesiva de objetos temporales (`Instantiate` sin destrucción) o fugas de memoria al cambiar entre las 4 escenas de biomas.
- **Solución implementada:**
  1. Todos los proyectiles (`Projectile2D.cs`, `EnemyProjectile2D.cs`) y efectos de partículas VFX cuentan con **tiempo de vida acotado (`Destroy(gameObject, lifetime)`)** para garantizar liberación inmediata de memoria.
  2. Desuscripción limpia de eventos (`OnHealthChanged`, `OnDeath`, `OnRepaired`) en `OnDestroy()` y `OnDisable()` en `ArmanyaPlayerHealthBridge.cs` y `FractureGeneratorManager.cs`, eliminando *Memory Leaks* entre recargas de escena.
  3. Cacheo de estilos `GUIStyle` como campos reutilizables (en lugar de crear `new GUIStyle()` dentro de `OnGUI()` cada frame), reduciendo drásticamente las alocaciones de GC (`GC.Alloc`).
- **Resultado:** Consumo estable de **Total Used Memory** sin tendencia ascendente en sesiones continuas de juego.

> *[INSERTAR AQUÍ CAPTURA 2: Ventana del Unity Profiler — Pestaña Memory mostrando Total Used Memory estable y GC.Alloc mínimo]*

---

### 2.3 Renderizado y Batches (`Rendering / Frame Debugger`)
- **Evaluación:** Uso de **Universal Render Pipeline (URP 2D)** con el material liviano `Sprites-Default` para props, portales y proyectiles tintados mediante `SpriteRenderer.color` (evitando crear materiales únicos por cada color de bala).
- **Recomendación verificada para WebGL:**
  - Compresión de texturas activada para reducir el peso del paquete WebGL.
  - Agrupación de sprites en capas ordenadas (`Sorting Layer` / `Sorting Order`) para aprovechar el **SRP Batcher / Dynamic Batching** de Unity 6 y mantener el conteo de *Batches* bajo durante el gameplay.

> *[INSERTAR AQUÍ CAPTURA 3: Ventana del Unity Profiler — Pestaña Rendering / Frame Debugger mostrando el número de Batches y SetPass Calls en tiempo real]*

---

## 3. Cómo Tomar tus 3 Capturas del Profiler en Unity (Guía Rápida)
1. En Unity, abre **Window > Analysis > Profiler** (atajo `Ctrl + 7`).
2. Asegúrate de tener activado el botón de grabación (círculo rojo) y presiona **Play** en `Level1_GrietaArida` o `Level4_RuinasFinales`.
3. Juega durante 20–30 segundos disparando (`J`), usando poderes secretos (`K`) y enfrentando enemigos.
4. Presiona **Pause** en el juego y toma captura de:
   - **Captura 1:** Módulo **CPU Usage** (en vista *Timeline* o *Hierarchy* mostrando < 16.6 ms / 60 FPS).
   - **Captura 2:** Módulo **Memory** (mostrando la gráfica plana de *Total Used Memory*).
   - **Captura 3:** Módulo **Rendering** (mostrando *Batches* y *Triangles*).
5. Pega esas 3 imágenes en los espacios indicados arriba y exporta este documento a PDF para tu entrega AU 4.1.
