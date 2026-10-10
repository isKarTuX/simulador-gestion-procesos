# AGENTS.md: reglas de trabajo

Reglas para cualquier agente de IA (o persona) que toque este repositorio. Los requisitos, el contrato y las decisiones de diseño viven en [`CONTEXT.md`](CONTEXT.md); el orden de trabajo y quién hace qué, en [`docs/PLAN.md`](docs/PLAN.md). Léelos antes de escribir código.

## Mapa de propiedad

Cada carpeta tiene un único dueño. Cuando un módulo está dividido en subtareas, **la subtarea de [`docs/PROMPTS.md`](docs/PROMPTS.md) declara los archivos exactos que posee y manda sobre esta tabla**: edita solo esos. Si necesitas cambiar otro archivo, detente y comenta en tu issue con la etiqueta `bloqueo`.

| Carpeta | Módulos | Agente dueño |
|---|---|---|
| `src/SimuladorProcesos.Core` | Contrato, modelos, fórmulas | arquitecto (humano). Cambios vía skill `contrato-xml` |
| `src/SimuladorProcesos.Data` + `tests/…Data.Tests` | Esquema BD, repositorios | `backend` |
| `src/SimuladorProcesos.Api` + `tests/…Api.Tests` | M4 servicio REST XML | `backend` |
| `src/SimuladorProcesos.Desktop` + `tests/…Desktop.Tests` | M1 recolector, M2 catálogos, M3 actividad | `desktop` |
| `src/SimuladorProcesos.Engine` + `tests/…Engine.Tests` | M5 motor Round Robin | `motor` |
| `src/SimuladorProcesos.Web` + `tests/…Web.Tests` | M6 GUI, M7 reportes | `frontend` |
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

## Arranque de sesión

Varios agentes trabajan a la vez sobre el mismo repo; cada uno necesita su propia copia de trabajo.

1. **Worktree propio.** Si la sesión no se abrió con worktree, entra en uno con la herramienta `EnterWorktree` antes de tocar archivos. Nunca cambies de rama en la carpeta principal.
2. **Rama desde `main` actualizado**: `git fetch origin` y `git switch -c <rama-de-la-subtarea> origin/main`.
3. **`dotnet`**: si `dotnet --version` falla, antepón `%LOCALAPPDATA%\Microsoft\dotnet` al PATH de la sesión (bash: `export PATH="$LOCALAPPDATA/Microsoft/dotnet:$PATH"`).
4. **Contexto**: lee `.claude/agents/<tu-rol>.md`, tu subtarea en `docs/PROMPTS.md` y `gh issue view <n>`.

## Flujo de trabajo

1. Commits pequeños con Conventional Commits y alcance = proyecto: `feat(engine): cola enlazada para Listos`, `test(engine): transición Ejecución→Listo`.
2. Si tu subtarea depende de otra que se fusionó mientras trabajabas: `git fetch origin && git rebase origin/main`.
3. Antes de abrir el PR, ejecuta la skill `verificar` hasta que quede en verde.

## Cierre de sesión

1. `git push -u origin <rama>`.
2. `gh pr create --base main` con el título del issue y el cuerpo de `.github/pull_request_template.md` rellenado, incluido `Closes #<issue>`.
3. `gh pr checks --watch` hasta que el CI quede en verde; si falla, corrige y vuelve a empujar.
4. Responde con: enlace del PR, qué quedó pendiente y 3 líneas "para la sustentación". **No fusiones**: el humano revisa y hace squash merge (el motor, línea por línea).

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
- Las casillas de tu issue quedan marcadas (`gh issue edit`/comentario). CONTEXT.md no se toca en PRs de subtareas: el humano actualiza §6 al fusionar, así los PR paralelos no chocan.
- Puedes explicar cada línea en la sustentación: si algo es mágico, simplifícalo o documéntalo.
