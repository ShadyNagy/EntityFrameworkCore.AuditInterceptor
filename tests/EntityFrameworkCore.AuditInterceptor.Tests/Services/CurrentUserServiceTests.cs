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
        public void UserId_WithAuthenticatedUser_ReturnsUserId()
        {
            // Arrange
            var userId = "user123";
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            
            claimsPrincipalMock
                .Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
                .Returns(new Claim(ClaimTypes.NameIdentifier, userId));
            
            httpContextAccessorMock
                .Setup(x => x.HttpContext.User)
                .Returns(claimsPrincipalMock.Object);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = currentUserService.UserId;
            
            // Assert
            Assert.Equal(userId, result);
        }
        
        [Fact]
        public void UserId_WithUnauthenticatedUser_ReturnsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            
            claimsPrincipalMock
                .Setup(x => x.FindFirst(ClaimTypes.NameIdentifier))
                .Returns((Claim)null);
            
            httpContextAccessorMock
                .Setup(x => x.HttpContext.User)
                .Returns(claimsPrincipalMock.Object);
            
            var currentUserService = new CurrentUserService(httpContextAccessorMock.Object);
            
            // Act
            var result = currentUserService.UserId;
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void UserId_WithNullHttpContext_ReturnsNull()
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
