using Moq;
using Orders.Abstractions;
using Orders.Models;
using Orders.Services;

namespace Orders.Tests;

public class OrderServiceTests
{
    readonly Mock<IOrderRepository> _repo = new();
    readonly Mock<IMessagePublisher> _pub = new();
    readonly OrderService _sut;

    public OrderServiceTests() =>
        _sut = new(_repo.Object, _pub.Object, new DiscountCalculator());

    static NewOrder Cmd() =>
        new(new List<OrderItem> { new("Книга", 300m, 2) });

    [Fact]
    public async Task CreateAsync_ValidOrder_SavesOrder()
    {
        await _sut.CreateAsync(Cmd());

        _repo.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once());
    }

    [Fact]
    public async Task CreateAsync_ValidOrder_PublishesEventWithSameId()
    {
        var id = await _sut.CreateAsync(Cmd());

        _pub.Verify(p => p.PublishAsync(It.Is<OrderCreated>(e => e.OrderId == id)),
                    Times.Once());
    }

    [Fact]
    public async Task CreateAsync_SaveFails_DoesNotPublish()
    {
        _repo.Setup(r => r.SaveAsync(It.IsAny<Order>()))
             .ThrowsAsync(new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() => _sut.CreateAsync(Cmd()));

        _pub.Verify(p => p.PublishAsync(It.IsAny<OrderCreated>()), Times.Never());
    }

    [Fact]
    public async Task CreateAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_EmptyItems_ThrowsAndDoesNotSave()
    {
        var cmd = new NewOrder(new List<OrderItem>());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(cmd));

        _repo.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Never());
    }

    [Fact]
    public async Task CreateAsync_PublishFails_ExceptionPropagates()
    {
        _pub.Setup(p => p.PublishAsync(It.IsAny<OrderCreated>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(Cmd()));
    }

    [Fact]
    public async Task GetPayableAsync_TotalBelowThreshold_NoDiscount()
    {
        var order = new Order();
        order.Add("Книга", 500m, 1);
        _repo.Setup(r => r.FindAsync(order.Id)).ReturnsAsync(order);

        var payable = await _sut.GetPayableAsync(order.Id);

        Assert.Equal(500m, payable);
    }

    [Fact]
    public async Task GetPayableAsync_TotalAboveThreshold_AppliesDiscount()
    {
        var order = new Order();
        order.Add("Книга", 1500m, 1);
        _repo.Setup(r => r.FindAsync(order.Id)).ReturnsAsync(order);

        var payable = await _sut.GetPayableAsync(order.Id);

        Assert.Equal(1350m, payable);
    }

    [Fact]
    public async Task GetPayableAsync_OrderNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetPayableAsync(id));
    }
}