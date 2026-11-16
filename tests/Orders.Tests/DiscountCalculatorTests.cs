using Orders.Services;

namespace Orders.Tests;

public class DiscountCalculatorTests
{
    readonly DiscountCalculator _sut = new();

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(999.99, 0.0)]
    [InlineData(1000.0, 100.0)]
    [InlineData(2000.0, 200.0)]
    public void Calculate_ReturnsExpected(double total, double expected)
    {
        var actual = _sut.Calculate((decimal)total);

        Assert.Equal((decimal)expected, actual);
    }

    [Fact]
    public void Calculate_NegativeTotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Calculate(-1m));
    }
}
