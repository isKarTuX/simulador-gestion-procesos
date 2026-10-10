# Simulador de Gestión de Procesos

[![CI](https://github.com/isKarTuX/simulador-gestion-procesos/actions/workflows/ci.yml/badge.svg)](https://github.com/isKarTuX/simulador-gestion-procesos/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Round Robin](https://img.shields.io/badge/planificaci%C3%B3n-Round%20Robin-2ea44f)

Simulador del **modelo de estados de un proceso** y del algoritmo de planificación **Round Robin**, desarrollado para el laboratorio *Gestión de procesos* del curso de Sistemas Operativos (Ingeniería de Sistemas, Universidad de Córdoba, 2026-II).

El sistema captura procesos reales del sistema operativo, los guarda en catálogos, los expone por un servicio REST en XML y los simula en una aplicación web donde cada proceso es un hilo que compite por la CPU.

```mermaid
flowchart LR
    A["App 1 · Escritorio<br/>captura procesos del SO"] -->|guarda catálogos| DB[(SQLite)]
    DB --> API["Servicio REST<br/>XML"]
    API -->|HTTP/XML| W["App 2 · Web<br/>motor Round Robin + GUI en vivo"]
```

## Funcionalidades

**App 1 · Escritorio (WPF)**
- Captura N procesos en ejecución, ordenados por mayor uso de CPU o de memoria.
- Obtiene PID, nombre, usuario, descripción y prioridad (procesos del SO = no expulsivos).
- Agrupa los procesos en catálogos numerados y los persiste en base de datos.
- Ejecuta la actividad de cada proceso: escribir su descripción en un archivo, carácter por carácter.

**Servicio REST (ASP.NET Core)**
- `GET /api/catalogos` · `GET /api/catalogos/{id}/procesos` · `GET /api/planificacion/{id}`, todos en XML.

**App 2 · Web (Blazor Server)**
- Simulación Round Robin con un hilo por proceso y estados Listo, Ejecución y Terminado.
- Panel en vivo con las bandas Listos, Ejecución, Espera y Terminado.
- Iniciar, pausar, reanudar y reiniciar en cualquier momento.
- Reportes: listado de ejecución, listado de no expulsivos y gráfico Proceso vs TurnaRound.

## Estructura

```
├─ src/
│  ├─ SimuladorProcesos.Core/      modelos, contrato XML, fórmulas (sin dependencias)
│  ├─ SimuladorProcesos.Data/      EF Core + SQLite
│  ├─ SimuladorProcesos.Desktop/   App 1 (WPF)
│  ├─ SimuladorProcesos.Api/       servicio REST XML
│  ├─ SimuladorProcesos.Engine/    motor Round Robin, estructuras dinámicas, hilos
│  └─ SimuladorProcesos.Web/       App 2 (Blazor Server)
├─ tests/                          xUnit: Core, Engine, Api
├─ docs/
│  ├─ PLAN.md                      olas de trabajo, metas y restricciones
│  ├─ diagramas.md                 estados, arquitectura, secuencia del dispatcher
│  ├─ adr/                         decisiones de arquitectura
│  └─ contrato/                    ejemplos XML del servicio
├─ CONTEXT.md                      requisitos, contrato y decisiones (fuente de verdad)
└─ AGENTS.md                       reglas de trabajo para personas y agentes de IA
```

## Cómo ejecutarlo

Requisitos: Windows 10/11 y [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build SimuladorProcesos.slnx
```

```bash
dotnet run --project src/SimuladorProcesos.Desktop
```

```bash
dotnet run --project src/SimuladorProcesos.Api
```

```bash
dotnet run --project src/SimuladorProcesos.Web
```

1. En la App de escritorio crea un catálogo (N procesos, criterio CPU o memoria).
2. Con la Api corriendo (`http://localhost:5080`), abre la Web (`http://localhost:5090`).
3. Elige el catálogo, define TH y quantum, e inicia la simulación.

Pruebas:

```bash
dotnet test SimuladorProcesos.slnx
```

## Documentación

- [Requisitos, contrato y decisiones](CONTEXT.md)
- [Plan de trabajo](docs/PLAN.md) · [Prompts por subtarea para agentes](docs/PROMPTS.md)
- [Diagramas](docs/diagramas.md)
- Decisiones: [arquitectura y stack](docs/adr/0001-arquitectura-y-stack.md) · [concurrencia](docs/adr/0002-modelo-de-concurrencia.md) · [estructuras dinámicas](docs/adr/0003-estructuras-dinamicas.md)

## Uso de IA

Este proyecto se desarrolla con apoyo de agentes de IA (Claude Code) bajo las reglas de [AGENTS.md](AGENTS.md). Los commits asistidos llevan la línea `Co-Authored-By` correspondiente y todo el código es revisado y sustentado por el equipo.
