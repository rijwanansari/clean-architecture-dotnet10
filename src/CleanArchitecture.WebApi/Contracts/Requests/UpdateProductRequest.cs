namespace CleanArchitecture.WebApi.Contracts.Requests;

/// <summary>Request model for updating an existing product.</summary>
public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity);
