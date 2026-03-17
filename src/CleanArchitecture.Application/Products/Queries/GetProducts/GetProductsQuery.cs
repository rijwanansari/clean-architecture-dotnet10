using CleanArchitecture.Application.Common.CQRS;

namespace CleanArchitecture.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
