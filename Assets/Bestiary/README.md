# Bestiario de The Forgotten Bestiary

El diario se abre y cierra con **B**. **Esc** lo cierra; **A/D** o las flechas cambian la hoja. También admite los botones y el índice con el ratón. La primera hoja contiene la carta del abuelo.

| Especie | Derrotas necesarias |
| --- | ---: |
| Murciélago | 5 |
| Sapo | 10 |
| Águila | 3 |

Antes del desbloqueo solo se revela el nombre de la especie y el estado sellado de la página. No se muestran retratos, descripciones ni contadores individuales. Al desbloquearla aparecen el sprite animado del enemigo, su hábitat, comportamiento, notas de combate y una anotación del abuelo. El HUD muestra cuántas de las tres entradas están reveladas y anuncia los nuevos registros.

## Integración

- `BestiaryCatalog.asset` contiene los textos, sprites y umbrales. Los tres prefabs originales de enemigos llevan `BestiaryEnemy`, que identifica su especie.
- `EnemigoVida.OnDerrotado` se emite una sola vez, después de aceptar el daño letal. Solo cuenta cuando el atacante pertenece al Player. Cuenta tanto espada como pisotón; no cuenta daño no letal, desactivaciones, despawns ni descargas de escena.
- El progreso se guarda inmediatamente en PlayerPrefs con las claves `TFB.Bestiary.v1.bat`, `.frog` y `.eagle`. Permanece al morir, reiniciar o volver a abrir el juego. No se borra automáticamente al iniciar una partida desde el menú.
- `Resources/Bestiary/BestiaryJournal.prefab` contiene la interfaz completa. `BestiaryBootstrap` la instala en tiempo de ejecución en escenas con `PlayerMovementTest` si todavía no existe. No aparece en escenas de menú sin Player. También proporciona un EventSystem si falta.
- El diario conserva el `Time.timeScale` previo, lo pone a cero y lo restaura al cerrar. Las animaciones del diario usan tiempo no escalado. Los cambios pequeños en `MenuPausa` y `PlayerMovementTest` impiden que Esc abra dos menús a la vez o que el Player ataque durante la lectura.

## Edición y comprobación

Los textos y umbrales se editan en el catálogo. La jerarquía visual es editable en el prefab. El constructor está en `Tools > The Forgotten Bestiary`; reconstruir la interfaz recrea el prefab con el diseño definido en `BestiaryBuilder`.

`BestiaryValidation.StartAutomatedSession()` ejecuta en Play Mode las comprobaciones de umbrales, daño no letal, autoría del golpe, duplicados, desactivación, persistencia y pausa. Se usan almacenes aislados y claves temporales: las pruebas no alteran el progreso real. Al terminar vuelve automáticamente a Edit Mode; el resultado queda disponible en `BestiaryValidation.LastSessionResult`.

Las texturas de la interfaz son copias del kit comprado, adaptadas a filtrado Point y bordes de nueve secciones dentro de `Visuals`; el kit de origen conserva sus ajustes. Los retratos referencian los sprites y clips actuales de los enemigos. La tipografía Crimson Text se distribuye con su licencia SIL Open Font License en `Fonts/OFL.txt`.
