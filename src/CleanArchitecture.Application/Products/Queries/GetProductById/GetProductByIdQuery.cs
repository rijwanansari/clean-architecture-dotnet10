using CleanArchitecture.Application.Common.CQRS;
using CleanArchitecture.Application.Products.Queries.GetProducts;

namespace CleanArchitecture.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto>;
