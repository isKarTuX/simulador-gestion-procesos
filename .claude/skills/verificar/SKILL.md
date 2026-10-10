---
name: verificar
description: Compuerta de calidad antes de commit, PR o de dar una tarea por terminada en este repo. Compila, formatea, prueba y revisa las reglas de AGENTS.md.
---

Ejecuta los pasos en orden (si `dotnet` no está en el PATH, mira "Arranque de sesión" en AGENTS.md). Si uno falla, corrígelo y vuelve a empezar desde ese paso. Termina solo cuando los cinco estén en verde.

1. **Formato**: `dotnet format SimuladorProcesos.slnx` y luego `dotnet format SimuladorProcesos.slnx --verify-no-changes` sale con código 0.
2. **Compilación**: `dotnet build SimuladorProcesos.slnx` termina con 0 advertencias y 0 errores.
3. **Pruebas**: `dotnet test SimuladorProcesos.slnx` con todas en verde. Si tocaste `Engine`, repite `dotnet test tests/SimuladorProcesos.Engine.Tests` 5 veces: una sola falla intermitente es una condición de carrera, no "mala suerte".
4. **Reglas**: revisa el diff (`git diff main...HEAD`) contra cada regla dura de AGENTS.md: dependencias entre proyectos (`dotnet list <csproj> reference`), estructuras propias en Engine, un `Thread` por proceso, nombres en español, archivos solo dentro de tu carpeta. Si el diff toca `Core/Contrato`, `Data/SimuladorDbContext.cs` o `Engine/ISimulador.cs` sin pasar por la skill `contrato-xml`, detente y aplícala.
5. **Alcance**: `git diff --name-only origin/main...HEAD` contiene solo archivos que tu subtarea de `docs/PROMPTS.md` declara como tuyos, y el PR dirá `Closes #<issue>`.

Reporta al final una línea por paso con ✅/❌.
