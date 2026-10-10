# CONTEXT.md: Simulador de Gestión de Procesos

> Fuente única de verdad del proyecto. Todo integrante y todo agente de IA debe leer este archivo antes de tocar código. Si algo cambia (contrato XML, reglas, decisiones), se actualiza aquí primero.

---

## 0. Ficha del proyecto

| Campo | Valor |
|---|---|
| Universidad / Programa | Universidad de Córdoba, Ingeniería de Sistemas |
| Curso | Sistemas Operativos |
| Guía | Laboratorio, tema "Gestión de procesos" (fecha de la guía: 27/09/2026) |
| Modalidad | Colaborativa, grupos de trabajo |
| **Entrega** | **Martes 20/10/2026, 1:30 p.m.** |
| Hoy | Sábado 10/10/2026, quedan 10 días |
| Evaluación | Diseño de la solución 30% · Funcionalidad 30% · **Sustentación 40%** |
| Repositorio | https://github.com/isKarTuX/simulador-gestion-procesos |
| Enunciado | `docs/enunciado/LABORATORIO GESTION DE PROCESOS-2026-II.pdf` (solo local, no se publica) |

---

## 1. Objetivos

### 1.1 Objetivo del enunciado
Realizar un simulador que permita reflejar los conceptos de **algoritmos de planificación** y **modelo de estados** para un proceso.

### 1.2 Mis objetivos (lo que quiero lograr)
1. Entregar el sistema completo funcionando de punta a punta (App 1 → BD → REST XML → App 2 → reportes).
2. Que **cada integrante pueda sustentar** cualquier módulo (la sustentación vale 40%).
3. Tener un diseño limpio y defendible: contrato definido, estructuras dinámicas propias, motor probado.
4. Trabajar en paralelo (humanos + agentes de IA) sin pisarnos, con un contrato claro entre módulos.
5. Llegar al 19/10 con todo congelado y dedicar el día a ensayar la sustentación.

### 1.3 Criterios de éxito (Definition of Done global)
- [ ] App 1 captura N procesos reales del SO (por CPU o memoria) y los guarda en un catálogo en BD.
- [ ] Cada proceso ejecuta su actividad (crear archivo y copiar la descripción carácter por carácter).
- [ ] El servicio REST devuelve XML con catálogos, procesos y datos de planificación.
- [ ] App 2 consume el servicio y simula Round Robin con los estados Listo / Ejecución / Terminado.
- [ ] Un hilo por proceso. La simulación se puede pausar y reiniciar en cualquier momento.
- [ ] GUI con los paneles Listos, Ejecución, Espera y Terminado, actualizada en vivo.
- [ ] Reportes: listado de ejecución, listado de no expulsivos y gráfico Proceso vs TurnaRound.
- [ ] Pruebas unitarias del motor en verde.
- [ ] Documentación y diagramas listos para la sustentación.

---

## 2. Requerimientos del enunciado (resumen fiel)

### 2.1 Aplicación 1: escritorio (generador de procesos)

**Captura**
- Obtiene un número de procesos que estén corriendo en el SO de la máquina. **El número lo define el usuario.**
- Se pueden seleccionar por **mayor uso de CPU o de memoria**.

**Catálogo**
- *Catálogo del proceso*: número consecutivo o digitado por el usuario.
- *Nombre del catálogo*: descripción digitada por el usuario que referencia todos los procesos obtenidos.

**Datos de cada proceso**
| Campo | Descripción |
|---|---|
| Pid | Identificador del proceso |
| Nombre | Nombre del proceso |
| Usuario | Usuario que generó el proceso |
| Descripción | Descripción del proceso |
| Prioridad | Procesos generados por el SO → **no expulsivos**; los demás → **expulsivos**. **0 = Expulsivo, 1 = No expulsivo** |

**Actividad de un proceso**
- Crea un archivo con el **nombre del proceso**; su contenido es la **descripción**.
- Por cada carácter copiado, el proceso gasta un **quantum** (disminuye su número de ejecuciones).

**Listado de procesos:** visualizar el conjunto obtenido.

**Web service:** servicio REST (**XML**) para obtener la información de los procesos y los datos del algoritmo de planificación.

### 2.2 Aplicación 2: web (simulador)

