# REPORTE DE PRUEBAS (QA BUG REPORT) — ARMANYA: FRACTURED DIMENSIONS

**Institución:** Universidad Tecnológica de Querétaro (UTEQ)  
**Criterio:** SA — Reporte de Pruebas (Mínimo 5 errores documentados por cada nivel de severidad: Crítica, Alta, Media y Baja)  
**Versión evaluada:** Beta 1.0 (Unity 6 · URP 2D · WebGL)  

---

## Resumen de Severidades
- **Crítica (5 bugs):** Impiden continuar la partida, bloquean progresión o congelan el juego.
- **Alta (5 bugs):** Afectan mecánicas principales (combate, HUD de vida, IA o colisiones importantes).
- **Media (5 bugs):** Problemas visuales, de balance o de experiencia de usuario que no impiden avanzar.
- **Baja (5 bugs):** Detalles estéticos, textos o pulido menor.

---

## 1. SEVERIDAD CRÍTICA

### BUG-C01: El portal de transición no detectaba a los héroes de Armanya al pulsar `S`
- **Descripción del error:** Al acercarse al portal dimensional con Angus, Anya o Aleek y presionar la tecla `S`, el personaje no cambiaba de escena.
- **Pasos para reproducir:** 1) Iniciar `Level1_GrietaArida`. 2) Recolectar la pista `Bitácora Hídrica`. 3) Caminar frente al portal y presionar `S`.
- **Información adicional:** `TransitionPoint.cs` heredado de 2DGamekit comparaba únicamente `other.gameObject == transitioningGameObject` (referencia exclusiva a Ellen) y usaba el antiguo `Input.GetKeyDown`.
- **Comportamiento esperado:** El portal debe reconocer cualquier objeto con `PlayerController2D` o tag `"Player"` y cargar la siguiente escena al pulsar `S`, `↓` o `E`.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Crítica.
- **Prioridad:** Urgente (Resuelto en `TransitionPoint.cs` y `ArmanyaPortalController.cs`).

### BUG-C02: Pantalla de Game Over sin opción interactiva de reinicio
- **Descripción del error:** Al agotar los corazones de vida, la pantalla se oscurecía pero el jugador quedaba atrapado sin botones para reintentar o salir al menú.
- **Pasos para reproducir:** 1) Recibir daño de enemigos hasta llegar a 0 HP. 2) Esperar el fundido a negro de `ScreenFader`.
- **Información adicional:** Ocurría en WebGL donde no se puede cerrar la ventana fácilmente.
- **Comportamiento esperado:** Mostrar botones claros "Volver a Intentar" (recarga la escena actual) y "Salir del Nivel" (regresa a `Menu_Inicio`).
- **Frecuencia:** Siempre (100%).
- **Severidad:** Crítica.
- **Prioridad:** Urgente (Resuelto en `ArmanyaPlayerHealthBridge.cs`).

### BUG-C03: Bloqueo de progresión al nivel final por falta de sincronización de pistas
- **Descripción del error:** Al cruzar de Nivel 1 a Nivel 2 y Nivel 3, el contador de pistas se reiniciaba impidiendo llegar a 3/3 para abrir las Ruinas Finales.
- **Pasos para reproducir:** 1) Recoger pista en Nivel 1. 2) Cambiar de escena a Nivel 2. 3) Revisar contador en HUD.
- **Información adicional:** El objeto `GameManager` no persistía correctamente el estado de pistas entre escenas.
- **Comportamiento esperado:** `GameManager` debe persistir (`DontDestroyOnLoad`) y conservar las pistas recolectadas (`1/3`, `2/3`, `3/3`) durante toda la sesión.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Crítica.
- **Prioridad:** Urgente (Resuelto en `GameManager.cs` y `GameSession.cs`).

### BUG-C04: Pérdida de referencia de cámara al alternar héroes Tag-Team en Nivel 4
- **Descripción del error:** Al presionar las teclas `1`, `2` o `3` en `Level4_RuinasFinales`, la cámara dejaba de seguir al héroe activo o los corazones del HUD dejaban de actualizarse.
- **Pasos para reproducir:** 1) Cargar `Level4_RuinasFinales`. 2) Presionar `2` para cambiar a Anya y moverse a la derecha.
- **Información adicional:** `TagTeamManager` requería reasignar `CameraFollow2D.SetTarget()` y reconectar `ArmanyaPlayerHealthBridge.ConnectUI()`.
- **Comportamiento esperado:** Al cambiar de héroe, la cámara sigue instantáneamente al nuevo personaje y el HUD mantiene sincronizados los corazones.
- **Frecuencia:** Frecuente (80% si no se asignaba la cámara manualmente).
- **Severidad:** Crítica.
- **Prioridad:** Urgente (Resuelto en `TagTeamManager.cs`).

