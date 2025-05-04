using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class CurrentUserServiceTests
    {
        [Fact]
        public void UserId_WhenUserIsAuthenticated_ReturnsNameIdentifierClaim()
        {
            // Arrange
            var userId = "test-user-id";
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsPrincipalMock = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuthentication"));

            httpContextAccessorMock.Setup(x => x.HttpContext.User).Returns(claimsPrincipalMock);
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

            // Act
            var result = currentUserService.UserId;

            // Assert
            Assert.Equal(userId, result);
        }

        [Fact]
        public void UserId_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsPrincipalMock = new ClaimsPrincipal(new ClaimsIdentity());

            httpContextAccessorMock.Setup(x => x.HttpContext.User).Returns(claimsPrincipalMock);
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

            // Act
            var result = currentUserService.UserId;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void UserId_WhenHttpContextIsNull_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);

            // Act
            var result = currentUserService.UserId;

            // Assert
            Assert.Null(result);
        }
    }
}