**Modelo de estados** (SO llamado SYSTEM):

```
        ┌──────────── (b) expulsivo ────────────┐
        ▼                                         │
     [ LISTO ] ──(CPU libre)──▶ [ EJECUCIÓN ] ───┘
                                      │
                                      └──(actividad completa)──▶ [ TERMINADO ]
```

**Estructuras de datos:** estructura dinámica **por cada estado**. La información de cada proceso se obtiene del servicio de la App 1.

**Liberación de CPU.** Un proceso sale de la CPU si:
- a. Culminó su actividad en totalidad.
- b. Es un proceso expulsivo.

**Cambios de estado**
- **Listo → Ejecución:** si no hay proceso en uso de la CPU.
- **Ejecución → Listo:** solo si se cumple el inciso b (es expulsivo).
- **Ejecución → Terminado:** si la actividad terminó en su totalidad.

**Reportes**
1. *Listado de ejecución*: columnas **P, T.L, R, P.R, T.R, T.F**.
2. *Listado de procesos (no expulsivos)*: mismas columnas.
3. *Gráfico de comportamiento*: **Proceso vs TurnaRound** (línea).
4. *Anexos y observaciones*.

Leyenda: P = nombre proceso · T.L = tiempo de llegada · R = ráfaga · P.R = prioridad asignada · T.R = TurnaRound · T.F = tiempo de finalización.

**GUI (esquema):** cuatro bandas horizontales: **Listos** (azul), **Ejecución** (verde), **Espera** (gris), **Terminado** (rojo). Cada proceso es una tarjeta verde con: `Procesos N`, `Pid`, `Quantum`, `Ejecuciones`.

**Hilos:** cada proceso mantiene **su propio hilo**. El tiempo asumido por cada carácter copiado es **TH**, que determina cuánto espera el hilo para volver a iniciar su actividad.

### 2.3 Consideraciones finales
- Algoritmo de planificación: **ROUND ROBIN**.
- Unidad de tiempo de la simulación: **milisegundos**.
- **TH:** milisegundos, especificado por el usuario en la aplicación web.
- **Ráfaga:** `TR = TH × (cantidad de caracteres de la descripción)`.
- La simulación puede **interrumpirse y reiniciarse** en cualquier momento.
- La App 1 debe permitir **catalogar** la información para usarla luego en el simulador.
- Los catálogos se **almacenan en base de datos**.
- El servicio web debe **permitir acceder a los catálogos almacenados**.

### 2.4 Fórmulas del enunciado
```
Tiempo de llegada:
  si LISTO está vacío  → TL = 0
  si no                → TL = TL_del_último_proceso + 1

Tiempo de finalización:
  TF = Quantum × #Ejecuciones      (#Ejecuciones = veces que usó la CPU)

Ráfaga:
  R = TH × nº_caracteres(descripción)
```
Notación: el enunciado usa "TR" para *ráfaga* (consideraciones finales) y para *TurnaRound* (reportes). **En el código y la documentación usaremos `R` = ráfaga y `TAT` = turnaround** para evitar confusión (en los reportes se rotula "T.R" como pide la guía).

Fórmulas estándar para complementar el reporte: `TAT = TF − TL` · `Espera = TAT − R`.

---

## 3. Ambigüedades y decisiones propuestas

> Mientras el profesor responde, se avanza con la columna "Decisión por defecto". Si responde distinto, se ajusta.

| # | Duda | Decisión por defecto |
|---|---|---|
| 1 | La GUI muestra "Espera" pero el modelo solo tiene 3 estados. | No agregar cuarto estado. "Espera" es una **vista** de los procesos que ya corrieron al menos una vez y esperan turno (subconjunto de Listo). |
| 2 | El quantum aparece distinto por proceso (2, 4, 6, 8, 10). | Quantum global configurable con opción de override por proceso. Medido en **caracteres copiados**. |
| 3 | "Ejecuciones" crece (0→4) pero el texto dice que "disminuye". | Contar veces que usó CPU (`#Ejecuciones`) y guardar aparte `caracteresRestantes`. |
| 4 | `TF = Quantum × #Ejecuciones` ignora la espera real. | Calcular la fórmula del enunciado **y** el tiempo real del reloj simulado. Mostrar ambos y justificar la diferencia. |
| 5 | Las tablas de ejemplo son incoherentes (Word: TR 35 vs 20, TF 35). | Usarlas solo como **formato**, no como casos de prueba. |
| 6 | ¿Cómo conviven expulsivos y no expulsivos en Round Robin? | Expulsivo: sale de CPU al agotar su quantum y vuelve al final de Listo. No expulsivo: corre **hasta terminar** sin salir. |
| 7 | ¿Cómo se determina el "usuario" y la "descripción" de un proceso? | Usuario vía WMI/API del SO; descripción vía metadatos del ejecutable. Respaldo: usar el nombre si viene vacía. |
| 8 | ¿Qué SO se usa? | Windows (WMI, WPF). Si alguien usa Linux/Mac, ver stack alterno (sección 4.3). |

