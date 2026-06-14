using Bep.Modules.Organization.Domain;

namespace Bep.Modules.Organization.Domain.Tests;

public sealed class RutTests
{
    [Theory]
    [InlineData("76.000.001-9", "76000001-9")]
    [InlineData("76000001-9", "76000001-9")]
    [InlineData("760000019", "76000001-9")]
    public void Create_normalizes_valid_rut(string input, string expected)
    {
        var rut = Rut.Create(input);

        Assert.Equal(expected, rut.Value);
    }

    [Theory]
    [InlineData("76000001-5")] // dígito verificador incorrecto (el correcto es 9)
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc-1")]
    public void Create_rejects_invalid_rut(string input)
    {
        Assert.Throws<ArgumentException>(() => Rut.Create(input));
    }

    [Fact]
    public void Two_ruts_with_same_value_are_equal()
    {
        Assert.Equal(Rut.Create("76000001-9"), Rut.Create("76.000.001-9"));
    }
}
