using System.Text;
using Bep.Modules.Laboratory.Application.Parsing;

namespace Bep.IntegrationTests.Laboratory;

/// <summary>
/// Pruebas del parser CSV de resultados (RF-04-001): cabecera, conversión numérica
/// con cultura invariante, columna método opcional y reporte de filas inválidas sin
/// invalidar el resto del lote.
/// </summary>
public sealed class LaboratoryParserTests
{
    private static MemoryStream Csv(string contenido) => new(Encoding.UTF8.GetBytes(contenido));

    [Fact]
    public void Parse_lee_filas_validas_y_metodo_opcional()
    {
        const string csv = """
            codigo_muestra,parametro,valor,unidad,metodo
            MTR-20260601-AAAAA00001,Oxígeno disuelto,8.4,mg/L,SM 4500-O
            MTR-20260601-AAAAA00001,pH,7.9,pH
            """;

        var result = new CsvResultadosParser().Parse(Csv(csv));

        Assert.Equal(2, result.Resultados.Count);
        Assert.Empty(result.Errores);
        Assert.Equal(8.4, result.Resultados[0].Valor);
        Assert.Equal("SM 4500-O", result.Resultados[0].Metodo);
        Assert.Null(result.Resultados[1].Metodo);
    }

    [Fact]
    public void Parse_reporta_filas_invalidas_sin_descartar_el_lote()
    {
        const string csv = """
            codigo_muestra,parametro,valor,unidad,metodo
            MTR-1,Turbidez,no-numero,NTU
            MTR-1,Turbidez,2.1,NTU
            ,Vacio,3,NTU
            """;

        var result = new CsvResultadosParser().Parse(Csv(csv));

        Assert.Single(result.Resultados);
        Assert.Equal(2, result.Errores.Count); // valor no numérico + código vacío
    }

    [Fact]
    public void Parse_archivo_solo_con_cabecera_no_produce_resultados()
    {
        var result = new CsvResultadosParser().Parse(Csv("codigo_muestra,parametro,valor,unidad,metodo\n"));

        Assert.Empty(result.Resultados);
        Assert.NotEmpty(result.Errores);
    }
}
