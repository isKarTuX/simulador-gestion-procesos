# Lanzamiento de las olas 2 y 3

Guía para lanzar los agentes que faltan en sesiones propias y dejar la verificación a la sesión principal. Los prompts completos de cada subtarea siguen en [`PROMPTS.md`](PROMPTS.md); aquí están el orden, un preámbulo común y los ajustes que aparecieron al fusionar la ola 1.

## Estado (main ya incluye)

Subtareas 1.1 a 1.12 y 2.1 (motor con hilos). Revisadas por QA (#47–#51 corregidos). Falta lo de abajo.

## Orden

| Cuándo | Subtarea | Issue | Agente | Dónde conviene lanzarla |
|---|---|---|---|---|
| Ya | **2.2** App 1 con servicios reales | #25 | `desktop` | **En tu Windows**: pide abrir WPF y tomar capturas |
| Ya | **2.3** Web con motor real y REST | #7 | `frontend` | Cualquiera (se prueba en Chromium) |
| Ya | **3.4** Notas de sustentación: motor | #29 | `motor` | Cualquiera |
| Tras fusionar 2.3 | **3.1** Tablas de reporte | #26 | `frontend` | Cualquiera |
| Tras fusionar 2.3 | **3.2** Gráfico Chart.js | #27 | `frontend` | Cualquiera |
| Tras fusionar 2.3 | **3.5** Notas: App 2 web | #30 | `frontend` | Cualquiera |
| Tras fusionar 2.2 | **3.3** Notas: App 1 y servicio | #28 | `backend` | Cualquiera |
| Tras fusionar 3.1 y 3.2 | **3.6** README, capturas, diagramas | #31 | `frontend` | Donde tengas las dos apps |
| Tras fusionar 2.2 y 2.3 | **3.7** QA punta a punta | #32 | `qa` | En tu Windows (App 1 real) |
| Después | Ola 4: `/sustentacion todo` y prompt de cierre | | | Sesión principal |

Lanza de una en una o en tandas de 2–3 para no gastar el límite de uso; no hace falta esperar a que termine una para lanzar otra que no dependa de ella.

## Cómo lanzar cada una

1. Sesión nueva sobre este repo con **worktree activado**.
2. Pega el **preámbulo común** (abajo).
3. Pega el bloque de la subtarea desde [`PROMPTS.md`](PROMPTS.md) (sección «Subtarea N.N») seguido de sus **ajustes** (abajo).
4. Cuando abra el PR, vuelve a la sesión principal y pega el **mensaje de verificación**.

## Preámbulo común

```text
Contexto: main ya tiene las subtareas 1.1 a 1.12 y 2.1 fusionadas. Antes de escribir código lee AGENTS.md, tu rol en .claude/agents/ y tu subtarea en docs/PROMPTS.md. Trabaja solo en los archivos que tu subtarea declara.

Reglas aprendidas en la ola 1:
- Crea tu rama desde origin/main actualizado y NO fusiones: el humano hace el squash merge.
- El CI (Compilar y probar) corre en windows-latest y es la referencia. Antes de decir "hecho" espera su conclusión real sobre el último commit; si falla, lee el log, corrige y vuelve a esperar. No reportes verde por pruebas locales.
- No adivines: si algo no se pudo verificar (ventana WPF, navegador, Windows), dilo en el PR.
- Si necesitas tocar un archivo fuera de tu alcance, avísalo en el cuerpo del PR (hay un caso aceptado: App.razor, ya resuelto en main).
- Termina con: enlace del PR, conclusión real del CI, archivos tocados (marca los fuera de alcance), pendientes y 3 líneas "para la sustentación".
- Capturas: no se pueden adjuntar desde un agente; guárdalas y dime la ruta, o descríbelas en el PR.
```

## Ajustes por subtarea

**2.2 · App 1 con servicios reales** (`desktop`, en Windows). Además de su prompt:
- `ActividadProceso` ya nombra los archivos `<nombre>_<pid>.txt`; la nota de 1.6 que dice `<nombre>.txt` está desactualizada.
- En `main`, `CatalogoViewModel` recibe `IRecolectorProcesos`, `ICatalogoRepositorio`, `IActividadProceso` y una `carpetaActividad` opcional; los reales son `RecolectorProcesos` (con `LectorProcesosSistema` y `ProveedorMetadatosWindows`), `CatalogoRepositorio` y `ActividadProceso`.
- Las casillas de CONTEXT §6 que siguen sin marcar para M2 (formulario, grid, guardar en BD) las cierra esta subtarea con las capturas del PR.

**2.3 · Web con motor real y REST** (`frontend`). Además de su prompt:
- El motor real ya está en `main`: `new SimuladorRoundRobin(IDestinoActividad? destino = null)`. Por defecto escribe en `RutasSimulador.CarpetaActividad/web`; dos pestañas a la vez escribirían los mismos archivos. Decide un destino por circuito (carpeta con el id del circuito) o `DestinoEnMemoria`, y justifícalo en el PR.
- `Pausar`, `Reanudar` e `Iniciar` lanzan `InvalidOperationException` en estados donde no aplican (p. ej. Pausar en Finalizada). Deshabilita los botones según `EstadoSimulacion` y atrapa esa excepción en el manejador del clic: la simulación puede terminar entre el render y el clic.
- Los manejadores de `CambioEstado` se invocan desde hilos del motor; una excepción en tu manejador se ignora (el motor la contiene). Aun así usa `InvokeAsync` y desuscribe en `Dispose`.
- Un fallo interno inesperado deja el motor en `Detenida` sin aviso: si la simulación vuelve a `Detenida` sin que el usuario pulsara Reiniciar, muestra un aviso.
- `SimuladorFalso` y `CatalogoDeEjemplo` quedan solo detrás de la opción `UsarSimuladorFalso` de appsettings, que crea esta subtarea (todavía no existe en el código).
- Usa `MapeoEntrada.AProcesosEntrada` y atrapa `ErrorServicioException` (503 = Api apagada).

**3.4 · Notas del motor** (`motor`). Cita en las preguntas de sustentación lo aprendido de la QA: pausa re-comprobada dentro del lock (`BajoLockSinPausa`), eventos fuera del lock, `Reiniciar` atómico (`DescartarCorrida`), contención de excepciones y el token en `IDestinoActividad.Escribir`. Fuentes: PR #46 y los issues #47–#51.

**3.1, 3.2, 3.5, 3.3, 3.6, 3.7**: sin ajustes más allá del preámbulo. 3.7 debe correr el checklist de CONTEXT §11 en tu Windows con App 1, Api y Web reales.

## Mensaje de verificación (sesión principal)

Pégalo cuando uno o varios agentes hayan abierto su PR:

```text
Verifica los PR abiertos de la ola <2 o 3>. Para cada uno: conclusión real del CI sobre la punta actual, conflictos con main, archivos fuera de los que su subtarea declara en docs/PROMPTS.md, resumen de 3 líneas, y fallos que veas leyendo el código (el motor y lo concurrente, línea por línea). Reproduce las pruebas en local cuando sea posible. Recomienda el orden de merge y qué PR pasa por qa. No fusiones nada sin mi OK; cuando te diga «fusiona X», haz squash merge, marca las casillas de CONTEXT §6 que correspondan en commits aparte y dime qué ramas deben hacer rebase.
```
