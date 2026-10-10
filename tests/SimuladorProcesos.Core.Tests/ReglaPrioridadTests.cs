using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Core.Reglas;

namespace SimuladorProcesos.Core.Tests;

public class ReglaPrioridadTests
{
    [Theory]
    [InlineData("SYSTEM")]
    [InlineData(@"NT AUTHORITY\SYSTEM")]
    [InlineData(@"NT AUTHORITY\LOCAL SERVICE")]
    [InlineData(@"NT AUTHORITY\NETWORK SERVICE")]
    [InlineData("system")]
    [InlineData("")]
    [InlineData(null)]
    public void Procesos_del_sistema_son_no_expulsivos(string? usuario) =>
        Assert.Equal(Prioridad.NoExpulsivo, ReglaPrioridad.Determinar(usuario));

    [Theory]
    [InlineData("juan")]
    [InlineData(@"DESKTOP-01\ktx")]
    public void Procesos_de_usuario_son_expulsivos(string usuario) =>
        Assert.Equal(Prioridad.Expulsivo, ReglaPrioridad.Determinar(usuario));

    [Fact]
    public void Los_valores_numericos_coinciden_con_el_enunciado()
    {
        Assert.Equal(0, (int)Prioridad.Expulsivo);
        Assert.Equal(1, (int)Prioridad.NoExpulsivo);
    }
}
