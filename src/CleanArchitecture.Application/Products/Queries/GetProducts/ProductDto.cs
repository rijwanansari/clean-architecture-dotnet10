namespace CleanArchitecture.Application.Products.Queries.GetProducts;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Status,
    int StockQuantity,
    DateTimeOffset CreatedAt);
