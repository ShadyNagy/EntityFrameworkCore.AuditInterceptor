using System.Security.Claims;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class CurrentUserServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CurrentUserService _currentUserService;

        public CurrentUserServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);
        }

        [Fact]
        public void GetUserId_WithAuthenticatedUser_ReturnsUserId()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            var httpContext = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId) },
                    "TestAuthentication"
                )
            );
            httpContext.User = claimsPrincipal;
            
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        [Fact]
        public void GetUserId_WithoutAuthenticatedUser_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            httpContext.User = claimsPrincipal;
            
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserId_WithNullHttpContext_ReturnsNull()
        {
            // Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserId_WithNullUser_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                User = null
            };
            
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _currentUserService.GetUserId();

            // Assert
            Assert.Null(result);
        }
    }
}
