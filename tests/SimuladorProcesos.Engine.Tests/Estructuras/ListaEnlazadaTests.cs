using SimuladorProcesos.Engine.Estructuras;

namespace SimuladorProcesos.Engine.Tests.Estructuras;

public class ListaEnlazadaTests
{
    [Fact]
    public void Una_lista_nueva_esta_vacia()
    {
        var lista = new ListaEnlazada<int>();

        Assert.True(lista.EstaVacia);
        Assert.Equal(0, lista.Cantidad);
        Assert.Empty(lista);
    }

    [Fact]
    public void Con_un_elemento_la_lista_lo_contiene()
    {
        var lista = new ListaEnlazada<string>();

        lista.Agregar("a");

        Assert.False(lista.EstaVacia);
        Assert.Equal(1, lista.Cantidad);
        Assert.Equal(["a"], lista);
    }

    [Fact]
    public void Enumera_en_orden_de_insercion()
    {
        var lista = new ListaEnlazada<int>();
        for (var i = 1; i <= 5; i++)
        {
            lista.Agregar(i);
        }

        Assert.Equal(5, lista.Cantidad);
        Assert.Equal([1, 2, 3, 4, 5], lista);
        Assert.Equal([1, 2, 3, 4, 5], lista);
    }

    [Fact]
    public void Enumerar_con_la_interfaz_no_generica_devuelve_los_mismos_elementos()
    {
        var lista = new ListaEnlazada<int>();
        lista.Agregar(1);
        lista.Agregar(2);

        var elementos = new List<object?>();
        foreach (var elemento in (System.Collections.IEnumerable)lista)
        {
            elementos.Add(elemento);
        }

        Assert.Equal([1, 2], elementos.Cast<int>());
    }

    [Fact]
    public void Vaciar_deja_la_lista_vacia_y_se_puede_reutilizar()
    {
        var lista = new ListaEnlazada<int>();
        lista.Agregar(1);
        lista.Agregar(2);

        lista.Vaciar();

        Assert.True(lista.EstaVacia);
        Assert.Equal(0, lista.Cantidad);
        Assert.Empty(lista);

        // Si el puntero al último no se reiniciara, este Agregar se engancharía a un nodo viejo y se perdería.
        lista.Agregar(7);
        lista.Agregar(8);

        Assert.Equal([7, 8], lista);
        Assert.Equal(2, lista.Cantidad);
    }

    [Fact]
    public void Vaciar_sobre_una_lista_vacia_no_falla()
    {
        var lista = new ListaEnlazada<int>();

        lista.Vaciar();

        Assert.True(lista.EstaVacia);
    }

    [Fact]
    public void Vaciar_suelta_las_referencias_a_los_valores()
    {
        var lista = new ListaEnlazada<object>();
        var referencia = CrearYAgregarObjeto(lista);

        lista.Vaciar();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(referencia.TryGetTarget(out _));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<object> CrearYAgregarObjeto(ListaEnlazada<object> lista)
    {
        var valor = new object();
        lista.Agregar(valor);
        return new WeakReference<object>(valor);
    }

    [Fact]
    public void Agregar_durante_la_enumeracion_lanza_excepcion()
    {
        var lista = new ListaEnlazada<int>();
        lista.Agregar(1);
        lista.Agregar(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var elemento in lista)
            {
                lista.Agregar(elemento + 10);
            }
        });
    }

    [Fact]
    public void Agregar_durante_la_enumeracion_lanza_excepcion_incluso_en_el_ultimo_elemento()
    {
        var lista = new ListaEnlazada<int>();
        lista.Agregar(1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in lista)
            {
                lista.Agregar(2);
            }
        });
    }

    [Fact]
    public void Vaciar_durante_la_enumeracion_lanza_excepcion()
    {
        var lista = new ListaEnlazada<int>();
        lista.Agregar(1);
        lista.Agregar(2);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in lista)
            {
                lista.Vaciar();
            }
        });
    }

    [Fact]
    public void Maneja_diez_mil_elementos_en_orden()
    {
        const int total = 10_000;
        var lista = new ListaEnlazada<int>();

        for (var i = 0; i < total; i++)
        {
            lista.Agregar(i);
        }

        Assert.Equal(total, lista.Cantidad);
        Assert.Equal(Enumerable.Range(0, total), lista);
    }
}
