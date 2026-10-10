using Microsoft.EntityFrameworkCore;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data.Tests;

public class EsquemaTests
{
    private static Catalogo NuevoCatalogo(int numero) => new()
    {
        Numero = numero,
        Nombre = $"Catálogo {numero}",
        Fecha = new DateTime(2026, 10, 10),
        Criterio = CriterioSeleccion.Memoria,
        Cantidad = 1,
        Procesos = [new ProcesoCatalogo { Pid = 4, Nombre = "System", Usuario = "SYSTEM", Descripcion = "NT Kernel", Prioridad = Prioridad.NoExpulsivo }],
    };

    [Fact]
    public void Guarda_y_lee_un_catalogo_con_sus_procesos()
    {
        using var bd = new BaseDatosEnMemoria();
        using (var contexto = bd.CrearContexto())
        {
            contexto.Catalogos.Add(NuevoCatalogo(1));
            contexto.SaveChanges();
        }

        using var lectura = bd.CrearContexto();
        var catalogo = lectura.Catalogos.Include(c => c.Procesos).Single();

        Assert.Equal(CriterioSeleccion.Memoria, catalogo.Criterio);
        Assert.Equal(Prioridad.NoExpulsivo, Assert.Single(catalogo.Procesos).Prioridad);
    }

    [Fact]
    public void El_numero_de_catalogo_es_unico()
    {
        using var bd = new BaseDatosEnMemoria();
        using var contexto = bd.CrearContexto();
        contexto.Catalogos.AddRange(NuevoCatalogo(7), NuevoCatalogo(7));

        Assert.Throws<DbUpdateException>(() => contexto.SaveChanges());
    }
}
