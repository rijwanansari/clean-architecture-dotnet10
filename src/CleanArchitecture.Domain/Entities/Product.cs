using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product() { } // Required by EF Core

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = default!;
    public ProductStatus Status { get; private set; }
    public int StockQuantity { get; private set; }

    public static Product Create(string name, string? description, Money price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        return new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Status = ProductStatus.Active,
            StockQuantity = stockQuantity
        };
    }

    public void Update(string name, string? description, Money price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void Deactivate() => Status = ProductStatus.Inactive;
    public void Activate() => Status = ProductStatus.Active;
}