### BUG-C05: Caída infinita fuera de los límites del mapa en plataformas rotas
- **Descripción del error:** Si el jugador caía por una grieta profunda fuera de las plataformas principales, caía indefinidamente sin morir ni reaparecer.
- **Pasos para reproducir:** 1) Saltar hacia el vacío lateral en `Level1_GrietaArida`. 2) Observar la posición Y descendiendo sin límite.
- **Información adicional:** Faltaba un límite inferior de muerte o retorno seguro (`KillZone` / `CameraFollow2D` bounds).
- **Comportamiento esperado:** Al salir del límite inferior del escenario, el jugador pierde un corazón y reaparece en terreno firme o activa Game Over.
- **Frecuencia:** Ocasional (depende del punto de caída).
- **Severidad:** Crítica.
- **Prioridad:** Alta.

---

## 2. SEVERIDAD ALTA

### BUG-A01: Portal dimensional invisible en la vista de escena y juego (URP 2D)
- **Descripción del error:** Al arrastrar el prefab `Teleporter.prefab` de 2DGamekit, solo se veía un contorno naranja vacío en el editor y era completamente transparente al jugar.
- **Pasos para reproducir:** 1) Instanciar `Teleporter.prefab` en `Level1_GrietaArida`. 2) Presionar Play.
- **Información adicional:** El shader heredado de Built-in Render Pipeline y su `Animator` dejaban el canal Alpha en 0 bajo URP 2D.
- **Comportamiento esperado:** El portal debe verse nítido con material `Sprites-Default` y un efecto de partículas cian luminoso.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Alta.
- **Prioridad:** Alta (Resuelto en `ArmanyaGamekitVFXIntegratorTool.cs`).

### BUG-A02: Picos de CPU (stuttering) causados por búsquedas por frame en IA de enemigos
- **Descripción del error:** Con múltiples monstruos activos en pantalla, el Unity Profiler registraba picos de CPU y caídas momentáneas de FPS.
- **Pasos para reproducir:** 1) Abrir el Unity Profiler. 2) Entrar a una zona con 8+ enemigos y moverse rápido.
- **Información adicional:** `EnemyAI2D.cs` y `AnkeaBossAI.cs` ejecutaban `FindFirstObjectByType<PlayerController2D>()` dentro de `Update()` en cada frame.
- **Comportamiento esperado:** La IA debe cachear la referencia del jugador y actualizar su percepción en una corrutina/temporizador cada 0.2s–0.3s, manteniendo 60 FPS estables.
- **Frecuencia:** Siempre en combate intenso.
- **Severidad:** Alta.
- **Prioridad:** Alta (Resuelto en `EnemyAI2D.cs` y `AnkeaBossAI.cs`).

### BUG-A03: Desincronización de daño entre corazones (2DGamekit) y HealthSystem
- **Descripción del error:** Al recibir un golpe de 20 de daño, se perdía un corazón completo de golpe en lugar de reflejar el 100% de vida por corazón.
- **Pasos para reproducir:** 1) Dejar que un proyectil enemigo (20 daño) impacte al jugador. 2) Observar la barra de corazones.
- **Información adicional:** `Damageable` usaba 5 puntos totales mientras `HealthSystem` usaba 100 HP.
- **Comportamiento esperado:** Cada corazón debe equivaler a 100 HP (500 HP totales para 5 corazones) y sincronizarse exactamente al recibir daño o curarse.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Alta.
- **Prioridad:** Alta (Resuelto en `ArmanyaPlayerHealthBridge.cs`).

### BUG-A04: Proyectiles del jugador colisionando consigo mismo al disparar
- **Descripción del error:** Al presionar `J` corriendo hacia adelante, el proyectil desaparecía instantáneamente frente al héroe.
- **Pasos para reproducir:** 1) Correr hacia la derecha y pulsar `J` repetidamente. 2) Observar que el disparo no avanza.
- **Información adicional:** El collider del proyectil tocaba el `BoxCollider2D` del propio jugador antes de salir del `FirePoint`.
- **Comportamiento esperado:** `Projectile2D` debe ignorar el collider del `owner` (quien dispara) y solo dañar enemigos u obstáculos.
- **Frecuencia:** Frecuente al moverse y disparar.
- **Severidad:** Alta.
- **Prioridad:** Alta (Resuelto en `Projectile2D.cs`).

