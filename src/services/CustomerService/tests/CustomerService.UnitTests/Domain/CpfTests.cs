using CustomerService.Domain.ValueObjects;
using FluentAssertions;

namespace CustomerService.UnitTests.Domain;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void IsValid_ShouldReturnTrue_ForValidCpfs(string cpf)
    {
        Cpf.IsValid(cpf).Should().BeTrue();
    }

    [Theory]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    [InlineData("123.456.789-00")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("123")]
    public void IsValid_ShouldReturnFalse_ForInvalidCpfs(string? cpf)
    {
        Cpf.IsValid(cpf).Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldNormalize_RemovingNonDigits()
    {
        var cpf = Cpf.Create("529.982.247-25");
        cpf.Value.Should().Be("52998224725");
    }

    [Fact]
    public void Create_ShouldThrow_ForInvalidCpf()
    {
        var act = () => Cpf.Create("000.000.000-00");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TwoCpfsWithSameValue_ShouldBeEqual()
    {
        var cpf1 = Cpf.Create("529.982.247-25");
        var cpf2 = Cpf.Create("52998224725");
        cpf1.Should().Be(cpf2);
    }

    [Theory]
    [InlineData("52998224735")] // first check digit changed from 2 to 3
    [InlineData("52998224745")] // first check digit changed from 2 to 4
    public void IsValid_ShouldReturnFalse_WhenFirstCheckDigitIsWrong(string cpf)
    {
        Cpf.IsValid(cpf).Should().BeFalse();
    }

    [Theory]
    [InlineData("52998224726")] // second check digit changed from 5 to 6
    [InlineData("52998224727")] // second check digit changed from 5 to 7
    public void IsValid_ShouldReturnFalse_WhenSecondCheckDigitIsWrong(string cpf)
    {
        Cpf.IsValid(cpf).Should().BeFalse();
    }
}
