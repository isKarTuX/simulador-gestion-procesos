---
name: contrato-xml
description: Procedimiento obligatorio para cambiar el contrato entre módulos: XML del servicio REST, esquema de BD o interfaz pública ISimulador del motor. Úsalo antes de editar Core/Contrato, Data/SimuladorDbContext.cs o Engine/ISimulador.cs.
---

El contrato es lo que permite que varios agentes trabajen en paralelo sin romperse entre sí. Un cambio de contrato es un PR propio, pequeño y aislado.

1. **Justifica**: escribe en una frase qué módulo lo necesita y por qué el contrato actual no alcanza. Si existe una alternativa sin cambiar el contrato, úsala y detente aquí.
2. **Rama propia**: `contrato/<descripcion-corta>` desde `main`, sin mezclar otro trabajo.
3. **Actualiza en este orden y en el mismo PR**, todo lo que aplique:
   - CONTEXT.md §5 (fuente de verdad).
   - `docs/contrato/*.xml` (ejemplos).
   - `src/SimuladorProcesos.Core/Contrato/*` o `Data/SimuladorDbContext.cs` o `Engine/ISimulador.cs` y sus tipos.
   - Pruebas que serializan los ejemplos (`Core.Tests/ContratoXmlTests`) y las de Api.
   - Todos los consumidores para que la solución compile.
4. **Verifica** con la skill `verificar`.
5. **Avisa**: PR con etiqueta `contrato` y, en la descripción, la lista de módulos afectados. Comenta en los issues abiertos de esos módulos con el enlace al PR.
