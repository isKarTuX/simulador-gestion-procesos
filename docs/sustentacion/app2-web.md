# App 2 web (M6 GUI + M7 reportes) — notas de sustentación

> Estado en `main` al 10/10/2026: la página `/` simula con `SimuladorFalso` y un catálogo fijo; el cliente REST XML existe y tiene pruebas pero `Home` aún no lo usa; `/reportes` y el gráfico aún no existen. Pendientes: #7 (2.3, motor real + REST en la GUI), #26 (3.1, tablas y anexos), #27 (3.2, gráfico Chart.js). Lo de abajo describe lo que hay hoy; al final de cada sección se dice qué cambiará, sin inventar código.

## 1. Qué hace y por qué está diseñado así

La App 2 es Blazor Server (`net10.0`): el circuito SignalR por pestaña empuja la simulación en vivo sin polling.
Programa contra `ISimulador`, así la GUI no sabe si detrás hay un falso o el motor con hilos.
Un `ISimulador` por circuito (scoped) aísla pestañas; al cerrar se desuscribe y libera.
La Web nunca lee la BD: los catálogos solo llegan por HTTP+XML desde la Api.
"Espera" no es un cuarto estado, es una vista de Listo (los que ya usaron CPU y esperan turno).
Los eventos del motor nunca tocan el componente: solo piden un render limitado a ~20/s vía `InvokeAsync(StateHasChanged)`.
Las fórmulas viven en `Core/Formulas`; la Web solo las muestra, no las inventa.

## 2. Recorrido del código (7 piezas)

1. `src/SimuladorProcesos.Web/Program.cs:11` — `ApiBaseUrl` (por defecto `http://localhost:5080`) configura el `HttpClient` de catálogos; la URL sale de configuración, no del código.
2. `src/SimuladorProcesos.Web/Servicios/ClienteCatalogos.cs:21` — `PedirAsync` pide `Accept: application/xml`, traduce "sin conexión" a 503 y XML roto a 502 para que la UI distinga "Api apagada" de "respuesta inesperada".
3. `src/SimuladorProcesos.Web/Servicios/MapeoEntrada.cs:10` — convierte `CatalogoXml` a `ProcesoEntrada`; si la prioridad no es 0/1 falla en vez de simular con un dato inventado.
4. `src/SimuladorProcesos.Web/Simulacion/SimuladorFalso.cs:175` — `Avanzar` (un tick = un carácter): el expulsivo agota su quantum y vuelve al final de Listo; el no expulsivo corre hasta terminar (`SimuladorFalso.cs:210`).
5. `src/SimuladorProcesos.Web/Components/Pages/Home.razor:106` — `OnInitialized` se suscribe a `CambioEstado`/`SimulacionFinalizada` y toma la foto inicial; `Home.razor:154` los handlers solo piden render, nunca tocan estado (los eventos llegan en hilos del motor).
6. `src/SimuladorProcesos.Web/Components/Pages/Home.razor:144` — `Actualizar` parte la instantánea en 4 bandas: Listos sin `EnEspera`, Ejecución (0..1), Espera (vista derivada) y Terminados; `Home.razor:166` limita los renders a uno cada 50 ms con `InvokeAsync`.
7. `src/SimuladorProcesos.Web/Components/Simulacion/TarjetaProceso.razor:10` — tarjeta verde del enunciado (`Procesos N`, Pid, Quantum, Ejecuciones) con insignia NE y barra de progreso; `BandaEstado.razor:8` pinta la banda con su color y contador. El gráfico hoy es solo un aviso: `Components/Reportes/GraficoTurnaround.razor:1` dice "pendiente (subtarea 3.2)".

