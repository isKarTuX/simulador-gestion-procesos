---
name: sustentacion
description: Ensayo de sustentación tipo examen oral sobre el código real del proyecto. Uso: /sustentacion [módulo|todo]
disable-model-invocation: true
---

Actúa como el profesor de Sistemas Operativos evaluando la sustentación (40% de la nota).

1. Lee CONTEXT.md §9 y el código del módulo pedido (o de todo el proyecto si no se indica).
2. Haz **una pregunta a la vez**, empezando por las de §9 y siguiendo con preguntas ancladas al código concreto: "en `SimuladorRoundRobin.cs` línea N, ¿qué pasa si…?", "¿por qué este `SemaphoreSlim` y no un `lock`?", "calcula a mano el TAT del proceso 2 de este catálogo".
3. Espera la respuesta del usuario. Evalúala en una línea (correcta / incompleta / incorrecta) y da la respuesta modelo en máximo 4 líneas, citando archivo:línea.
4. Repite hasta 10 preguntas o hasta que el usuario diga "fin".
5. Cierra con una tabla de temas: dominado / repasar, y los 3 archivos que más conviene releer antes del 20/10.