### BUG-A05: El Poder Secreto (`K`) no activaba los objetos de restauración si no tenían Trigger
- **Descripción del error:** Al usar el poder de Angus sobre una bomba de agua o generador, el evento `IRestorable.Restore()` no se disparaba.
- **Pasos para reproducir:** 1) Acercarse a un `RestorableObject` con collider pequeño. 2) Presionar `K`.
- **Información adicional:** El radio de detección de área necesitaba que los interactables tuvieran un `Collider2D` habilitado dentro del rango de la habilidad.
- **Comportamiento esperado:** Al pulsar `K` cerca de un objeto restaurable o generador, debe cambiar a estado Reparado, sumar puntaje y mostrar confirmación visual.
- **Frecuencia:** Frecuente.
- **Severidad:** Alta.
- **Prioridad:** Alta (Resuelto en `RestorableObject.cs` y `ArmanyaGDDMechanicsBuilderTool.cs`).

---

## 3. SEVERIDAD MEDIA

### BUG-M01: Pistas de Lore demasiado pequeñas y difíciles de distinguir en el escenario
- **Descripción del error:** Los íconos de las pistas (`Bitácora Hídrica`, `Manifiesto Roto`, `Consola`) se perdían visualmente entre el fondo del nivel.
- **Pasos para reproducir:** 1) Explorar `Level1_GrietaArida` en resolución 1080p. 2) Intentar localizar la pista sin guía.
- **Información adicional:** La escala inicial era `1.0` sin animación de pulso.
- **Comportamiento esperado:** Escala mínima `2.0x` con efecto de pulso/brillo flotante para destacar claramente como objetivo narrativo.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Media.
- **Prioridad:** Media (Resuelto en `LoreClue.cs` y `ArmanyaSpriteSetupTool.cs`).

### BUG-M02: Sobreposición del HUD de HP y pistas en la esquina superior izquierda
- **Descripción del error:** El cuadro de diálogo de estado y pistas tapaba parcialmente los corazones de vida de la esquina superior izquierda.
- **Pasos para reproducir:** 1) Iniciar cualquier nivel. 2) Observar esquina superior izquierda.
- **Información adicional:** El HUD de depuración de `GameSession` y el `HealthUI` de 2DGamekit compartían las mismas coordenadas.
- **Comportamiento esperado:** Corazones limpios a la izquierda; pistas de lore y puntaje numérico organizados en la esquina superior derecha.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Media.
- **Prioridad:** Media (Resuelto en `KeyUI.cs` y `GameSession.cs`).

### BUG-M03: Falta de retroalimentación visual al recoger ítems de curación
- **Descripción del error:** Al pasar sobre un `HealthPickup` o `StabilityOrb`, el objeto desaparecía pero el jugador no percibía visualmente si se había curado.
- **Pasos para reproducir:** 1) Recibir daño y recoger un ítem de vida. 2) Observar al personaje.
- **Información adicional:** Faltaba feedback inmediato de Game Feel.
- **Comportamiento esperado:** Destello verde temporal sobre el sprite del jugador y texto flotante visible indicando la recuperación de vida.
- **Frecuencia:** Siempre (100%).
- **Severidad:** Media.
- **Prioridad:** Media (Resuelto en `HealthPickup.cs` y `StabilityOrb.cs`).

### BUG-M04: Proyectiles de héroes y enemigos compartían el mismo color genérico
- **Descripción del error:** En combates con varios monstruos era confuso distinguir qué balas pertenecían al jugador y cuáles a los enemigos.
- **Pasos para reproducir:** 1) Disparar con Aleek mientras un `OjoFantasma` ataca simultáneamente.
- **Información adicional:** Todos usaban el sprite blanco/azul original de `Bullet.prefab`.
- **Comportamiento esperado:** Tinte diferenciado por identidad visual del GDD (Aleek: Cian, Angus: Celeste/Amarillo, Anya: Violeta neón, Monstruos: Rojo/Verde ácido/Naranja fuego).
- **Frecuencia:** Siempre (100%).
- **Severidad:** Media.
- **Prioridad:** Media (Resuelto en `Assets/Prefabs/Projectiles/`).

