# ADR-0001: Arquitectura y stack

- **Estado**: aceptado
- **Fecha**: 2026-10-10

## Contexto

El enunciado pide dos aplicaciones (escritorio y web), un servicio REST que responda XML, catálogos en base de datos, hilos por proceso y una GUI que se actualice en vivo. Hay 10 días y la sustentación vale 40%: el stack debe ser uno solo y fácil de explicar.

## Decisión

- **C# / .NET 10 (LTS)** para todo. Se elige .NET 10 y no .NET 8 porque el soporte de .NET 8 termina el 10/11/2026, y las APIs usadas son las mismas.
- Seis proyectos con dependencias en una sola dirección:

| Proyecto | Tipo | Responsabilidad |
|---|---|---|
| `Core` | Biblioteca | Modelos de dominio, DTO del contrato XML, fórmulas y reglas puras |
| `Data` | Biblioteca | EF Core + SQLite: `SimuladorDbContext` y repositorios |
| `Desktop` | WPF | App 1: recolector, catálogos, actividad |
| `Api` | ASP.NET Core Web API | Servicio REST XML sobre la misma BD |
| `Engine` | Biblioteca | Motor Round Robin, sin dependencias de UI ni red |
| `Web` | Blazor Server | App 2: GUI en vivo y reportes |

- **SQLite** como BD: un archivo, sin instalación; ambas apps lo encuentran en `%LOCALAPPDATA%\SimuladorProcesos\simulador.db`.
- **Api como proceso aparte** (no embebida en el escritorio): se puede probar sola y la App 2 no depende de que la App 1 esté abierta.
- **Blazor Server** en lugar de un hub SignalR propio: Blazor Server ya mantiene un circuito SignalR por pestaña; el componente se suscribe a los eventos del motor y re-renderiza. Menos piezas que explicar.
- **La Web lee los catálogos solo por HTTP/XML** desde la Api, como pide el enunciado ("la información de cada proceso será obtenida mediante la utilización del servicio").

## Consecuencias

- Requiere Windows para la App 1 (WMI, WPF); Api, Engine y Web son multiplataforma.
- El motor se prueba sin UI ni red, lo que permite TDD y paralelizar el trabajo.
- Un solo lenguaje: cualquier integrante puede leer cualquier módulo en la sustentación.
