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

    [Fact]
    public void Equals_ShouldReturnFalse_WhenObjIsNull()
    {
        var cpf = Cpf.Create("52998224725");
        cpf.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenObjIsNotCpf()
    {
        var cpf = Cpf.Create("52998224725");
        cpf.Equals("52998224725").Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenValuesAreDifferent()
    {
        var cpf1 = Cpf.Create("52998224725");
        var cpf2 = Cpf.Create("111.444.777-35");
        cpf1.Equals(cpf2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeEqual_ForCpfsWithSameValue()
    {
        var cpf1 = Cpf.Create("529.982.247-25");
        var cpf2 = Cpf.Create("52998224725");
        cpf1.GetHashCode().Should().Be(cpf2.GetHashCode());
    }

    [Theory]
    [InlineData("98765432110")] // remainder=0 < 2 → first=0, but digit is 1
    [InlineData("52998224735")] // remainder=9 ≥ 2 → first=11-9=2, but digit is 3
    public void IsValid_ShouldReturnFalse_WhenFirstCheckDigitIsWrong(string cpf)
    {
        Cpf.IsValid(cpf).Should().BeFalse();
    }

    [Theory]
    [InlineData("98765432101")] // remainder=1 < 2 → second=0, but digit is 1
    [InlineData("53998224745")] // remainder=9 ≥ 2 → second=11-9=2, but digit is 5
    public void IsValid_ShouldReturnFalse_WhenSecondCheckDigitIsWrong(string cpf)
    {
        Cpf.IsValid(cpf).Should().BeFalse();
    }
}