### Preguntas para enviar al profesor
1. ¿"Espera" es un estado, una vista, o parte de Listo?
2. ¿El quantum es global o por proceso? ¿En qué unidad (caracteres o ms)?
3. ¿Cómo debe interactuar un proceso no expulsivo con el Round Robin?
4. ¿Qué fórmula de TurnaRound deben mostrar los reportes?
5. ¿Hay restricción de lenguaje o base de datos?
6. ¿Está permitido el uso de herramientas de IA y cómo se cita?

---

## 4. Arquitectura y stack

### 4.1 Vista general
```
┌────────────────────┐   guarda   ┌──────┐   lee   ┌────────────────────┐
│  APP 1 ESCRITORIO  │ ─────────▶ │  BD  │ ◀────── │  SERVICIO REST XML │
│  Recolector SO     │            └──────┘         │  (ASP.NET Core)    │
│  Catálogos         │                             └─────────┬──────────┘
│  Actividad (archivo)│                                      │ HTTP/XML
└────────────────────┘                                       ▼
                                             ┌────────────────────────────┐
                                             │  APP 2 WEB                 │
                                             │  Motor Round Robin + hilos │
                                             └─────────────┬──────────────┘
                                                           │ SignalR
                                                           ▼
                                             ┌────────────────────────────┐
                                             │ GUI: colas, reportes,      │
                                             │ gráfico (Chart.js)         │
                                             └────────────────────────────┘
```

### 4.2 Stack elegido: C# / .NET 10 LTS (un solo lenguaje)

Decisión y justificación en [ADR-0001](docs/adr/0001-arquitectura-y-stack.md).

| Capa | Tecnología |
|---|---|
| Escritorio | WPF (`net10.0-windows`), MVVM simple |
| Captura de procesos | `System.Diagnostics.Process` + WMI (`System.Management`) para usuario dueño |
| Servicio REST | ASP.NET Core Web API con `AddXmlSerializerFormatters()`, puerto 5080 |
| Base de datos | SQLite + EF Core, archivo en `%LOCALAPPDATA%\SimuladorProcesos\simulador.db` |
| Web | Blazor Server (su circuito SignalR da el tiempo real), puerto 5090 |
| Gráfico | Chart.js vía JS interop |
| Pruebas | xUnit |
| Control de versiones | Git + GitHub (rama por issue, PR con CI) |

### 4.3 Estructura del repo y dependencias
```
src/SimuladorProcesos.Core      ← (sin dependencias) modelos, DTO XML, fórmulas, reglas
src/SimuladorProcesos.Data      ← Core
src/SimuladorProcesos.Engine    ← Core
src/SimuladorProcesos.Api       ← Core, Data
src/SimuladorProcesos.Desktop   ← Core, Data
src/SimuladorProcesos.Web       ← Core, Engine   (catálogos solo por HTTP)
tests/SimuladorProcesos.{Core,Engine,Api}.Tests
```

---

## 5. Modelo de datos y contrato

### 5.1 Esquema de BD
```
Catalogo(
  id            INTEGER PK,
  numero        INTEGER UNIQUE, -- consecutivo o digitado
  nombre        TEXT NOT NULL,
  fecha         DATETIME,
  criterio      TEXT,           -- 'CPU' | 'MEMORIA'
  cantidad      INTEGER         -- N: cantidad de procesos pedidos
)

ProcesoCatalogo(
  id            INTEGER PK,
  catalogo_id   INTEGER FK → Catalogo.id,
  pid           INTEGER,
  nombre        TEXT,
  usuario       TEXT,
  descripcion   TEXT,
  prioridad     INTEGER         -- 0 = expulsivo, 1 = no expulsivo
)
```

