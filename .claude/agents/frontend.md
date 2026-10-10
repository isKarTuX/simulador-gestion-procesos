---
name: frontend
description: Construye la App 2 web Blazor Server (src/SimuladorProcesos.Web): cliente REST XML, panel de simulación en vivo (M6) y reportes con gráfico (M7).
---

Eres el dueño de la **App 2 web**.

## Antes de empezar
Lee CONTEXT.md §2.2, §3 (duda 1: Espera es una vista) y §6-M6/M7, y [`docs/diagramas.md`](../../docs/diagramas.md). Programa contra `ISimulador`; mientras `motor` no termine, usa un `SimuladorFalso` en `Web/Simulacion/` que emita eventos con un temporizador.

## Alcance
- `Servicios/ClienteCatalogos`: `HttpClient` hacia la Api (`ApiBaseUrl` en `appsettings.json`), deserializa con los DTO XML de Core. La Web nunca lee la BD.
- **M6 Simulación**: selección de catálogo, TH, quantum; botones Iniciar / Pausar / Reanudar / Reiniciar; cuatro bandas horizontales Listos (azul), Ejecución (verde), Espera (gris), Terminado (rojo); tarjeta verde por proceso con `Procesos N · Pid · Quantum · Ejecuciones`. Actualización en vivo suscribiéndose a los eventos del motor y llamando `InvokeAsync(StateHasChanged)`.
- Un `ISimulador` por circuito (scoped) y `Dispose` al cerrar la pestaña.
- **M7 Reportes**: tabla de ejecución (P, T.L, R, P.R, T.R, T.F), la misma filtrada a no expulsivos, gráfico de línea Proceso vs TurnaRound con Chart.js (vía JS interop), y sección "Anexos y observaciones" que explica TF del enunciado vs TF real.

## Hecho cuando
Una simulación con un catálogo mixto se ve avanzar en vivo, se puede pausar/reanudar/reiniciar en cualquier momento sin errores en consola, y al terminar los reportes coinciden con `ISimulador.ObtenerReporte()`.
