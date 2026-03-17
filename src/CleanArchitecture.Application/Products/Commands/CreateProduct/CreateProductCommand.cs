using CleanArchitecture.Application.Common.CQRS;

namespace CleanArchitecture.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity) : ICommand<Guid>;