### 5.2 Endpoints REST (todos devuelven `application/xml`)
| Método | Ruta | Devuelve |
|---|---|---|
| GET | `/api/catalogos` | `<Catalogos>` con un `<Catalogo>` resumen por catálogo |
| GET | `/api/catalogos/{id}/procesos` | `<Catalogo>` con sus `<Proceso>` |
| GET | `/api/planificacion/{id}?th=100&quantum=5` | `<Planificacion>`: TH, quantum y ráfaga calculada por proceso (`th`, `quantum` opcionales) |
| — | errores | `<Error codigo="404\|400"><Mensaje>…</Mensaje></Error>` |

### 5.3 Ejemplos XML (contrato congelado el 10/10/2026)
Los ejemplos completos están en [`docs/contrato/`](docs/contrato/) y se deserializan en las pruebas con los DTO de `Core/Contrato`. Resumen:
```xml
<Catalogo id="1" numero="1" nombre="Prueba CPU" fecha="2026-10-11T10:30:00" criterio="CPU" cantidad="3">
  <Proceso>
    <Pid>4120</Pid>
    <Nombre>WINWORD</Nombre>
    <Usuario>juan</Usuario>
    <Descripcion>Microsoft Word</Descripcion>
    <Prioridad>0</Prioridad>
  </Proceso>
</Catalogo>
```
```xml
<Planificacion catalogoId="1" algoritmo="RoundRobin">
  <TH>100</TH>
  <Quantum>5</Quantum>
  <Proceso pid="4120" nombre="WINWORD" caracteres="14" rafagaMs="1400" prioridad="0" />
</Planificacion>
```

### 5.4 Interfaz del motor
`Engine/ISimulador.cs` es el contrato entre M5 (motor) y M6/M7 (Web): `Cargar`, `Iniciar`, `Pausar`, `Reanudar`, `Reiniciar`, `ObtenerInstantanea`, `ObtenerReporte` y el evento `CambioEstado`.

> **Regla:** el contrato (§5.1–§5.4) solo cambia con la skill `contrato-xml`, que actualiza este archivo, el código, los ejemplos y las pruebas en un mismo PR.

---

## 6. Módulos (paso a paso)

### M0. Base del proyecto (día 1)
- [x] Repo en Git con la solución y los proyectos de la sección 4.3.
- [x] Congelar contrato XML, esquema de BD e interfaz `ISimulador` (§5).
- [ ] Enviar preguntas al profesor (sección 3).
- [x] Repartir módulos entre agentes (AGENTS.md, `.claude/agents/`, issues en GitHub).

### M1. Recolector de procesos (App 1)
- [ ] Listar procesos y ordenar por memoria (`WorkingSet64`) o por CPU (medir dos muestras separadas ~500 ms).
- [ ] Obtener **usuario** por WMI; **descripción** por `FileVersionInfo.FileDescription` (respaldo: nombre).
- [ ] Manejar `Access Denied` con try/catch y omitir el proceso sin romper.
- [x] Regla de prioridad (`Core/Reglas/ReglaPrioridad`): usuario SYSTEM / NT AUTHORITY / LOCAL SERVICE / NETWORK SERVICE → `1` (no expulsivo); el resto → `0` (expulsivo).
- [ ] Devolver exactamente N procesos válidos (si se omiten por permisos, seguir hasta completar N).

### M2. Catálogos y listado (App 1)
- [ ] Formulario: N, criterio (CPU/memoria), número de catálogo (consecutivo o digitado), nombre del catálogo.
- [ ] Guardar en BD.
- [ ] Grid con el listado de procesos obtenidos.
- [ ] Validaciones: N > 0, nombre no vacío, número no repetido.

### M3. Actividad del proceso (App 1)
- [ ] Crear en una carpeta de trabajo un archivo `<nombre_saneado>.txt` (extensión `.txt` siempre; quitar caracteres inválidos; evitar escribir `chrome.exe`).
- [ ] Copiar la descripción **carácter por carácter**, esperando TH ms entre caracteres.
- [ ] Cada carácter consume un quantum.

