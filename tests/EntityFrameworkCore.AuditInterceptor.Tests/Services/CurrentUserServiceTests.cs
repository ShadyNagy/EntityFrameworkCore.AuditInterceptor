using System.Security.Claims;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class CurrentUserServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly CurrentUserService _currentUserService;

        public CurrentUserServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _currentUserService = new CurrentUserService(_mockHttpContextAccessor.Object);
        }

        [Fact]
        public void GetUserId_ShouldReturnUserId_WhenUserIsAuthenticated()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, expectedUserId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        [Fact]
        public void GetUserId_ShouldReturnNull_WhenHttpContextIsNull()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserId_ShouldReturnNull_WhenUserIsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                User = null
            };
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserId_ShouldReturnNull_WhenNameIdentifierClaimIsMissing()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"), // Different claim type
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }
    }
}
