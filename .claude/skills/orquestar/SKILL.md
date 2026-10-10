---
name: orquestar
description: Lanza en paralelo una tanda de subtareas de docs/PROMPTS.md, cada una como subagente en su worktree. Uso: /orquestar [ola o lista de subtareas, p. ej. "1" o "1.4 1.5 1.9"]
disable-model-invocation: true
---

1. **Base al día**: `git switch main && git pull`. Los worktrees de los subagentes nacen de aquí; si `main` está viejo, nacen viejos.
2. **Elegir subtareas**: si el argumento es una lista, úsala. Si es una ola, toma de `docs/PROMPTS.md` las subtareas de esa ola cuyo issue siga abierto (`gh issue list --state open --label ola:<n> --json number,title,labels`; ignora las de etiqueta `epica`) y cuyas dependencias estén cerradas o, si el prompt lo permite, ya tengan PR abierto. Máximo 6 a la vez; si hay más, propone tandas en el orden de la sección de esa ola.
3. **Confirmar**: muestra una tabla (subtarea, issue, agente, rama, dependencias) y espera el OK del usuario.
4. **Lanzar**: en un solo mensaje, un subagente por subtarea con `subagent_type` = agente de la subtarea, `isolation: "worktree"`, en segundo plano, y como `prompt` el bloque completo de esa subtarea copiado tal cual de `docs/PROMPTS.md`.
5. **Resumir** cuando terminen: tabla con subtarea, enlace del PR, estado del CI (`gh pr checks <n>`), archivos tocados fuera de su alcance (`gh pr diff <n> --name-only`) y pendientes. Recomienda el orden de merge (primero lo que otras subtareas necesitan) y qué PR conviene pasar por `qa`.
