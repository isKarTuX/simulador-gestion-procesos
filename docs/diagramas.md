# Diagramas

GitHub renderiza estos diagramas Mermaid. Para la sustentación se pueden exportar a PNG desde [mermaid.live](https://mermaid.live).

Versiones PNG generadas con `@mermaid-js/mermaid-cli@12.0.0` (fondo blanco, escala 2): [estados](img/diagrama-estados.png) · [arquitectura](img/diagrama-arquitectura.png) · [secuencia](img/diagrama-secuencia.png).

## 1. Modelo de estados

![Modelo de estados](img/diagrama-estados.png)

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Listo: llega (TL)
    Listo --> Ejecucion: CPU libre (dispatcher)
    Ejecucion --> Listo: expulsivo y agotó su quantum
    Ejecucion --> Terminado: copió toda la descripción
    Terminado --> [*]

    note right of Listo
        "Espera" en la GUI = procesos de Listo
        que ya usaron la CPU al menos una vez
    end note
```

## 2. Arquitectura

![Arquitectura](img/diagrama-arquitectura.png)

```mermaid
flowchart LR
    subgraph App1["App 1 · Escritorio (WPF)"]
        R[Recolector SO<br/>Process + WMI] --> C[Catálogos]
        C --> A[Actividad<br/>archivo .txt]
    end
    DB[(SQLite<br/>simulador.db)]
    subgraph Api["Servicio REST (ASP.NET Core)"]
        E[Endpoints XML]
    end
    subgraph App2["App 2 · Web (Blazor Server)"]
        CL[ClienteCatalogos<br/>HTTP + XML] --> M[Motor Round Robin<br/>dispatcher + hilos]
        M -- eventos --> G[GUI en vivo<br/>4 bandas]
        M --> RP[Reportes + Chart.js]
    end
    C -- EF Core --> DB
    E -- EF Core --> DB
    CL -- GET /api/... XML --> E
```

## 3. Secuencia del dispatcher (un turno)

![Secuencia del dispatcher](img/diagrama-secuencia.png)

```mermaid
sequenceDiagram
    autonumber
    participant D as Dispatcher
    participant L as Listos (cola)
    participant P as Hilo del proceso
    participant UI as GUI

    D->>D: esperar compuerta de pausa
    D->>L: Desencolar()
    L-->>D: proceso p
    D->>UI: CambioEstado(p: Listo → Ejecución)
    D->>P: turno.Release()
    loop hasta quantum (expulsivo) o fin (no expulsivo)
        P->>P: esperar compuerta de pausa
        P->>P: copiar 1 carácter, reloj += TH, Sleep(TH)
    end
    P->>D: turnoTerminado.Release()
    alt completó la descripción
        D->>UI: CambioEstado(p: Ejecución → Terminado)
    else expulsivo con caracteres pendientes
        D->>L: Encolar(p)
        D->>UI: CambioEstado(p: Ejecución → Listo)
    end
```
