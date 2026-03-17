using CleanArchitecture.Application.Common.CQRS;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Products.Queries.GetProducts;

internal sealed class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);

        return products
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Status.ToString(),
                p.StockQuantity,
                p.CreatedAt))
            .ToList()
            .AsReadOnly();
    }
}
