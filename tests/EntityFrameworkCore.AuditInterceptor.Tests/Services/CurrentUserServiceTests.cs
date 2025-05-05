using System.Security.Claims;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class CurrentUserServiceTests
    {
        [Fact]
        public void GetUserId_WithAuthenticatedUser_ShouldReturnNameIdentifierClaim()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, expectedUserId),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var userId = currentUserService.GetUserId();
            
            // Assert
            Assert.Equal(expectedUserId, userId);
        }
        
        [Fact]
        public void GetUserId_WithoutHttpContext_ShouldReturnNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var userId = currentUserService.GetUserId();
            
            // Assert
            Assert.Null(userId);
        }
        
        [Fact]
        public void GetUserId_WithoutUser_ShouldReturnNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = null!;
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var userId = currentUserService.GetUserId();
            
            // Assert
            Assert.Null(userId);
        }
        
        [Fact]
        public void GetUserId_WithoutNameIdentifierClaim_ShouldReturnNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var userId = currentUserService.GetUserId();
            
            // Assert
            Assert.Null(userId);
        }
    }
}
