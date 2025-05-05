using System.Security.Claims;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services;

public class CurrentUserServiceTests
{
  [Fact]
  public void GetUserId_WhenUserHasNameIdentifierClaim_ReturnsClaimValue()
  {
    // Arrange
    var userId = "test-user-id";
    var httpContextMock = new Mock<HttpContext>();
    var claimsPrincipalMock = new Mock<ClaimsPrincipal>();

    claimsPrincipalMock
        .Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
        .Returns(new Claim(ClaimTypes.NameIdentifier, userId));

    httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);

    var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

    var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

    // Act
    var result = currentUserService.GetUserId();

    // Assert
    Assert.Equal(userId, result);
  }

  [Fact]
  public void GetUserId_WhenHttpContextIsNull_ReturnsNull()
  {
    // Arrange
    var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);

    var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

    // Act
    var result = currentUserService.GetUserId();

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void GetUserId_WhenUserIsNull_ReturnsNull()
  {
    // Arrange
    var httpContextMock = new Mock<HttpContext>();
    httpContextMock.Setup(x => x.User).Returns((ClaimsPrincipal)null!);

    var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

    var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

    // Act
    var result = currentUserService.GetUserId();

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void GetUserId_WhenNameIdentifierClaimNotFound_ReturnsNull()
  {
    // Arrange
    var httpContextMock = new Mock<HttpContext>();
    var claimsPrincipalMock = new Mock<ClaimsPrincipal>();

    claimsPrincipalMock
        .Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
        .Returns((Claim?)null);

    httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);

    var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

    var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

    // Act
    var result = currentUserService.GetUserId();

    // Assert
    Assert.Null(result);
  }
}
