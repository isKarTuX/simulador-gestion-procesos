# ADR-0003: Estructuras dinámicas por estado

- **Estado**: aceptado
- **Fecha**: 2026-10-10

## Contexto

El enunciado pide "estructura de datos dinámica para cada estado". Usar `List<T>` cumple en lo técnico, pero no demuestra el concepto en la sustentación.

## Decisión

Estructuras propias en `Engine/Estructuras`, con nodos enlazados escritos por nosotros:

| Estado | Estructura | Por qué |
|---|---|---|
| Listo | `ColaEnlazada<T>` (FIFO con punteros `cabeza` y `cola`) | Round Robin atiende en orden de llegada y reencola al final: `Encolar`/`Desencolar` en O(1) |
| Ejecución | Slot único (`ProcesoSimulado?`) | Una sola CPU: a lo sumo un proceso |
| Terminado | `ListaEnlazada<T>` (inserción al final) | Solo crece; se recorre para los reportes en orden de finalización |
| Espera | Vista derivada de Listo | Ver CONTEXT §3, duda 1: procesos de Listo con `Ejecuciones > 0` |

Ambas implementan `IEnumerable<T>` para que la UI obtenga instantáneas, pero se modifican solo dentro del lock del motor.

## Consecuencias

- Hay que probarlas (vacía, un elemento, muchos, desencolar de vacía lanza excepción).
- En la sustentación se puede dibujar cada estructura y su operación en la pizarra.
