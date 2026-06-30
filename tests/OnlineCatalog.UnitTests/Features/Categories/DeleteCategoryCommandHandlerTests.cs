using FluentAssertions;
using Moq;
using OnlineCatalog.Application.Features.Categories.Commands;
using OnlineCatalog.Domain.Entities;
using OnlineCatalog.Domain.Exceptions;
using OnlineCatalog.Domain.Interfaces.Repositories;
using Xunit;

namespace OnlineCatalog.UnitTests.Features.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repoMock = new();
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _handler = new DeleteCategoryCommandHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_CategoryWithItems_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default))
            .ReturnsAsync(Category.Create("Electronics"));
        _repoMock.Setup(r => r.HasCatalogItemsAsync(id, default)).ReturnsAsync(true);

        var act = () => _handler.Handle(new DeleteCategoryCommand(id), default);

        await act.Should().ThrowAsync<ConflictException>();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Category>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Category?)null);

        var act = () => _handler.Handle(new DeleteCategoryCommand(id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidCategory_DeletesSuccessfully()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default))
            .ReturnsAsync(Category.Create("Electronics"));
        _repoMock.Setup(r => r.HasCatalogItemsAsync(id, default)).ReturnsAsync(false);

        await _handler.Handle(new DeleteCategoryCommand(id), default);

        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Category>(), default), Times.Once);
    }
}
