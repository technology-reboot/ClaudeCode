using AutoMapper;
using FluentAssertions;
using Moq;
using OnlineCatalog.Application.Features.Catalog.Commands;
using OnlineCatalog.Application.Mappings;
using OnlineCatalog.Domain.Entities;
using OnlineCatalog.Domain.Exceptions;
using OnlineCatalog.Domain.Interfaces.Repositories;
using Xunit;

namespace OnlineCatalog.UnitTests.Features.Catalog;

public class CreateCatalogItemCommandHandlerTests
{
    private readonly Mock<ICatalogItemRepository> _catalogRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly IMapper _mapper;
    private readonly CreateCatalogItemCommandHandler _handler;

    public CreateCatalogItemCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _handler = new CreateCatalogItemCommandHandler(_catalogRepoMock.Object, _categoryRepoMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ThrowsUnprocessableEntityException()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync((Category?)null);

        var command = new CreateCatalogItemCommand(categoryId, "Headphones", null, 99.99m, null);
        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCatalogItem()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics");
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var item = CatalogItem.Create(categoryId, "Headphones", null, 99.99m, null);
        _catalogRepoMock.Setup(r => r.AddAsync(It.IsAny<CatalogItem>(), default))
            .ReturnsAsync((CatalogItem i, CancellationToken _) => i);
        _catalogRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(item);

        var command = new CreateCatalogItemCommand(categoryId, "Headphones", null, 99.99m, null);
        var result = await _handler.Handle(command, default);

        result.Name.Should().Be("Headphones");
        result.Price.Should().Be(99.99m);
    }
}
