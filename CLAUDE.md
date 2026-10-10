@AGENTS.md

## Específico de Claude Code

- Subagentes por rol en `.claude/agents/`: `backend`, `desktop`, `motor`, `frontend`, `qa`. Cuando una tarea cae en la carpeta de un rol, delega o actúa bajo ese rol.
- Skills del proyecto: `verificar` (antes de cada PR), `contrato-xml` (cualquier cambio al XML, la BD o `ISimulador`), `/orquestar` (lanzar una tanda de subtareas de `docs/PROMPTS.md`) y `/sustentacion` (ensayo de preguntas).
- Usa `gh` para issues y PRs del repo.
