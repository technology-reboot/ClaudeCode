using AutoMapper;
using FluentAssertions;
using Moq;
using OnlineCatalog.Application.Common;
using OnlineCatalog.Application.Features.Users.Commands;
using OnlineCatalog.Application.Mappings;
using OnlineCatalog.Domain.Entities;
using OnlineCatalog.Domain.Exceptions;
using OnlineCatalog.Domain.Interfaces.Repositories;
using Xunit;

namespace OnlineCatalog.UnitTests.Features.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IApiKeyRepository> _apiKeyRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
        _apiKeyRepoMock.Setup(r => r.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey k, CancellationToken _) => k);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        _handler = new CreateUserCommandHandler(_userRepoMock.Object, _apiKeyRepoMock.Object, _hasherMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsApiKey()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("test@example.com", default)).ReturnsAsync(false);

        var command = new CreateUserCommand("Test User", "test@example.com", "SecurePass1!");
        var result = await _handler.Handle(command, default);

        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
        result.ApiKey.Should().NotBeNullOrEmpty();
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        _apiKeyRepoMock.Verify(r => r.AddAsync(It.IsAny<ApiKey>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("exists@example.com", default)).ReturnsAsync(true);

        var command = new CreateUserCommand("Test User", "exists@example.com", "SecurePass1!");
        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