### M4. Servicio REST XML
- [x] Web API con formateador XML. (#37)
- [x] Endpoints de la sección 5.2. (#37)
- [x] Alojarlo embebido en la App 1 (Kestrel) o como proyecto aparte que lee la misma BD. (#37, proyecto aparte)
- [ ] Probar con Postman **antes** de tocar la App 2.
- [x] Manejo de errores: catálogo inexistente → 404 en XML. (#37)

### M5. Motor de simulación (App 2, núcleo)
**Estructuras dinámicas propias (no solo `List<T>`):**
- Listos → cola enlazada.
- Ejecución → un solo slot (referencia al proceso actual o null).
- Terminados → lista enlazada.
- Espera → vista derivada (procesos que ya corrieron y esperan turno).

**Hilos y sincronización**
- Un hilo **dispatcher** + un hilo **por proceso**.
- `SemaphoreSlim` por proceso para darle el turno.
- `ManualResetEventSlim` global como compuerta de pausa; cada hilo la revisa antes de cada carácter.
- `CancellationToken` para reiniciar y detener limpiamente.

**Pseudocódigo del dispatcher**
```
loop mientras haya procesos sin terminar:
    esperar compuerta de pausa
    si CPU libre y Listos no vacía:
        p = Listos.desencolar()                 // Listo → Ejecución
        Ejecución = p
        p.concederTurno()
        esperar a que p termine su turno
        si p.completó:
            p.TF = ...                          // Ejecución → Terminado
            Terminados.agregar(p)
        sino (es expulsivo y agotó quantum):
            Listos.encolar(p)                   // Ejecución → Listo
        Ejecución = null
```

**Hilo del proceso (turno)**
```
si es no expulsivo → limite = caracteresRestantes
sino               → limite = min(quantum, caracteresRestantes)
repetir limite veces:
    esperar compuerta de pausa
    copiar siguiente carácter al archivo
    dormir TH ms
    caracteresRestantes--
#Ejecuciones++
```

**Reglas**
- Llegada: si Listos está vacía → `TL = 0`; si no → `TL = TL_último + 1`.
- Finalización: `TF = Quantum × #Ejecuciones` (fórmula del enunciado) + tiempo real simulado.
- **Reinicio:** cancelar hilos, vaciar colas, recargar el catálogo desde el servicio.

### M6. GUI web
- [ ] Cuatro paneles: Listos (azul), Ejecución (verde), Espera (gris), Terminado (rojo).
- [ ] Tarjeta por proceso: `Procesos N · Pid · Quantum · Ejecuciones`.
- [ ] Controles: seleccionar catálogo, TH, quantum, **Iniciar / Pausar / Reanudar / Reiniciar**.
- [ ] Actualización en vivo con SignalR (el motor emite eventos de cambio de estado).

### M7. Reportes
- [ ] Tabla "Listado de ejecución" (P, T.L, R, P.R, T.R, T.F).
- [ ] Misma tabla filtrada a **no expulsivos**.
- [ ] Gráfico de línea **Proceso vs TurnaRound** (Chart.js).
- [ ] Sección "Anexos y observaciones".

### M8. Pruebas y documentación
**Casos de prueba**
- [ ] Todos expulsivos.
- [ ] Todos no expulsivos.
- [ ] Mezcla de ambos.
- [ ] Un solo proceso.
- [ ] Descripción vacía.
- [ ] Pausar y reiniciar a mitad de la simulación.
- [ ] Catálogo con 1 y con muchos procesos.

**Documentación**
- [x] Diagrama de estados (`docs/diagramas.md`).
- [x] Diagrama de arquitectura.
- [x] Diagrama de secuencia del dispatcher.
- [ ] README con cómo correr cada app.
- [ ] Notas de sustentación (sección 9).

---

## 7. Plan para agentes de IA

Movido a archivos propios para no duplicar:

| Qué | Dónde |
|---|---|
| Reglas duras, mapa de propiedad, flujo de ramas y PR | [`AGENTS.md`](AGENTS.md) |
| Roles (`backend`, `desktop`, `motor`, `frontend`, `qa`) con su alcance y criterio de "hecho" | [`.claude/agents/`](.claude/agents/) |
| Skills: `verificar`, `contrato-xml`, `/orquestar`, `/sustentacion` | [`.claude/skills/`](.claude/skills/) |
| Olas de trabajo en paralelo, metas, restricciones | [`docs/PLAN.md`](docs/PLAN.md) |

El rol "Tutor" es la skill `/sustentacion`; el rol "Arquitecto" lo ejerce el humano (dueño de `Core` y del contrato).

---

## 8. Cronograma

Ver las **olas** en [`docs/PLAN.md`](docs/PLAN.md#olas). Hitos fijos: integración App 1 → REST el **mié 14**, punta a punta el **sáb 17**, congelar el **dom 18**, ensayo el **lun 19**, entrega el **mar 20 a la 1:30 p.m.**

> Colchón: si el motor (M5) se atrasa, se recorta pulido visual de M6, nunca el motor ni las pruebas.

---

## 9. Sustentación (40%)

Cada integrante debe poder responder:
- ¿Por qué un hilo por proceso y qué hace el dispatcher?
- ¿Cómo funciona la pausa/reanudación y qué pasa al reiniciar?
- ¿De dónde sale cada fórmula (ráfaga, TL, TF, TAT)? ¿Por qué TF del enunciado difiere del tiempo real?
- ¿Qué diferencia a un proceso expulsivo de uno no expulsivo en tu implementación?
- ¿Qué estructura dinámica usaste para cada estado y por qué?
- ¿Cómo evitaste condiciones de carrera y bloqueos?
- ¿Cómo se obtienen usuario y descripción de un proceso del SO?
- ¿Cómo viaja la información de la App 1 a la App 2?

**Material de apoyo:** diagrama de estados, diagrama de arquitectura, secuencia del dispatcher, demo con un catálogo preparado (incluye mezcla de expulsivos y no expulsivos).

---

## 10. Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Condiciones de carrera / bloqueos en el motor | Diseño simple de turnos con semáforos, pruebas unitarias, revisión humana, QA dedicado |
| Procesos protegidos (Access Denied) | try/catch y omitir; completar N con otros procesos |
| Descripción vacía o rara | Respaldo al nombre del proceso; probar caso borde |
| Nombres de archivo inválidos | Sanear y forzar extensión `.txt` |
| Cambios de contrato a mitad de camino | Contrato congelado en M0; cambios solo vía este archivo |
| Integración tardía | Integrar el domingo 11 y el jueves 15 con datos de prueba, no al final |
| Un integrante no entiende su módulo | Tutor + ronda de explicación cruzada el lunes 19 |
| Ambigüedades del enunciado | Preguntas al profesor ya, decisiones por defecto documentadas en la sección 3 |

---

## 11. Checklist final de entrega (lunes 19)

- [ ] App 1 funciona con N procesos reales, ambos criterios (CPU y memoria).
- [ ] Catálogos persistidos en BD y accesibles por el servicio.
- [ ] REST devuelve XML válido en todos los endpoints.
- [ ] App 2 simula Round Robin correctamente con hilos.
- [ ] Pausa y reinicio funcionan en cualquier momento.
- [ ] GUI con los 4 paneles y tarjetas actualizadas en vivo.
- [ ] Reportes y gráfico correctos.
- [ ] Pruebas unitarias en verde.
- [ ] README, diagramas y notas de sustentación listos.
- [ ] Demo ensayada de principio a fin al menos dos veces.

---

## 12. Glosario rápido

| Término | Significado |
|---|---|
| **Round Robin** | Planificación por turnos: cada proceso usa la CPU por un quantum y vuelve al final de la cola |
| **Quantum** | Cantidad de trabajo (caracteres copiados) que un proceso puede hacer por turno |
| **Expulsivo (preemptive)** | Puede ser sacado de la CPU antes de terminar |
| **No expulsivo** | Mantiene la CPU hasta terminar su actividad |
| **Ráfaga (R)** | Tiempo total de CPU que necesita: `TH × caracteres` |
| **TH** | Tiempo (ms) que se asume por cada carácter copiado |
| **TL** | Tiempo de llegada |
| **TF** | Tiempo de finalización |
| **TAT / T.R** | Turnaround: `TF − TL` |
| **Espera** | `TAT − R` |
