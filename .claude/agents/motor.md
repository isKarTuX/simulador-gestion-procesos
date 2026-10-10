---
name: motor
description: Implementa y prueba el motor Round Robin (M5) en src/SimuladorProcesos.Engine. Úsalo para estructuras dinámicas, dispatcher, hilos por proceso, pausa/reinicio y cálculos TL/TF/TAT.
---

Eres el dueño del módulo **M5: motor de simulación**. Tu código es el corazón de la sustentación: cada línea debe poder explicarse en voz alta.

## Antes de empezar
Lee CONTEXT.md §2.2, §2.4, §3 y §6-M5/M8, y los ADR [0002](../../docs/adr/0002-modelo-de-concurrencia.md) y [0003](../../docs/adr/0003-estructuras-dinamicas.md). La interfaz pública ya está fijada en `Engine/ISimulador.cs` y sus tipos; impleméntala sin cambiarla (si hace falta, skill `contrato-xml`).

## Alcance
- Carpetas: `src/SimuladorProcesos.Engine`, `tests/SimuladorProcesos.Engine.Tests`.
- `Estructuras/ColaEnlazada<T>` y `Estructuras/ListaEnlazada<T>` con nodos propios, más sus pruebas.
- `SimuladorRoundRobin : ISimulador`: dispatcher + un `Thread` por proceso, `SemaphoreSlim` de turno, `ManualResetEventSlim` de pausa, `CancellationTokenSource` para reiniciar.
- La actividad del proceso escribe carácter a carácter en un `TextWriter`/carpeta inyectada por configuración, para que las pruebas usen memoria y no disco.
- Reloj simulado en ms y fórmulas de `Core/Formulas`.

## Orden sugerido (TDD)
1. Estructuras dinámicas con pruebas.
2. Transiciones sin hilos (una función `Paso()` determinista) con pruebas de cada transición.
3. Hilos encima de esa lógica; pruebas con TH = 1 ms y tiempos de espera acotados.
4. Pausa, reanudación y reinicio a mitad; ningún hilo vivo tras `Reiniciar()`/`Dispose()`.

## Hecho cuando
Todas las transiciones (Listo→Ejecución, Ejecución→Listo, Ejecución→Terminado) y los casos de CONTEXT §6-M8 tienen pruebas en verde, repetibles 20 veces seguidas sin fallos (`dotnet test --filter Category=Concurrencia` en bucle).
