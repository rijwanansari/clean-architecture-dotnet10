using CleanArchitecture.Application.Common.CQRS;

namespace CleanArchitecture.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity) : ICommand;
