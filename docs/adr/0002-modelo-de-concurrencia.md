# ADR-0002: Modelo de concurrencia del motor

- **Estado**: aceptado
- **Fecha**: 2026-10-10

## Contexto

"Cada proceso debe mantener su propio hilo de ejecución", la simulación se puede pausar y reiniciar en cualquier momento, y solo un proceso puede estar en Ejecución. Hay que coordinar N hilos sin condiciones de carrera ni bloqueos.

## Decisión

- **Un hilo dispatcher** (el planificador) y **un `Thread` dedicado por proceso** (`IsBackground = true`, nombre `Proceso-<pid>`). No se usa el `ThreadPool` ni `Task.Run` para los procesos: el enunciado pide hilos, y un `Thread` explícito es lo que se defiende.
- **Turno por semáforo**: cada proceso tiene un `SemaphoreSlim(0, 1)`. El dispatcher lo libera para darle la CPU; el proceso trabaja su quantum (o hasta terminar si es no expulsivo) y avisa al dispatcher con otro `SemaphoreSlim` de "turno terminado". En todo momento como máximo un proceso tiene permiso: la exclusión mutua de la CPU es estructural.
- **Pausa**: un `ManualResetEventSlim` global. Cada hilo lo espera antes de copiar cada carácter, y el dispatcher antes de cada despacho. Pausar = `Reset()`, reanudar = `Set()`.
- **Reinicio y cierre**: un `CancellationTokenSource` por simulación. Todas las esperas reciben el token; `Reiniciar()` cancela, hace `Join` con tiempo límite de cada hilo y vacía las estructuras.
- **Estado compartido** (colas, slot de Ejecución, reloj) protegido por un único `lock` del motor. Los eventos hacia la UI se disparan **fuera** del lock con una instantánea inmutable.
- **Reloj simulado** en ms que avanza TH por cada carácter copiado; el TH real (`Thread.Sleep`) solo controla la velocidad visual.

## Consecuencias

- Fácil de razonar: un único permiso de CPU, un único lock de estado.
- Las pruebas usan TH = 1 ms y verifican resultados contra el reloj simulado, no contra el reloj de pared, así que son deterministas.
- Con cientos de procesos se crean cientos de hilos; aceptable para el tamaño de catálogo esperado (decenas).
