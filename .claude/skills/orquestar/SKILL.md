---
name: orquestar
description: Lanza en paralelo la siguiente ola de agentes del plan, cada uno en su worktree. Uso: /orquestar [ola]
disable-model-invocation: true
---

1. Lee la sección "Olas" de `docs/PLAN.md` y el estado real: `gh issue list --state all --json number,title,state,labels`. La ola a lanzar es la indicada en el argumento o, si no hay, la primera con issues abiertos cuyas dependencias estén cerradas.
2. Muestra al usuario una tabla: issue, agente, rama, dependencias. Espera su confirmación antes de lanzar nada.
3. Lanza un subagente por issue de la ola, todos en el mismo mensaje, con `subagent_type` = el agente dueño, `isolation: "worktree"` y en segundo plano. Prompt de cada uno:
   > Trabaja el issue #N (`gh issue view N`). Sigue AGENTS.md: rama `<rama>`, commits Conventional, skill `verificar` en verde, luego `git push -u origin <rama>` y `gh pr create --fill --body "Closes #N"`. Responde con el enlace del PR y lo que quedó pendiente.
4. Cuando todos terminen, resume: PR creado por issue, estado del CI (`gh pr checks`), y bloqueos. Recomienda el orden de merge (primero los que otros necesitan) y si conviene lanzar `qa` sobre algún PR.