Qué cambiará: 2.3 (#7) reemplaza `SimuladorFalso` por el motor real en `Program.cs:15` y conecta el selector de catálogo al servicio; 3.1 (#26) agrega la página `/reportes` con tablas y anexos; 3.2 (#27) implementa el gráfico con Chart.js local vía JS interop.

## 3. Preguntas y respuestas modelo (máx. 5 líneas cada una)

### De CONTEXT §9 que tocan M6/M7

**¿Por qué un hilo por proceso y qué hace el dispatcher? (la vista Web)**
La Web solo ve el efecto por `ISimulador`: el dispatcher decide quién entra a Ejecución y cada hilo de proceso copia sus caracteres esperando TH ms. Hoy el falso imita eso con un `Timer` (`SimuladorFalso.cs:28`); con 2.3 serán hilos reales y la GUI no cambia porque solo consume eventos e instantáneas.

**¿Cómo funcionan pausa, reanudación y reinicio en la Web?**
Botones en `Home.razor:41`: Pausar detiene el temporizador sin perder el turno, Reanudar lo reprograma, Reiniciar vacía colas y deja lo cargado listo para Iniciar (`SimuladorFalso.cs:85`). Los botones se habilitan según `EstadoSimulacion` para no pedir transiciones inválidas.

**¿De dónde sale cada fórmula y por qué hay dos TF?**
`Formulas.cs:7`: R = TH × caracteres; `Formulas.cs:15`: TL = 0 si Listo vacío, si no TL anterior + 1; `Formulas.cs:19`: TF enunciado = Quantum × Ejecuciones; TAT = TF − TL; Espera = TAT − R. El falso guarda además el TF real del reloj (`SimuladorFalso.cs:205`) porque el del enunciado ignora la espera; el reporte usa el real para TAT y muestra ambos (duda 4 de §3).

**¿Qué diferencia a un expulsivo de un no expulsivo en tu GUI?**
En el falso (`SimuladorFalso.cs:210`) el expulsivo sale al agotar su quantum y vuelve al final de Listo; el no expulsivo nunca entra a esa rama y corre hasta terminar. En la tarjeta se ve la insignia NE (`TarjetaProceso.razor:16`) y en bandas el expulsivo "circula" Listo→Ejecución→Listo.

**¿Qué estructura usa cada estado y qué ve la Web?**
Las colas propias (`ColaEnlazada`/`ListaEnlazada`) viven en el motor (M5); la Web solo recibe `InstantaneaSimulacion` con listas de solo lectura (`Tipos.cs:67`). Por eso la regla 3 de AGENTS.md no se rompe en Web: aquí `List` es solo una foto para pintar.

**¿Cómo evitan condiciones de carrera entre motor y GUI?**
El falso protege todo con `lock (_candado)` y dispara eventos fuera del lock (`SimuladorFalso.cs:173`); la página nunca muta estado en el handler, solo programa un render y lee la foto dentro de `InvokeAsync` (`Home.razor:191`). El throttle (`Home.razor:68`) evita inundar el circuito SignalR.

**¿Cómo viaja la información de la App 1 a la App 2?**
App 1 → BD (SQLite) → Api REST XML → `ClienteCatalogos` por HTTP → `MapeoEntrada` → `ISimulador.Cargar`. Hoy el cliente ya está probado pero `Home.razor:13` usa `CatalogoDeEjemplo` fijo; 2.3 (#7) conectará el selector al servicio sin que la Web toque jamás la BD (ADR-0001).

*(§9, "¿cómo se obtienen usuario y descripción del SO?": es de M1/App 1, no de este módulo.)*

### 5 probables del profesor

**¿Por qué Blazor Server y no WASM o MVC con refresh?**
Porque su circuito SignalR empuja cada transición en vivo sin recargar ni polling (`Program.cs:9`). El motor emite eventos y la página repinta solo lo necesario con `StateHasChanged`.

**¿Por qué un `ISimulador` scoped por circuito y `Dispose`?**
Para que dos pestañas simulen distinto catálogo/TH sin interferir (`Program.cs:15`). `Home.razor:206` se desuscribe y el simulador libera su temporizador (con el motor real: cancela hilos).

**¿Por qué "Espera" es una vista y no un estado?**
Lo pide la duda 1 de §3: el modelo solo tiene Listo/Ejecución/Terminado. `InstantaneaSimulacion.Espera` (`Tipos.cs:75`) filtra Listos con `EnEspera` (ya usaron CPU, `SimuladorFalso.cs:291`); la banda gris solo agrupa visualmente.

**¿Qué pasa si la Api está apagada o devuelve XML roto?**
`ClienteCatalogos` lanza `ErrorServicioException`: 503 "servicio no disponible", 502 "formato inesperado", o el `<Error>` de la Api (`ClienteCatalogos.cs:37`). Hoy la simulación no lo sufre porque usa el catálogo fijo; tras 2.3 el selector deberá mostrar ese mensaje.

**¿Dónde están los reportes y el gráfico?**
Aún no existen en `main`: `Home.razor:51` enlaza a `reportes` pero no hay página; el componente solo anuncia el pendiente (`GraficoTurnaround.razor:4`). 3.1 agrega tablas + anexos (TF enunciado vs real, promedios) y 3.2 el gráfico Chart.js local con `Filas` como parámetro.

## 4. Diagrama (evento → pantalla)

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario
    participant H as Home.razor
    participant S as ISimulador (hoy Falso)
    participant UI as Bandas + Tarjetas
    U->>H: Iniciar (TH, quantum)
    H->>S: Cargar(ejemplo) + Iniciar
    loop cada TH ms (Timer)
        S->>S: copiar 1 carácter
        S-->>H: CambioEstado
        H->>H: throttle 50 ms + InvokeAsync
        H->>S: ObtenerInstantanea()
        S-->>H: Listos / Ejecución / Espera / Terminados
        H->>UI: repintar 4 bandas
    end
    S-->>H: SimulacionFinalizada
    Note over H,S: Tras 2.3: S = motor real (hilos); tras 3.1/3.2: H enlaza a /reportes con tablas y Chart.js
```

## 5. Si me preguntan algo que no sé

No inventes: di "no lo tengo verificado, lo miro en…", y razona en voz alta.
Motor/hilos → `src/SimuladorProcesos.Engine/SimuladorRoundRobin.cs` y `docs/diagramas.md` §3 (secuencia del dispatcher); contrato GUI↔motor → `src/SimuladorProcesos.Engine/ISimulador.cs:7` y `Tipos.cs:47`.
REST/XML → `src/SimuladorProcesos.Web/Servicios/ClienteCatalogos.cs` + pruebas en `tests/SimuladorProcesos.Web.Tests/`; fórmulas → `src/SimuladorProcesos.Core/Formulas.cs:7`.
Pendientes 2.3/3.1/3.2 → issues #7, #26, #27 y `docs/PROMPTS.md` (subtareas 2.3, 3.1, 3.2): cita lo planeado sin describir código que no existe.
