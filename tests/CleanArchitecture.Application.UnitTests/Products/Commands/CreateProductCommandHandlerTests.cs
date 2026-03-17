using CleanArchitecture.Application.Products.Commands.CreateProduct;
using CleanArchitecture.Application.Products.Queries.GetProducts;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;
using NSubstitute;

namespace CleanArchitecture.Application.UnitTests.Products.Commands;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProductCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsNonEmptyGuid()
    {
        var command = new CreateProductCommand("Widget", "A fine widget", 9.99m, "USD", 100);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsProductToRepository()
    {
        var command = new CreateProductCommand("Widget", null, 9.99m, "USD", 10);

        await _handler.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CommitsUnitOfWork()
    {
        var command = new CreateProductCommand("Widget", null, 9.99m, "USD", 10);

        await _handler.HandleAsync(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MapsCommandPropertiesToProduct()
    {
        Product? captured = null;
        await _repository.AddAsync(
            Arg.Do<Product>(p => captured = p),
            Arg.Any<CancellationToken>());

        var command = new CreateProductCommand("My Product", "Desc", 49.99m, "EUR", 5);
        await _handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("My Product", captured.Name);
        Assert.Equal(new Money(49.99m, "EUR"), captured.Price);
        Assert.Equal(5, captured.StockQuantity);
    }
}
