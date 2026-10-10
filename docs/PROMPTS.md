# Prompts por subtarea

Cada módulo está dividido en subtareas que **no comparten archivos**, para que varios agentes trabajen al mismo tiempo sin pisarse. Las interfaces y stubs que las conectan ya están en `main`. Cada bloque de abajo es un prompt completo: cópialo y pégalo tal cual.

## Cómo usarlos

**Opción A, una sola sesión (recomendada).** En la sesión principal, con `main` actualizado, escribe `/orquestar 1` (o `/orquestar 1.4 1.5 1.9` para elegir). Claude lanza cada subtarea como subagente en su propio worktree, en segundo plano, y al final te da la tabla de PR. También puedes pegar el [prompt orquestador](#prompt-orquestador).

**Opción B, una sesión por agente.** En la app de Claude abre una sesión nueva sobre esta carpeta **con worktree activado** y pega el prompt de la subtarea. Repite por cada subtarea que quieras en paralelo. Si olvidas el worktree, el prompt le pide al agente que entre en uno.

**Cuántas a la vez.** Hasta 6 es manejable para revisar PR. Usa las tandas sugeridas de cada ola.

**Después de cada tanda**, pega en la sesión principal el [prompt de integración](#prompt-de-integración) para revisar y fusionar en orden.

Reglas que todos los prompts dan por hechas (están en [`AGENTS.md`](../AGENTS.md)): *Arranque de sesión* (worktree, rama desde `origin/main`, `dotnet` en el PATH), *Cierre de sesión* (push, PR con `Closes #n`, CI en verde, no fusionar) y la skill `verificar`.

---

## Ola 1 · Sáb 10 – Mié 14

| Subtarea | Issue | Agente | Rama | Depende de |
|---|---|---|---|---|
| 1.1 Repositorio de catálogos | #12 | backend | `feat/data-repositorio` | — |
| 1.2 Endpoints REST XML | #13 | backend | `feat/m4-endpoints` | — (prueba con repositorio falso) |
| 1.3 Selección por CPU/memoria | #14 | desktop | `feat/m1-seleccion` | — (metadatos falsos) |
| 1.4 Metadatos usuario/descripción | #15 | desktop | `feat/m1-metadatos` | — |
| 1.5 Actividad carácter a carácter | #16 | desktop | `feat/m3-actividad` | — |
| 1.6 Ventana de catálogos (MVVM) | #17 | desktop | `feat/m2-ui-catalogos` | — (servicios falsos) |
| 1.7 Estructuras dinámicas | #18 | motor | `feat/m5-estructuras` | — **prioridad: desbloquea 1.9 y 2.1** |
| 1.8 Cálculo de tiempos | #19 | motor | `feat/m5-calculos` | — |
| 1.9 Planificador determinista | #20 | motor | `feat/m5-planificador` | 1.7 para que sus pruebas pasen |
| 1.10 Página de simulación | #21 | frontend | `feat/m6-pagina-simulacion` | — (simulador falso) |
| 1.11 Bandas y tarjetas | #22 | frontend | `feat/m6-componentes` | — |
| 1.12 Cliente REST XML | #23 | frontend | `feat/m6-cliente-rest` | — (respuestas de `docs/contrato`) |

**Tandas sugeridas:** A = 1.7, 1.1, 1.2, 1.3, 1.10, 1.12 · B = 1.4, 1.5, 1.6, 1.8, 1.11, 1.9 (lanza 1.9 cuando 1.7 tenga PR).

<a id="subtarea-11"></a>
### Subtarea 1.1 · Repositorio de catálogos en SQLite

```text
Eres el agente `backend` del proyecto (lee .claude/agents/backend.md). Subtarea 1.1 · Issue #12 · Rama `feat/data-repositorio`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: implementar CatalogoRepositorio (EF Core + SQLite) contra ICatalogoRepositorio.

Archivos tuyos (solo estos):
- src/SimuladorProcesos.Data/CatalogoRepositorio.cs
- tests/SimuladorProcesos.Data.Tests/** (ya trae BaseDatosEnMemoria y EsquemaTests)
Solo lectura: SimuladorDbContext.cs e ICatalogoRepositorio.cs (contrato congelado), ExtensionesServicios.cs, todo lo demás.

Pasos (TDD, prueba primero):
1. Constructor que recibe SimuladorDbContext (ExtensionesServicios ya registra el repositorio como scoped).
2. ListarAsync: catálogos sin procesos, ordenados por Numero, con AsNoTracking.
3. ObtenerConProcesosAsync: Include(Procesos) ordenados por Id (orden de captura); null si no existe.
4. GuardarAsync: ArgumentException si Nombre vacío, Cantidad <= 0 o Numero <= 0; InvalidOperationException con mensaje claro en español si el Numero ya existe (compruébalo antes de guardar, no dependas solo de la excepción de la BD); Fecha = DateTime.Now si viene en default; devuelve el catálogo con su Id.
5. SiguienteNumeroAsync: máximo Numero + 1, o 1 si no hay catálogos.
6. Pruebas en tests/SimuladorProcesos.Data.Tests/CatalogoRepositorioTests.cs usando BaseDatosEnMemoria: cada método con caso de éxito y de error (vacío, inexistente, número duplicado, validaciones).

Hecho cuando: todas las pruebas de Data pasan y cada método público tiene al menos una prueba de éxito y una de error.
```

<a id="subtarea-12"></a>
### Subtarea 1.2 · Endpoints REST XML y errores

```text
Eres el agente `backend` (lee .claude/agents/backend.md). Subtarea 1.2 · Issue #13 · Rama `feat/m4-endpoints`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: los 3 endpoints de CONTEXT §5.2 respondiendo con la misma estructura que docs/contrato/*.xml.

Archivos tuyos: src/SimuladorProcesos.Api/** y tests/SimuladorProcesos.Api.Tests/**
Solo lectura: Core/Contrato (DTO congelados), todo Data.
Contexto: CatalogoRepositorio lo implementa la subtarea 1.1 en paralelo. Tú programas contra ICatalogoRepositorio y en las pruebas lo reemplazas por un falso en memoria con WebApplicationFactory + ConfigureTestServices. No necesitas esperar a 1.1.

Pasos:
1. Api/Mapeo/MapeoContrato.cs: Catalogo → CatalogoResumenXml y CatalogoXml (criterio "CPU" | "MEMORIA", prioridad como int); Catalogo + th + quantum → PlanificacionXml (caracteres = Descripcion.Length, rafagaMs = Formulas.Rafaga).
2. Reemplaza los stubs 501 de los controladores. 404 con ErrorXml si el catálogo no existe; 400 con ErrorXml si th o quantum <= 0.
3. Siempre application/xml, aunque el cliente no envíe Accept. Si logras quitar los xmlns:xsi/xsd sin complicar el código, hazlo; si no, déjalos.
4. Program.cs: al arrancar crea la BD con InicializadorBaseDatos.AsegurarCreadaAsync dentro de un scope.
5. Pruebas por endpoint (éxito y error) que deserializan con SerializadorXml y los DTO de Core, más una que compara la estructura de la respuesta con docs/contrato/catalogo-procesos.xml. Las pruebas NO deben crear la BD real en %LOCALAPPDATA%: sobrescribe los servicios o la cadena de conexión en la fábrica.
6. Actualiza SimuladorProcesos.Api.http.

Hecho cuando: pruebas en verde. La prueba manual con la BD real (`dotnet run` + curl) solo es posible cuando 1.1 esté en main; si aún no está, anótalo en el PR.
```

<a id="subtarea-13"></a>
### Subtarea 1.3 · Selección de N procesos por CPU o memoria

```text
Eres el agente `desktop` (lee .claude/agents/desktop.md). Subtarea 1.3 · Issue #14 · Rama `feat/m1-seleccion`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: RecolectorProcesos : IRecolectorProcesos.

Archivos tuyos: src/SimuladorProcesos.Desktop/Servicios/Recolector/** y tests/SimuladorProcesos.Desktop.Tests/Recolector/**
Solo lectura: Servicios/IRecolectorProcesos.cs, Servicios/IProveedorMetadatos.cs (lo implementa 1.4 en paralelo: tú lo recibes por constructor y en pruebas usas un falso), Core/Reglas/ReglaPrioridad.

Diseño (separa lo puro de lo que toca el SO para poder probarlo):
1. MuestraProceso(Pid, Nombre, long? MemoriaBytes, TimeSpan? CpuInicial, TimeSpan? CpuFinal).
2. SelectorProcesos.Ordenar(muestras, criterio): por Memoria = WorkingSet64 descendente; por CPU = (CpuFinal - CpuInicial) descendente; descarta muestras sin dato para el criterio (Access Denied); desempata por Pid. Puro, sin E/S.
3. ILectorProcesos + LectorProcesosSistema: lee System.Diagnostics.Process.GetProcesses(); para CPU toma dos lecturas de TotalProcessorTime separadas ~500 ms (Task.Delay cancelable); captura Win32Exception e InvalidOperationException por proceso dejando el dato en null; hace Dispose de cada Process. Excluye el PID 0 (Idle) y el proceso actual.
4. RecolectorProcesos(ILectorProcesos, IProveedorMetadatos): ordena candidatos, recorre pidiendo metadatos; si devuelven null (el proceso terminó) sigue con el siguiente hasta completar la cantidad; arma ProcesoCatalogo con Prioridad = ReglaPrioridad.Determinar(usuario). cantidad <= 0 → ArgumentOutOfRangeException. Si hay menos procesos que la cantidad, devuelve los que haya.

Pruebas: SelectorProcesos con datos falsos (orden por memoria, por CPU, descarte, desempate); RecolectorProcesos con lector y metadatos falsos (completa N saltando nulls, aplica prioridad, cantidad mayor que disponibles); una prueba de humo [Trait("Category","SistemaReal")] con LectorProcesosSistema real y metadatos falsos que pide 5 procesos por memoria.

Hecho cuando: pruebas en verde y la de humo devuelve 5 procesos reales.
```

<a id="subtarea-14"></a>
### Subtarea 1.4 · Metadatos: usuario (WMI) y descripción

```text
Eres el agente `desktop` (lee .claude/agents/desktop.md). Subtarea 1.4 · Issue #15 · Rama `feat/m1-metadatos`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: ProveedorMetadatosWindows : IProveedorMetadatos, cumpliendo el contrato escrito en IProveedorMetadatos.cs (nunca lanza; "" si no hay usuario; el nombre si no hay descripción; null solo si el proceso terminó).

Archivos tuyos: src/SimuladorProcesos.Desktop/Servicios/Metadatos/** y tests/SimuladorProcesos.Desktop.Tests/Metadatos/**

Pasos:
1. Usuario: WMI `SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = {pid}` + InvokeMethod("GetOwner") → "DOMINIO\usuario"; si ReturnValue != 0, "". System.Management ya está referenciado. Mide cuánto tardan 20 consultas; si pasan de 2 s, agrega una consulta única de todos los procesos con caché corta y documenta la decisión en el PR.
2. Descripción: Process.GetProcessById(pid).MainModule.FileVersionInfo.FileDescription; si falla (acceso denegado, 32/64 bits) usa ExecutablePath de WMI con FileVersionInfo.GetVersionInfo(ruta); si sigue vacía, el nombre del proceso.
3. Proceso inexistente (sin fila WMI y GetProcessById lanza ArgumentException) → null.
4. Pruebas: el proceso de pruebas (Environment.ProcessId) devuelve usuario igual a $"{Environment.UserDomainName}\{Environment.UserName}" y descripción no vacía; PID int.MaxValue → null; PID 4 (System) no lanza y ReglaPrioridad.Determinar(usuario) da NoExpulsivo.

Hecho cuando: pruebas en verde en Windows.
```

<a id="subtarea-15"></a>
### Subtarea 1.5 · Actividad: archivo carácter a carácter

```text
Eres el agente `desktop` (lee .claude/agents/desktop.md). Subtarea 1.5 · Issue #16 · Rama `feat/m3-actividad`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: ActividadProceso : IActividadProceso (M3 del enunciado).

Archivos tuyos: src/SimuladorProcesos.Desktop/Servicios/Actividad/** y tests/SimuladorProcesos.Desktop.Tests/Actividad/**

Pasos:
1. NombreArchivoActividad.Para(nombre, pid): quita Path.GetInvalidFileNameChars y la extensión .exe; nombres reservados de Windows (CON, PRN, AUX, NUL, COM1-9, LPT1-9) llevan sufijo "_"; vacío → "proceso"; resultado "<nombre>_<pid>.txt". El _pid evita que varios procesos con el mismo nombre (svchost, chrome) se sobrescriban; explica esa decisión en el PR para agregarla a CONTEXT §3.
2. EjecutarAsync: crea la carpeta; FileStream con FileShare.Read (el archivo se puede abrir mientras se escribe); UTF-8 sin BOM; por cada carácter: escribir, Flush, progreso.Report(copiados), await Task.Delay(thMs, cancelacion). Descripción vacía → archivo vacío. Cancelación → deja el archivo parcial y propaga OperationCanceledException. thMs <= 0 → ArgumentOutOfRangeException. Devuelve la ruta.
3. Pruebas en una carpeta temporal única (Path.GetTempPath() + Guid) con TH = 1: contenido final == descripción (incluye tildes y ñ); progreso 1..N en orden (usa un IProgress<int> síncrono propio, no Progress<T>); nombres saneados y reservados; cancelación a mitad; descripción vacía. Borra la carpeta al terminar.

Hecho cuando: pruebas en verde.
```

<a id="subtarea-16"></a>
### Subtarea 1.6 · Ventana de catálogos (MVVM) con servicios falsos

```text
Eres el agente `desktop` (lee .claude/agents/desktop.md). Subtarea 1.6 · Issue #17 · Rama `feat/m2-ui-catalogos`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: la ventana principal de la App 1 (M2) completa y presentable, funcionando con servicios falsos.

Archivos tuyos: src/SimuladorProcesos.Desktop/ViewModels/**, Vistas/**, Falsos/**, Recursos/** (estilos), MainWindow.xaml, MainWindow.xaml.cs y tests/SimuladorProcesos.Desktop.Tests/ViewModels/**
Solo lectura: App.xaml(.cs) (lo cablea la subtarea 2.2), Servicios/**, Data.

Diseño:
1. ViewModels/Base: ObservableObject (INotifyPropertyChanged + SetProperty), RelayCommand y AsyncRelayCommand propios, sin paquetes externos (hay que poder explicarlos).
2. CatalogoViewModel(IRecolectorProcesos, ICatalogoRepositorio, IActividadProceso):
   - Formulario: Cantidad, Criterio (CPU/Memoria), UsarConsecutivo + Numero, NombreCatalogo, ThMs de la actividad (100 por defecto).
   - Comandos: Capturar, Guardar (habilitado con procesos y formulario válido), EjecutarActividad (todos los procesos en paralelo, progreso por fila), Cancelar.
   - Validaciones visibles: N > 0, nombre no vacío, número > 0; si el repositorio lanza InvalidOperationException (número repetido), muéstralo como mensaje.
   - Al iniciar: Numero = SiguienteNumeroAsync y carga la lista de catálogos guardados; al seleccionar uno muestra sus procesos.
   - Ocupado (bool) y Mensaje para la barra de estado.
3. ProcesoFilaViewModel: Pid, Nombre, Usuario, Descripcion, Prioridad como "0 · Expulsivo" / "1 · No expulsivo", ProgresoActividad 0-100.
4. Vista: formulario arriba, DataGrid (Pid, Nombre, Usuario, Descripción, Prioridad, barra de progreso), panel lateral de catálogos guardados, barra de estado. Diseño limpio: Segoe UI, márgenes 8/16, colores sobrios, alineado al estilo de la Web (verde #138a13 como acento).
5. Falsos/: RecolectorFalso (mezcla SYSTEM y usuario), RepositorioEnMemoria, ActividadFalsa. MainWindow crea el ViewModel con estos falsos en un único lugar marcado con el comentario `// 2.2 reemplaza los falsos por servicios reales`.

Pruebas del ViewModel con los falsos: capturar llena la lista; guardar con nombre vacío no guarda y muestra error; número repetido muestra mensaje; guardar bien recarga la lista y sugiere el siguiente número.

Hecho cuando: pruebas en verde y `dotnet run --project src/SimuladorProcesos.Desktop` abre la ventana con el flujo completo sobre datos falsos. Adjunta una captura al PR.
```

<a id="subtarea-17"></a>
### Subtarea 1.7 · Estructuras dinámicas: ColaEnlazada y ListaEnlazada

```text
Eres el agente `motor` (lee .claude/agents/motor.md). Subtarea 1.7 · Issue #18 · Rama `feat/m5-estructuras`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.
PRIORIDAD ALTA: las subtareas 1.9 y 2.1 dependen de esta. Termina y abre el PR lo antes posible.

Objetivo: implementar los cuerpos de Engine/Estructuras/ColaEnlazada.cs y ListaEnlazada.cs (ADR-0003).

Archivos tuyos: src/SimuladorProcesos.Engine/Estructuras/** y tests/SimuladorProcesos.Engine.Tests/Estructuras/**

Reglas:
- La firma pública está congelada: no agregues ni quites miembros públicos.
- Nodos propios (Nodo<T> con Valor y Siguiente). Ningún List<T>, Queue<T> ni LinkedList<T>.
- Encolar, Desencolar y Agregar en O(1) con punteros cabeza/cola y un contador.
- Desencolar y VerFrente sobre vacía → InvalidOperationException con mensaje en español.
- Vaciar suelta todas las referencias.
- El enumerador lanza InvalidOperationException si la estructura cambia durante el recorrido (contador de versión); explica el porqué en un comentario de una línea.
- No son thread-safe: el motor las protege con su lock. Dilo en el <summary>.

Pruebas: vacía, un elemento, muchos (FIFO), intercalar encolar/desencolar, Vaciar y reutilizar, enumerar en orden, modificar durante la enumeración, 10 000 elementos.

Hecho cuando: pruebas en verde que cubren cada miembro público.
```

<a id="subtarea-18"></a>
### Subtarea 1.8 · Cálculo de tiempos y filas de reporte

```text
Eres el agente `motor` (lee .claude/agents/motor.md). Subtarea 1.8 · Issue #19 · Rama `feat/m5-calculos`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: convertir procesos terminados en filas de reporte con T.F del enunciado y T.F real.

Archivos tuyos: src/SimuladorProcesos.Engine/Calculos/** y tests/SimuladorProcesos.Engine.Tests/Calculos/**
Solo lectura: Modelo/ProcesoSimulado.cs, Tipos.cs (FilaReporte), Core/Formulas.cs.

Crea internal static class CalculadoraTiempos:
- FilaReporte Fila(ProcesoSimulado p): exige Estado == Terminado y TiempoFinalizacionReal con valor (si no, InvalidOperationException).
  · Proceso = Nombre; TiempoLlegada = TL; Rafaga = R; PrioridadAsignada = (int)Prioridad
  · TiempoFinalizacion (columna T.F) = Formulas.TiempoFinalizacionEnunciado(Quantum, Ejecuciones)
  · TiempoFinalizacionReal = reloj simulado al terminar
  · Turnaround (columna T.R) = Formulas.Turnaround(TF real, TL); usamos el TF real porque refleja la espera (CONTEXT §3, duda 4). Déjalo en un comentario de una línea.
  · Espera = Formulas.Espera(TAT, R)
- IReadOnlyList<FilaReporte> Reporte(IEnumerable<ProcesoSimulado> terminadosEnOrden)
- IReadOnlyList<FilaReporte> SoloNoExpulsivos(IReadOnlyList<FilaReporte> filas)

Pruebas: un catálogo de 3 procesos (2 expulsivos y 1 no expulsivo, TH = 10, quantum = 2) con la tabla esperada calculada a mano y escrita en un comentario de la prueba; arma los ProcesoSimulado a mano (estado, ejecuciones, TF real) sin depender del planificador; proceso no terminado lanza; filtro de no expulsivos.

Hecho cuando: pruebas en verde y la tabla a mano queda en el código de la prueba.
```

<a id="subtarea-19"></a>
### Subtarea 1.9 · Planificador Round Robin determinista

```text
Eres el agente `motor` (lee .claude/agents/motor.md). Subtarea 1.9 · Issue #20 · Rama `feat/m5-planificador`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.
Depende de 1.7 (#18): puedes empezar ya, pero tus pruebas solo pasan cuando ColaEnlazada/ListaEnlazada estén en main. Antes del PR: `git fetch origin && git rebase origin/main`.

Objetivo: la lógica Round Robin SIN hilos, determinista y probada a fondo. La subtarea 2.1 la envolverá con hilos.

Archivos tuyos: src/SimuladorProcesos.Engine/Planificacion/** y tests/SimuladorProcesos.Engine.Tests/Planificacion/**
Solo lectura: Estructuras/, Modelo/ProcesoSimulado.cs, Tipos.cs, ISimulador.cs, Core/Formulas.cs.

API (internal sealed class PlanificadorRoundRobin):
- ctor(IReadOnlyList<ProcesoEntrada>, ConfiguracionSimulacion, Action<CambioEstadoEventArgs> alCambiar): crea ProcesoSimulado 1..N y los encola en Listos asignando TL con Formulas.TiempoLlegada(listosVacia, tlUltimo); avisa (null → Listo) por cada uno.
- RelojMs, Listos (ColaEnlazada), EnEjecucion (ProcesoSimulado?), Terminados (ListaEnlazada), TodosTerminados.
- Despachar(): si la CPU está libre y Listos no está vacía, desencola, Estado = Ejecucion, Ejecuciones++, avisa (Listo → Ejecución) y lo devuelve; si no hay nada, null. Con la CPU ocupada → InvalidOperationException.
- LimiteDelTurno(p): no expulsivo → CaracteresRestantes; expulsivo → min(Quantum, CaracteresRestantes).
- CopiarCaracter(): toma SiguienteCaracter del proceso en ejecución, CaracteresCopiados++, RelojMs += ThMs, devuelve el carácter (2.1 lo escribe al archivo). Sin proceso en ejecución → InvalidOperationException.
- FinalizarTurno(): si Completo → Terminado, TiempoFinalizacionReal = RelojMs, a Terminados, avisa (Ejecución → Terminado); si no (solo expulsivos llegan aquí) → Estado = Listo, al final de Listos, avisa (Ejecución → Listo). EnEjecucion = null.
- Instantanea(EstadoSimulacion): InstantaneaSimulacion con Listos en orden de cola.
- Descripción vacía: al despacharse se completa en ese turno sin copiar caracteres (pasa a Terminado con el reloj actual).
- Un no expulsivo nunca vuelve a Listo.

Pruebas, cada una con la traza esperada escrita a mano en un comentario: todos expulsivos (orden de turnos A, B, C, A, C, …), todos no expulsivos (orden de llegada), mezcla, un solo proceso, descripción vacía, quantum propio, TL = 0, 1, 2…, reloj final = TH × total de caracteres, instantánea con EnEspera correcto, operaciones inválidas lanzan.

Hecho cuando: pruebas en verde con trazas verificadas a mano.
```

<a id="subtarea-110"></a>
### Subtarea 1.10 · Página de simulación con SimuladorFalso

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 1.10 · Issue #21 · Rama `feat/m6-pagina-simulacion`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: la página de simulación completa, funcionando contra un simulador falso.

Archivos tuyos: src/SimuladorProcesos.Web/Simulacion/** y src/SimuladorProcesos.Web/Components/Pages/Home.razor (+ Home.razor.css)
Solo lectura: ISimulador y Tipos.cs, Components/Simulacion/BandaEstado.razor y TarjetaProceso.razor (los estiliza 1.11 en paralelo; usa sus parámetros tal cual), Program.cs (ya registra ISimulador → SimuladorFalso como scoped).

Pasos:
1. SimuladorFalso: implementación creíble con System.Threading.Timer (cada ThMs avanza un carácter, rota por quantum, un no expulsivo corre hasta terminar). Eventos CambioEstado y SimulacionFinalizada reales, Pausar/Reanudar/Reiniciar, instantánea y reporte plausibles. Es de mentira: no tiene que ser exacto, pero sí thread-safe (lock) y Dispose debe detener el timer.
2. Simulacion/CatalogoDeEjemplo.cs: 6 ProcesoEntrada que mezclen prioridad 0 y 1 con descripciones de distinto largo.
3. Home.razor: selector de catálogo (por ahora solo "Catálogo de ejemplo"), TH en ms, quantum; botones Iniciar / Pausar / Reanudar / Reiniciar habilitados según EstadoSimulacion; reloj simulado; las 4 BandaEstado: Listos (solo los que no están EnEspera), Ejecución, Espera (EnEspera) y Terminado. Suscríbete a CambioEstado y llama InvokeAsync(StateHasChanged) con un límite sencillo de ~20 renders por segundo; implementa IDisposable para desuscribirte. Enlace a /reportes (esa página llega en la ola 3).
4. Validación visible: TH y quantum > 0.

Hecho cuando: `dotnet run --project src/SimuladorProcesos.Web` muestra la simulación avanzando en vivo y pausar, reanudar y reiniciar funcionan sin errores en la consola del navegador ni del servidor. Compruébalo en el navegador y adjunta una captura al PR.
```

<a id="subtarea-111"></a>
### Subtarea 1.11 · Bandas y tarjetas con el estilo del enunciado

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 1.11 · Issue #22 · Rama `feat/m6-componentes`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: que las bandas y tarjetas se vean como el esquema de la guía (CONTEXT §2.2, "GUI"), pero pulidas y profesionales.

Archivos tuyos: src/SimuladorProcesos.Web/Components/Simulacion/** (incluidos .razor.css aislados), Components/Layout/MainLayout.razor y wwwroot/app.css
No cambies los parámetros públicos de BandaEstado ni de TarjetaProceso: los usa 1.10 en paralelo.

Requisitos:
- Título de cada banda en su color (Listos azul, Ejecución verde, Espera gris, Terminado rojo) con línea gris debajo.
- Tarjetas verdes (≈ #138a13) con texto blanco: "Procesos N" grande; Pid, Quantum y Ejecuciones pequeños. En fila, con salto de línea.
- Banda vacía con altura mínima y texto tenue "Sin procesos".
- Extras: barra fina de progreso en la tarjeta (CaracteresTotales − CaracteresRestantes), insignia "NE" para no expulsivos, transición suave al cambiar de banda que respete prefers-reduced-motion.
- Layout: encabezado con el nombre del proyecto y navegación Simulación / Reportes. Responsivo hasta 360 px, contraste AA.
- Para probar sin 1.10, crea Components/Simulacion/DemoComponentes.razor con @page "/demo-componentes" y datos fijos (puede quedarse).

Hecho cuando: el PR incluye una captura de /demo-componentes con las 4 bandas y tarjetas.
```

<a id="subtarea-112"></a>
### Subtarea 1.12 · Cliente REST XML de catálogos

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 1.12 · Issue #23 · Rama `feat/m6-cliente-rest`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md.

Objetivo: implementar ClienteCatalogos (HttpClient + XmlSerializer) y probarlo sin la Api real.

Archivos tuyos: src/SimuladorProcesos.Web/Servicios/ClienteCatalogos.cs, src/SimuladorProcesos.Web/Servicios/MapeoEntrada.cs (nuevo) y tests/SimuladorProcesos.Web.Tests/**
Solo lectura: IClienteCatalogos y ErrorServicioException (contrato), Program.cs (ya registra AddHttpClient con ApiBaseUrl).

Pasos:
1. GET api/catalogos, api/catalogos/{id}/procesos y api/planificacion/{id}?th=..&quantum=.. con Accept: application/xml; deserializa con SerializadorXml de Core.
2. Respuesta no 2xx: intenta leer <Error> → ErrorServicioException(codigo, mensaje); si no se puede, ErrorServicioException(status, mensaje genérico). HttpRequestException (Api apagada) → ErrorServicioException(503, "El servicio REST no está disponible en {BaseAddress}").
3. MapeoEntrada.AProcesosEntrada(CatalogoXml) → IReadOnlyList<ProcesoEntrada> (Prioridad desde el int). La subtarea 2.3 lo usa para alimentar el motor.
4. Pruebas con un HttpMessageHandler falso que responde con los archivos de docs/contrato (ya se copian a la carpeta de salida en contrato/): cada método, query string correcta, 404 con <Error>, 500 sin cuerpo, Api caída, mapeo a ProcesoEntrada.

Hecho cuando: pruebas en verde.
```

---

## Ola 2 · Jue 15 – Sáb 17

| Subtarea | Issue | Agente | Rama | Requiere en `main` |
|---|---|---|---|---|
| 2.1 Hilos, pausa y reinicio | #24 | motor | `feat/m5-hilos` | 1.7, 1.8, 1.9 |
| 2.2 App 1 con servicios reales | #25 | desktop | `feat/m2-composicion` | 1.1, 1.3, 1.4, 1.5, 1.6 |
| 2.3 Web con motor real y REST | #7 | frontend | `feat/m6-integracion` | 1.2, 1.10, 1.11, 1.12, 2.1 |
| 2.4 QA del motor | #8 | qa | `test/qa-motor` | PR de 2.1 abierto |

**Tandas sugeridas:** A = 2.1, 2.2 · B = 2.4 en cuanto 2.1 abra su PR · C = 2.3 cuando 2.1 esté fusionado.

<a id="subtarea-21"></a>
### Subtarea 2.1 · Hilos por proceso, pausa y reinicio

```text
Eres el agente `motor` (lee .claude/agents/motor.md). Subtarea 2.1 · Issue #24 · Rama `feat/m5-hilos`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #18, #19 y #20.

Objetivo: SimuladorRoundRobin : ISimulador con un Thread por proceso más un dispatcher (ADR-0002), envolviendo PlanificadorRoundRobin y CalculadoraTiempos.

Archivos tuyos: src/SimuladorProcesos.Engine/SimuladorRoundRobin.cs, src/SimuladorProcesos.Engine/Actividad/** y tests/SimuladorProcesos.Engine.Tests/Concurrencia/**

Diseño:
- Un lock _estado protege al planificador. Las transiciones que avisa el planificador se acumulan y los eventos se disparan FUERA del lock.
- Hilos: un Thread dispatcher y un Thread por proceso (IsBackground, Name = $"Proceso-{pid}"); SemaphoreSlim(0,1) de turno por proceso; SemaphoreSlim(0,1) "turnoTerminado"; ManualResetEventSlim de pausa (se espera antes de cada carácter y de cada despacho); CancellationTokenSource por corrida; todas las esperas reciben el token.
- Hilo del proceso: espera su turno → repite LimiteDelTurno veces { esperar pausa; lock { c = CopiarCaracter() }; destino.Escribir(pid, nombre, c); token.WaitHandle.WaitOne(ThMs) } → libera turnoTerminado. Termina cuando su proceso queda completo.
- Dispatcher: mientras no TodosTerminados { esperar pausa; lock { p = Despachar() }; liberar el turno de p; esperar turnoTerminado; lock { FinalizarTurno() } } → Estado = Finalizada y SimulacionFinalizada.
- Actividad/IDestinoActividad.Escribir(int pid, string nombre, char c) con DestinoArchivos(carpeta) (archivo <nombre>_<pid>.txt, mismo criterio que la App 1) y DestinoEnMemoria (pruebas). Constructor SimuladorRoundRobin(IDestinoActividad? destino = null); por defecto DestinoArchivos en Path.Combine(RutasSimulador.CarpetaActividad, "web").
- Reiniciar: cancela, Join con 2 s de límite de todos los hilos, recrea el planificador con los mismos procesos y configuración, Estado = Detenida. Dispose = cancelar + Join. Pausar/Reanudar idempotentes; transición de EstadoSimulacion inválida → InvalidOperationException.
- ObtenerReporte = CalculadoraTiempos.Reporte(Terminados).

Pruebas [Trait("Category","Concurrencia")] con TH = 1 y DestinoEnMemoria:
- El reporte coincide exactamente con el del planificador ejecutado sin hilos para el mismo catálogo (expulsivos, no expulsivos, mezcla).
- Nunca hay dos procesos en Ejecución (compruébalo en el handler de CambioEstado).
- Pausar congela el reloj (dos instantáneas separadas 100 ms son iguales); Reanudar termina la simulación.
- Reiniciar a mitad deja 0 hilos "Proceso-*" vivos (guarda la lista de Thread y revisa IsAlive) y una nueva corrida da el mismo reporte.
- Dispose durante la ejecución no se cuelga (límite 5 s).
- El texto escrito por cada proceso == su descripción.

Hecho cuando: `for i in $(seq 20); do dotnet test tests/SimuladorProcesos.Engine.Tests --filter Category=Concurrencia || break; done` da 20 de 20 en verde. Al abrir el PR, avisa para lanzar QA (subtarea 2.4).
```

<a id="subtarea-22"></a>
### Subtarea 2.2 · App 1 con servicios reales

```text
Eres el agente `desktop` (lee .claude/agents/desktop.md). Subtarea 2.2 · Issue #25 · Rama `feat/m2-composicion`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #12, #14, #15, #16 y #17.

Objetivo: la App 1 funcionando de punta a punta con servicios reales.

Archivos tuyos: src/SimuladorProcesos.Desktop/App.xaml, App.xaml.cs, MainWindow.xaml.cs (solo la creación del ViewModel), Falsos/** (bórralos o muévelos a tests si las pruebas los usan), SimuladorProcesos.Desktop.csproj y Directory.Packages.props (solo si agregas Microsoft.Extensions.DependencyInjection).

Pasos:
1. Composición en App.OnStartup con ServiceCollection: AddSimuladorData(), RecolectorProcesos, LectorProcesosSistema, ProveedorMetadatosWindows, ActividadProceso, CatalogoViewModel y MainWindow. Quita StartupUri.
2. InicializadorBaseDatos.AsegurarCreadaAsync al arrancar; carpeta de actividad = RutasSimulador.CarpetaActividad.
3. DispatcherUnhandledException y TaskScheduler.UnobservedTaskException → mensaje amable y registro en un log dentro de RutasSimulador.CarpetaDatos.

Prueba manual obligatoria, documentada con capturas en el PR: catálogo por CPU con N = 5 y otro por memoria con N = 8, ambos guardados y listados; número repetido rechazado; la actividad genera los .txt en %LOCALAPPDATA%\SimuladorProcesos\actividad con la descripción completa; con la Api corriendo, `curl http://localhost:5080/api/catalogos` lista ambos (si 1.2 ya está en main).

Hecho cuando: lo anterior funciona y todas las pruebas siguen en verde.
```

<a id="subtarea-23"></a>
### Subtarea 2.3 · Web con motor real y catálogos por REST

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 2.3 · Issue #7 · Rama `feat/m6-integracion`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #13, #21, #22, #23 y #24.

Objetivo: la Web usa el motor real y los catálogos reales a través del servicio REST.

Archivos tuyos: src/SimuladorProcesos.Web/Program.cs, Components/Pages/Home.razor (+ .css), Simulacion/** y appsettings*.json.

Pasos:
1. Registra ISimulador → SimuladorRoundRobin (scoped: se libera con el circuito). Conserva SimuladorFalso solo detrás de la opción "UsarSimuladorFalso": true en appsettings, para demos sin Api.
2. Home: lista de catálogos con IClienteCatalogos.ListarAsync; al elegir uno, obtén procesos y planificación con el TH y quantum del usuario; MapeoEntrada.AProcesosEntrada → Cargar.
3. Errores del servicio con un aviso amable: si la Api está apagada, di cómo arrancarla y ofrece un botón Reintentar.
4. Muestra la ráfaga R de cada proceso (de la planificación) en un detalle o tooltip.

Prueba punta a punta obligatoria, con capturas en el PR: Api y Web corriendo con una BD que tenga un catálogo creado por la App 1 (si 2.2 aún no está, insértalo con un script); simulación completa; pausa, reanudación y reinicio a mitad; cerrar la pestaña no deja hilos vivos (revisa el número de hilos del proceso).

Hecho cuando: lo anterior está documentado en el PR.
```

<a id="subtarea-24"></a>
### Subtarea 2.4 · QA del motor

```text
Eres el agente `qa` (lee .claude/agents/qa.md). Subtarea 2.4 · Issue #8. No implementas funcionalidades: revisas y reportas.
Sigue "Arranque de sesión" de AGENTS.md.

Objetivo: auditar el PR del motor con hilos (subtarea 2.1, issue #24) antes de que se fusione.

Pasos:
1. Encuentra el PR: `gh pr list --search "24 in:body"`; haz `gh pr checkout <n>` dentro de tu worktree.
2. Recorre la lista de .claude/agents/qa.md línea por línea sobre SimuladorRoundRobin.cs y Planificacion/.
3. Corre las pruebas de concurrencia 50 veces seguidas y anota cualquier falla intermitente.
4. Escribe 2 o 3 pruebas adversariales en una rama `test/qa-motor` creada desde la rama del PR: Pausar/Reanudar en bucle cerrado, Reiniciar justo después de Iniciar, Dispose llamado desde el handler de un evento. Abre un PR (draft si fallan) contra la rama del motor.
5. Calcula a mano el reporte de un catálogo de 3 procesos y compáralo con ObtenerReporte().

Entrega: un comentario en el PR del motor con los hallazgos ordenados por severidad (archivo:línea, cómo reproducir, esperado vs obtenido) y un issue `bug` por cada hallazgo confirmado.
```

---

## Ola 3 · Dom 18

| Subtarea | Issue | Agente | Rama | Requiere en `main` |
|---|---|---|---|---|
| 3.1 Tablas de reporte y anexos | #26 | frontend | `feat/m7-tablas` | 2.3 |
| 3.2 Gráfico Proceso vs TurnaRound | #27 | frontend | `feat/m7-grafico` | 2.3 |
| 3.3 Notas: App 1 y servicio REST | #28 | backend | `docs/m8-sustentacion-app1` | 2.2 |
| 3.4 Notas: motor | #29 | motor | `docs/m8-sustentacion-motor` | 2.1 |
| 3.5 Notas: App 2 web | #30 | frontend | `docs/m8-sustentacion-web` | 2.3 |
| 3.6 README, capturas y diagramas | #31 | frontend | `docs/m8-readme` | 3.1, 3.2 |
| 3.7 QA punta a punta | #32 | qa | — | 2.2, 2.3 |

**Tandas sugeridas:** A = 3.1, 3.2, 3.3, 3.4, 3.5, 3.7 · B = 3.6.

<a id="subtarea-31"></a>
### Subtarea 3.1 · Tablas de reporte y anexos

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 3.1 · Issue #26 · Rama `feat/m7-tablas`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #7.

Objetivo: la página /reportes con los listados del enunciado y los anexos.

Archivos tuyos: src/SimuladorProcesos.Web/Components/Pages/Reportes.razor, Components/Reportes/TablaReporte.razor, Components/Reportes/AnexosObservaciones.razor (+ sus .razor.css).
No toques GraficoTurnaround.razor (lo hace 3.2 en paralelo); úsalo así: <GraficoTurnaround Filas="filas" />.

Contenido de /reportes (usa el mismo ISimulador scoped del circuito):
1. Listado de ejecución: columnas P, T.L, R, P.R, T.R, T.F en orden de finalización.
2. Listado de procesos (no expulsivos): mismas columnas, filtradas.
3. Gráfico de comportamiento.
4. Anexos y observaciones: tabla con TF del enunciado vs TF real y espera por proceso; promedios de TAT y espera; un párrafo que explica la diferencia entre ambos TF (CONTEXT §3, duda 4) y los parámetros usados (catálogo, TH, quantum).
- Si la simulación no ha terminado: mensaje y enlace a la simulación.
- Leyenda de columnas en tooltips (T.L = tiempo de llegada, etc.). Botón "Exportar CSV" opcional.

Hecho cuando: tras una simulación, /reportes coincide con ObtenerReporte(). Adjunta una captura al PR.
```

<a id="subtarea-32"></a>
### Subtarea 3.2 · Gráfico Proceso vs TurnaRound con Chart.js

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 3.2 · Issue #27 · Rama `feat/m7-grafico`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #7.

Objetivo: el gráfico de línea Proceso vs TurnaRound de la guía.

Archivos tuyos: src/SimuladorProcesos.Web/Components/Reportes/GraficoTurnaround.razor (+ .razor.css), wwwroot/js/grafico.js, wwwroot/lib/chartjs/** y Components/App.razor (solo para agregar el <script> de Chart.js).
No cambies el parámetro Filas.

Pasos:
1. Copia local de Chart.js para que funcione sin internet el día de la sustentación: descarga chart.umd.min.js de chart.js@4.5.1 (o la última 4.x con al menos dos semanas de publicada) desde cdn.jsdelivr.net a wwwroot/lib/chartjs/ y anota versión y SHA-256 en un comentario de App.razor.
2. wwwroot/js/grafico.js como módulo ES: dibujar(canvasId, etiquetas, valores) y destruir(canvasId).
3. El componente importa el módulo con IJSRuntime en OnAfterRenderAsync, redibuja cuando cambian las filas e implementa IAsyncDisposable para destruir el gráfico.
4. Estilo como la guía: línea azul con marcadores, título "Proceso vs TurnaRound", eje X con los nombres de proceso, eje Y con TAT en ms, cuadrícula horizontal. Sin datos → mensaje.

Hecho cuando: el gráfico aparece en /reportes tras una simulación, sin errores en la consola del navegador. Adjunta una captura al PR.
```

<a id="subtarea-33"></a><a id="subtarea-34"></a><a id="subtarea-35"></a>
### Subtareas 3.3, 3.4 y 3.5 · Notas de sustentación

Un prompt por módulo; cambia solo la primera línea.

```text
Eres el agente `backend`. Subtarea 3.3 · Issue #28 · Rama `docs/m8-sustentacion-app1` · Archivo tuyo: docs/sustentacion/app1-y-servicio.md · Módulos: M1, M2, M3, M4 y Data.
```
```text
Eres el agente `motor`. Subtarea 3.4 · Issue #29 · Rama `docs/m8-sustentacion-motor` · Archivo tuyo: docs/sustentacion/motor.md · Módulo: M5 (estructuras, planificador, hilos).
```
```text
Eres el agente `frontend`. Subtarea 3.5 · Issue #30 · Rama `docs/m8-sustentacion-web` · Archivo tuyo: docs/sustentacion/app2-web.md · Módulos: M6 y M7.
```

Y debajo de esa línea, en los tres casos:

```text
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Edita solo tu archivo.

Objetivo: notas de estudio para que el humano sustente estos módulos sin mirar el código. Léelo todo en main antes de escribir.

Estructura:
1. Qué hace y por qué está diseñado así (5 a 8 líneas).
2. Recorrido del código: las 5 a 8 piezas clave como enlaces archivo:línea, con una línea de explicación cada una.
3. Las preguntas de CONTEXT §9 que tocan estos módulos y 5 preguntas probables más del profesor, cada una con respuesta modelo de máximo 5 líneas.
4. Un diagrama Mermaid si ayuda a explicar (secuencia o clases).
5. "Si me preguntan algo que no sé": dónde mirar y cómo razonarlo.
Español claro, sin relleno. Cada afirmación debe coincidir con el código actual.

Hecho cuando: un compañero que no escribió el código podría responder las preguntas leyendo solo este archivo.
```

<a id="subtarea-36"></a>
### Subtarea 3.6 · README final, capturas y diagramas PNG

```text
Eres el agente `frontend` (lee .claude/agents/frontend.md). Subtarea 3.6 · Issue #31 · Rama `docs/m8-readme`.
Sigue "Arranque de sesión" y "Cierre de sesión" de AGENTS.md. Requiere en main: #26 y #27.

Archivos tuyos: README.md, docs/img/** y docs/diagramas.md (solo para enlazar los PNG).

Pasos:
1. Arranca la Api y la Web (y la App 1 si hay escritorio disponible) con un catálogo real. Toma capturas: App 1 con un catálogo, XML de /api/catalogos en el navegador, simulación a mitad con las 4 bandas ocupadas, y /reportes con el gráfico. PNG de máximo 300 KB en docs/img/.
2. Exporta los 3 diagramas de docs/diagramas.md a PNG con `npx -y @mermaid-js/mermaid-cli@12.0.0` (o una versión exacta con al menos dos semanas de publicada) en docs/img/.
3. README: agrega las secciones "Capturas" y "Demo en 5 minutos" paso a paso. Copia y ejecuta cada comando del README para confirmar que funciona.

Hecho cuando: el README se ve completo en GitHub y todos sus comandos funcionan.
```

<a id="subtarea-37"></a>
### Subtarea 3.7 · QA punta a punta

```text
Eres el agente `qa` (lee .claude/agents/qa.md). Subtarea 3.7 · Issue #32. No implementas: pruebas y reportas.
Sigue "Arranque de sesión" de AGENTS.md. Requiere en main: #25 y #7.

Recorre en la máquina real el checklist de CONTEXT §11 punto por punto: App 1 → BD → Api → Web → reportes. Ejecuta también los casos de CONTEXT §6-M8 en la aplicación real (todos expulsivos, todos no expulsivos, mezcla, un proceso, descripción vacía, pausar y reiniciar a mitad, catálogo de 1 y de muchos procesos).

Entrega: un comentario en el issue #32 con una línea por punto (✅/❌ + evidencia: comando y salida, o captura) y un issue `bug` por cada falla, con pasos para reproducir.
```

---

## Ola 4 · Lun 19 – Mar 20

**Ensayo.** En la sesión principal escribe `/sustentacion todo`. Repite tres veces durante el día; entre rondas relee los archivos que te recomiende.

<a id="prompt-de-cierre"></a>
**Cierre y entrega**, en la sesión principal:

```text
Prepara la entrega. 1) Confirma que main está en verde y lista los issues abiertos de las olas 1 a 3. 2) En una rama docs/m8-cierre, actualiza CONTEXT.md §6 y §11 marcando lo que ya está fusionado (cita el PR de cada casilla) y abre el PR. 3) Cuando lo apruebe y lo fusione, crea la etiqueta v1.0.0-rc1 (o v1.0.0 si te lo digo) y prepara un GitHub Release con notas generadas desde los PR, instrucciones de ejecución y enlace a las capturas. No publiques el release sin mi confirmación.
```

---

## Prompts de la sesión principal

<a id="prompt-orquestador"></a>
### Prompt orquestador

Equivale a `/orquestar`. Cambia la lista de subtareas según la tanda.

```text
Lanza en paralelo las subtareas 1.7, 1.1, 1.2, 1.3, 1.10 y 1.12 de docs/PROMPTS.md. Primero `git switch main && git pull`. Luego, en un solo mensaje, lanza un subagente por subtarea: subagent_type = el agente indicado en la subtarea, isolation "worktree", en segundo plano, y como prompt el bloque completo de esa subtarea copiado tal cual. Cuando terminen, dame una tabla con subtarea, enlace del PR, estado del CI, archivos tocados fuera de su alcance y pendientes, y recomiéndame el orden de merge.
```

<a id="prompt-de-integración"></a>
### Prompt de integración

Úsalo cuando una tanda haya abierto sus PR.

```text
Revisa los PR abiertos (`gh pr list`). Para cada uno: estado del CI, si tiene conflictos con main, archivos que toca fuera de los que su subtarea declara en docs/PROMPTS.md, y un resumen de 3 líneas de lo que hace. Recomienda el orden de merge (primero lo que otras subtareas necesitan) y señala lo que conviene revisar con lupa. No fusiones nada sin mi OK. Cuando te diga "fusiona X", haz squash merge, borra la rama, actualiza las casillas de CONTEXT.md §6 en un commit aparte por PR y dime qué ramas activas deben hacer rebase.
```

### Revisión línea por línea del motor

Antes de fusionar 1.7, 1.9 y 2.1, en la sesión principal:

```text
/code-review sobre el PR <número>: compáralo contra AGENTS.md (reglas duras), ADR-0002 y ADR-0003, y contra lo que pide su subtarea en docs/PROMPTS.md. Después explícame el código de ese PR en orden de ejecución, como si yo tuviera que sustentarlo mañana.
```
