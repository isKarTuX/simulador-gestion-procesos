namespace SimuladorProcesos.Engine.Actividad;

/// <summary>
/// Dónde escribe cada proceso lo que copia; se inyecta para que las pruebas usen memoria y no disco.
/// El motor la llama desde el hilo del proceso que tiene la CPU, de a un carácter por vez.
/// </summary>
public interface IDestinoActividad
{
    /// <summary>El motor la llama en cada Iniciar: una corrida nueva empieza de cero y no se mezcla con el texto de la anterior.</summary>
    void NuevaCorrida();

    /// <summary>Agrega un carácter a la actividad del proceso; pid y nombre identifican su archivo.</summary>
    void Escribir(int pid, string nombre, char caracter);
}
