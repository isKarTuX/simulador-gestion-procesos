# Guion de demo (5 minutos)

Para el ensayo y para el día de la entrega. Los comandos y la demo completa están en el [README](../README.md#demo-en-5-minutos); aquí va qué hacer y qué decir en cada minuto.

## Antes de empezar (no cuenta en los 5 minutos)

- Cierra las apps pesadas que no quieras ver en el catálogo, para que salgan procesos reconocibles.
- Arranca la Api y la Web, y deja abierta la App 1:

```bash
dotnet run --project src/SimuladorProcesos.Api       # http://localhost:5080
dotnet run --project src/SimuladorProcesos.Web       # http://localhost:5090
dotnet run --project src/SimuladorProcesos.Desktop
```

- Crea de antemano un catálogo de 8 procesos por **memoria**. Si entre los 8 hay algún proceso del sistema (SYSTEM), saldrá no expulsivo y se verá la mezcla.
- Deja listas dos pestañas: `http://localhost:5090` y `http://localhost:5080/api/catalogos`.
- Respaldo: `demo-respaldo/demo-web.webm` (parte web grabada; no está en el repo).

## Guion

| Min | Qué haces | Qué dices |
|---|---|---|
| **0:00–0:30** | Muestra el esquema: App 1, BD, Api y Web | «Son tres programas sobre una BD. La App 1 captura procesos reales, la Api los entrega en XML y la App 2 simula Round Robin. Todo en C# y .NET 10.» |
| **0:30–1:30** | **App 1:** N=5, criterio CPU, nombre, **Capturar**, **Guardar** | «Pide a Windows la lista de procesos, ordena por CPU y, si Windows niega uno, lo omite y completa hasta N. La prioridad sale del usuario: SYSTEM es no expulsivo, el resto es expulsivo.» |
| **1:30–2:00** | **Ejecutar actividad** y abre la carpeta de `.txt` | «Cada proceso crea su archivo y copia su descripción letra por letra, esperando TH. Cada carácter consume un quantum.» |
| **2:00–2:30** | Pestaña de la Api: `/api/catalogos` y luego `/procesos` | «Este es el servicio. Devuelve XML y la Web no toca la BD, solo pregunta aquí. Un id que no existe devuelve 404 en XML.» |
| **2:30–3:45** | **Web:** catálogo, TH=100, quantum=5, **Iniciar** | «Cuatro bandas. Cada tarjeta es un proceso con un hilo propio. Los expulsivos salen al agotar su quantum y vuelven al final de Listos; los no expulsivos (tarjeta NE) corren hasta terminar.» Señala el reloj y las ejecuciones que suben. |
| **3:45–4:15** | **Pausar**, espera 2 s, **Reanudar** y luego **Reiniciar** | «La pausa es una compuerta que todos los hilos revisan antes de cada carácter. Reiniciar cancela los hilos, vacía las colas y recarga el catálogo.» |
| **4:15–5:00** | Inicia de nuevo, deja terminar y pulsa **Reportes** | «Aquí el listado de ejecución, el de no expulsivos, el gráfico Proceso contra TurnaRound y los anexos. El TF del enunciado ignora la espera, así que el reporte calcula también el TF real y lo explica.» |

## Consejos

- Si vas justo de tiempo, recorta la parte de los `.txt`: es la más prescindible.
- Entra a **Reportes** con el enlace de la barra, no escribiendo la URL ni recargando: el estado de la simulación vive en la pestaña y se pierde.
- La Web muestra los reportes solo cuando todos los procesos llegan a Terminado.
- Los números del reporte cambian en cada PC, porque dependen de los procesos que haya abiertos.

## Si te preguntan

- **¿Qué es «Espera»?** Una vista, no un estado: los procesos que ya corrieron al menos una vez y esperan turno (subconjunto de Listo). Ver CONTEXT §3.
- **¿Por qué hay dos TF?** El del enunciado (`Quantum × #Ejecuciones`) ignora la espera en Listo; el real es el reloj simulado al terminar. El T.R usa el real.
- **¿Cómo evitan que se mezclen los hilos?** Un hilo dispatcher da el turno por semáforo a un solo proceso a la vez; la pausa es una compuerta global. Ver [ADR-0002](adr/0002-modelo-de-concurrencia.md).
- **¿Por qué una cola propia?** El enunciado pide estructuras dinámicas: Listos usa `ColaEnlazada<T>` y Terminados `ListaEnlazada<T>` con nodos propios. Ver [ADR-0003](adr/0003-estructuras-dinamicas.md).
- **¿Usaron IA?** Sí, bajo las reglas de [AGENTS.md](../AGENTS.md), y está declarado en el README. Cada línea se revisa y se explica.

Para practicar las preguntas: `/sustentacion motor` (o el módulo que quieras) y, antes de la entrega, `/sustentacion todo`. Las notas por módulo están en [docs/sustentacion](sustentacion/).
