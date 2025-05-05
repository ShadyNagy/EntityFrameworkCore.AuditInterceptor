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
        var expectedUserId = "user-123";
        var httpContext = new Mock<HttpContext>();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        }));
        
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        
        var currentUserService = new CurrentUserService(httpContextAccessor.Object);
        
        // Act
        var userId = currentUserService.GetUserId();
        
        // Assert
        Assert.Equal(expectedUserId, userId);
    }
    
    [Fact]
    public void GetUserId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);
        
        var currentUserService = new CurrentUserService(httpContextAccessor.Object);
        
        // Act
        var userId = currentUserService.GetUserId();
        
        // Assert
        Assert.Null(userId);
    }
    
    [Fact]
    public void GetUserId_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.User).Returns((ClaimsPrincipal)null!);
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        
        var currentUserService = new CurrentUserService(httpContextAccessor.Object);
        
        // Act
        var userId = currentUserService.GetUserId();
        
        // Assert
        Assert.Null(userId);
    }
    
    [Fact]
    public void GetUserId_WhenUserHasNoNameIdentifierClaim_ReturnsNull()
    {
        // Arrange
        var httpContext = new Mock<HttpContext>();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Test User")
        }));
        
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        
        var currentUserService = new CurrentUserService(httpContextAccessor.Object);
        
        // Act
        var userId = currentUserService.GetUserId();
        
        // Assert
        Assert.Null(userId);
    }
}
