using SimuladorProcesos.Engine.Estructuras;

namespace SimuladorProcesos.Engine.Tests.Estructuras;

public class ColaEnlazadaTests
{
    [Fact]
    public void Una_cola_nueva_esta_vacia()
    {
        var cola = new ColaEnlazada<int>();

        Assert.True(cola.EstaVacia);
        Assert.Equal(0, cola.Cantidad);
        Assert.Empty(cola);
    }

    [Fact]
    public void Desencolar_de_una_cola_vacia_lanza_excepcion_en_espanol()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ColaEnlazada<int>().Desencolar());

        Assert.Contains("vacía", ex.Message);
    }

    [Fact]
    public void VerFrente_de_una_cola_vacia_lanza_excepcion_en_espanol()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ColaEnlazada<int>().VerFrente());

        Assert.Contains("vacía", ex.Message);
    }

    [Fact]
    public void Con_un_elemento_frente_y_desencolar_lo_devuelven_y_la_cola_queda_vacia()
    {
        var cola = new ColaEnlazada<string>();
        cola.Encolar("a");

        Assert.False(cola.EstaVacia);
        Assert.Equal(1, cola.Cantidad);
        Assert.Equal("a", cola.VerFrente());
        Assert.Equal(1, cola.Cantidad);
        Assert.Equal("a", cola.Desencolar());
        Assert.True(cola.EstaVacia);
        Assert.Equal(0, cola.Cantidad);
    }

    [Fact]
    public void Desencola_en_orden_FIFO()
    {
        var cola = new ColaEnlazada<int>();
        for (var i = 1; i <= 5; i++)
        {
            cola.Encolar(i);
        }

        Assert.Equal(5, cola.Cantidad);
        for (var i = 1; i <= 5; i++)
        {
            Assert.Equal(i, cola.Desencolar());
        }

        Assert.True(cola.EstaVacia);
    }

    [Fact]
    public void VerFrente_no_quita_el_elemento()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);

        Assert.Equal(1, cola.VerFrente());
        Assert.Equal(1, cola.VerFrente());
        Assert.Equal(2, cola.Cantidad);
    }

    [Fact]
    public void Intercalar_encolar_y_desencolar_mantiene_el_orden_y_los_punteros()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);
        Assert.Equal(1, cola.Desencolar());
        cola.Encolar(3);
        Assert.Equal(2, cola.Desencolar());
        Assert.Equal(3, cola.Desencolar());

        // Al quedar vacía el puntero a la cola debe reiniciarse: si no, este Encolar se perdería.
        cola.Encolar(4);
        cola.Encolar(5);

        Assert.Equal([4, 5], cola);
        Assert.Equal(2, cola.Cantidad);
    }

    [Fact]
    public void Simula_el_ciclo_de_Round_Robin_sacar_el_primero_y_reencolarlo_al_final()
    {
        var cola = new ColaEnlazada<string>();
        cola.Encolar("A");
        cola.Encolar("B");
        cola.Encolar("C");

        cola.Encolar(cola.Desencolar());
        cola.Encolar(cola.Desencolar());

        Assert.Equal(["C", "A", "B"], cola);
    }

    [Fact]
    public void Vaciar_deja_la_cola_vacia_y_se_puede_reutilizar()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);

        cola.Vaciar();

        Assert.True(cola.EstaVacia);
        Assert.Equal(0, cola.Cantidad);
        Assert.Empty(cola);
        Assert.Throws<InvalidOperationException>(() => cola.Desencolar());
        Assert.Throws<InvalidOperationException>(() => cola.VerFrente());

        cola.Encolar(9);
        cola.Encolar(10);

        Assert.Equal([9, 10], cola);
        Assert.Equal(9, cola.Desencolar());
    }

    [Fact]
    public void Vaciar_sobre_una_cola_vacia_no_falla()
    {
        var cola = new ColaEnlazada<int>();

        cola.Vaciar();

        Assert.True(cola.EstaVacia);
    }

    [Fact]
    public void Vaciar_suelta_las_referencias_a_los_valores()
    {
        var cola = new ColaEnlazada<object>();
        var referencia = CrearYEncolarObjeto(cola);

        cola.Vaciar();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(referencia.TryGetTarget(out _));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<object> CrearYEncolarObjeto(ColaEnlazada<object> cola)
    {
        var valor = new object();
        cola.Encolar(valor);
        return new WeakReference<object>(valor);
    }

    [Fact]
    public void Enumera_del_frente_al_final_sin_modificar_la_cola()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);
        cola.Encolar(3);

        Assert.Equal([1, 2, 3], cola);
        Assert.Equal([1, 2, 3], cola);
        Assert.Equal(3, cola.Cantidad);
    }

    [Fact]
    public void Enumerar_con_la_interfaz_no_generica_devuelve_los_mismos_elementos()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);

        var elementos = new List<object?>();
        foreach (var elemento in (System.Collections.IEnumerable)cola)
        {
            elementos.Add(elemento);
        }

        Assert.Equal([1, 2], elementos.Cast<int>());
    }

    [Fact]
    public void Encolar_durante_la_enumeracion_lanza_excepcion()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var elemento in cola)
            {
                cola.Encolar(elemento + 10);
            }
        });
    }

    [Fact]
    public void Desencolar_durante_la_enumeracion_lanza_excepcion_incluso_en_el_ultimo_elemento()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in cola)
            {
                cola.Desencolar();
            }
        });
    }

    [Fact]
    public void Vaciar_durante_la_enumeracion_lanza_excepcion()
    {
        var cola = new ColaEnlazada<int>();
        cola.Encolar(1);
        cola.Encolar(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in cola)
            {
                cola.Vaciar();
            }
        });
    }

    [Fact]
    public void Maneja_diez_mil_elementos_en_orden()
    {
        const int total = 10_000;
        var cola = new ColaEnlazada<int>();

        for (var i = 0; i < total; i++)
        {
            cola.Encolar(i);
        }

        Assert.Equal(total, cola.Cantidad);
        Assert.Equal(total, cola.Count());
        for (var i = 0; i < total; i++)
        {
            Assert.Equal(i, cola.Desencolar());
        }

        Assert.True(cola.EstaVacia);
    }
}
