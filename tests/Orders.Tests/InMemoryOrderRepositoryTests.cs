using Orders.Models;
using Orders.Repositories;

namespace Orders.Tests;

public class InMemoryOrderRepositoryTests
{
    readonly InMemoryOrderRepository _sut = new();

    [Fact]
    public async Task SaveAsync_ThenFindAsync_ReturnsSameOrder()
    {
        var order = new Order();
        order.Add("Книга", 300m, 2);

        await _sut.SaveAsync(order);
        var found = await _sut.FindAsync(order.Id);

        Assert.NotNull(found);
        Assert.Same(order, found);
    }

    [Fact]
    public async Task FindAsync_UnknownId_ReturnsNull()
    {
        var found = await _sut.FindAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task SaveAsync_Null_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SaveAsync(null!));
    }
}