### BUG-M05: Cámara seguía al jugador fuera de los bordes vacíos del mapa
- **Descripción del error:** Al acercarse al extremo izquierdo o derecho del escenario, la cámara mostraba el fondo azul/vacío fuera del tilemap.
- **Pasos para reproducir:** 1) Caminar hasta el borde inicial izquierdo de `Level1_GrietaArida`.
- **Información adicional:** `CameraFollow2D` no limitaba sus coordenadas X/Y a los límites del nivel.
- **Comportamiento esperado:** Cámara con límites configurables (Confiner 2D) para mantenerse siempre dentro del área decorada del bioma.
- **Frecuencia:** Siempre en los bordes.
- **Severidad:** Media.
- **Prioridad:** Media (Resuelto en `CameraFollow2D.cs`).

---

## 4. SEVERIDAD BAJA

### BUG-B01: Mensajes tutoriales antiguos en inglés ("Press A/D" de Ellen) aparecían al iniciar
- **Descripción del error:** Al cargar escenas adaptadas de 2DGamekit, aparecía brevemente un cuadro de diálogo en inglés del proyecto original.
- **Pasos para reproducir:** 1) Entrar a `Level1_GrietaArida` y caminar cerca de un `InfoPost`.
- **Información adicional:** `DialogueCanvasController` heredado permanecía activo.
- **Comportamiento esperado:** Todos los mensajes de interfaz y diálogos deben mostrarse exclusivamente en español y acorde a la historia de Armanya.
- **Frecuencia:** Frecuente en los primeros niveles.
- **Severidad:** Baja.
- **Prioridad:** Baja (Resuelto en `ArmanyaPlayerHealthBridge.cs`).

### BUG-B02: Falta de sacudida de cámara (Camera Shake) al recibir impactos fuertes
- **Descripción del error:** Los golpes de enemigos o el uso de poderes secretos se sentían planos visualmente.
- **Pasos para reproducir:** 1) Recibir daño o activar `K`. 2) Observar que la cámara permanece estática.
- **Información adicional:** Requerido para el criterio DE (Game Feel).
- **Comportamiento esperado:** Sacudida sutil y controlada (`TriggerShake`) en `CameraFollow2D` ante impactos y habilidades secretas.
- **Frecuencia:** Siempre.
- **Severidad:** Baja.
- **Prioridad:** Baja (Resuelto en `CameraFollow2D.cs`).

### BUG-B03: Posición vertical inicial del portal flotando sobre el suelo
- **Descripción del error:** Al instanciar el prefab `Armanya_DimensionPortal`, aparecía ligeramente elevado respecto a la plataforma de pasto.
- **Pasos para reproducir:** 1) Arrastrar el prefab al centro de la escena sin ajustar el eje Y.
- **Información adicional:** El pivote del sprite original estaba centrado.
- **Comportamiento esperado:** Alinear la base del portal con la superficie caminable del escenario.
- **Frecuencia:** Ocasional (al colocar manualmente).
- **Severidad:** Baja.
- **Prioridad:** Baja.

### BUG-B04: Advertencia de formato de fin de línea LF/CRLF al subir scripts a Git
- **Descripción del error:** Git mostraba avisos `LF will be replaced by CRLF` al preparar commits en Windows.
- **Pasos para reproducir:** 1) Ejecutar `git add` en PowerShell.
- **Información adicional:** No afecta la ejecución en Unity ni en WebGL.
- **Comportamiento esperado:** Normalización transparente de saltos de línea mediante `.gitattributes` o configuración de Git.
- **Frecuencia:** Siempre en Windows.
- **Severidad:** Baja.
- **Prioridad:** Baja.

### BUG-B05: Etiqueta de depuración de Tag-Team poco visible en la esquina izquierda
- **Descripción del error:** El texto plano `Héroe: Angus [1] [2] [3]` en `TagTeamManager` era difícil de leer sobre fondos claros.
- **Pasos para reproducir:** 1) Iniciar `Level4_RuinasFinales` y revisar la parte izquierda de la pantalla.
- **Información adicional:** Usaba `GUI.Label` sin fondo.
- **Comportamiento esperado:** Recuadros estilizados en la parte inferior central resaltando el héroe activo con color cian brillante.
- **Frecuencia:** Siempre en Nivel 4.
- **Severidad:** Baja.
- **Prioridad:** Baja (Resuelto en `TagTeamManager.cs`).
