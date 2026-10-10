# AGENTS.md: reglas de trabajo

Reglas para cualquier agente de IA (o persona) que toque este repositorio. Los requisitos, el contrato y las decisiones de diseño viven en [`CONTEXT.md`](CONTEXT.md); el orden de trabajo y quién hace qué, en [`docs/PLAN.md`](docs/PLAN.md). Léelos antes de escribir código.

## Mapa de propiedad

Cada carpeta tiene un único dueño. Trabaja solo dentro de las carpetas de tu módulo; si necesitas algo de otra, abre un issue con la etiqueta `bloqueo`.

| Carpeta | Módulos | Agente dueño |
|---|---|---|
| `src/SimuladorProcesos.Core` | Contrato, modelos, fórmulas | arquitecto (humano). Cambios vía skill `contrato-xml` |
| `src/SimuladorProcesos.Data` | Esquema BD, repositorios | `backend` |
| `src/SimuladorProcesos.Api` + `tests/…Api.Tests` | M4 servicio REST XML | `backend` |
| `src/SimuladorProcesos.Desktop` | M1 recolector, M2 catálogos, M3 actividad | `desktop` (M1 lo puede tomar `backend`) |
| `src/SimuladorProcesos.Engine` + `tests/…Engine.Tests` | M5 motor Round Robin | `motor` |
| `src/SimuladorProcesos.Web` | M6 GUI, M7 reportes | `frontend` |
| `docs/` | M8 documentación | todos (cada uno documenta su módulo) |

## Reglas duras

1. **Contrato congelado.** El XML (CONTEXT §5), el esquema de BD y la interfaz pública `ISimulador` solo cambian con la skill `contrato-xml`, que actualiza CONTEXT.md, código, ejemplos y pruebas en el mismo PR.
2. **Dependencias entre proyectos** (cualquier otra referencia es un error de diseño):
   ```
   Core      ← (ninguna)
   Data      ← Core
   Engine    ← Core
   Api       ← Core, Data
   Desktop   ← Core, Data
   Web       ← Core, Engine        (lee catálogos SOLO por HTTP desde Api, nunca desde Data)
   ```
3. **Estructuras dinámicas propias.** Los estados Listo y Terminado usan `ColaEnlazada<T>` y `ListaEnlazada<T>` escritas en `Engine/Estructuras` con nodos propios. `List<T>`, `Queue<T>` y `LinkedList<T>` solo se usan para instantáneas de solo lectura hacia la UI.
4. **Un `Thread` dedicado por proceso** más un hilo dispatcher. El modelo de sincronización es el de [ADR-0002](docs/adr/0002-modelo-de-concurrencia.md).
5. **Engine es puro**: sin UI, sin red, sin BD, sin `Console`. Todo lo observable sale por eventos e instantáneas.
6. **Nombres de dominio en español** y sin tildes en identificadores (`TiempoLlegada`, `Rafaga`, `ColaEnlazada`). Comentarios en español, una línea, explicando el porqué.
7. **Sin secretos ni rutas absolutas** en el código. La BD por defecto vive en `RutasSimulador.BaseDatosPorDefecto`.

## Flujo de trabajo

1. Toma un issue abierto de tu módulo en GitHub (`gh issue list --label modulo:M5`).
2. Crea tu rama desde `main`: `feat/m5-motor-dispatcher`, `fix/m4-error-404`, `docs/m8-diagramas`. Si hay otros agentes trabajando, hazlo en un git worktree propio.
3. Commits pequeños con Conventional Commits y alcance = proyecto: `feat(engine): cola enlazada para Listos`, `test(engine): transición Ejecución→Listo`.
4. Antes de abrir el PR, ejecuta la skill `verificar` hasta que quede en verde.
5. Abre el PR contra `main` con la plantilla y `Closes #<issue>`. El CI debe pasar; el humano revisa y hace squash merge. El código del motor (M5) se revisa línea por línea.

## Comandos

```bash
dotnet build SimuladorProcesos.slnx
dotnet test SimuladorProcesos.slnx
dotnet format SimuladorProcesos.slnx --verify-no-changes
dotnet run --project src/SimuladorProcesos.Api       # http://localhost:5080
dotnet run --project src/SimuladorProcesos.Web       # http://localhost:5090
dotnet run --project src/SimuladorProcesos.Desktop
```

## Hecho significa

- Compila sin advertencias (son errores) y todas las pruebas pasan.
- Cada regla nueva tiene al menos una prueba que la ejercita; el motor cubre cada transición de estado y los casos de CONTEXT §6-M8.
- La casilla correspondiente de CONTEXT §6 queda marcada en el mismo PR.
- Puedes explicar cada línea en la sustentación: si algo es mágico, simplifícalo o documéntalo.
