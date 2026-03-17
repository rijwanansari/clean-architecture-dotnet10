using CleanArchitecture.Application.Common.CQRS;

namespace CleanArchitecture.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand;
