using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration
{
    public class AuditInterceptorIntegrationTests
    {
        [Fact]
        public async Task SaveChanges_WithAuditableEntity_AppliesAuditInformation()
        {
            // Arrange
            var userId = "integration-test-user";
            var services = SetupServices(userId);
            
            // Get the DbContext
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            // Add a new entity
            var entity = new TestAuditableEntity { Name = "Integration Test Entity" };
            dbContext.AuditableEntities.Add(entity);
            
            // Act
            await dbContext.SaveChangesAsync();
            
            // Assert
            Assert.Equal(userId, entity.CreatedBy);
            Assert.NotNull(entity.CreatedOn);
            Assert.Equal(userId, entity.LastModifiedBy);
            Assert.NotNull(entity.LastModifiedOn);
        }
        
        [Fact]
        public async Task SaveChanges_WithExistingEntity_UpdatesModifiedInformation()
        {
            // Arrange
            var userId = "integration-test-user";
            var services = SetupServices(userId);
            
            // Get the DbContext
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            // Add and save an entity first
            var entity = new TestAuditableEntity { Name = "Original Name" };
            dbContext.AuditableEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            var originalCreatedBy = entity.CreatedBy;
            var originalCreatedOn = entity.CreatedOn;
            
            // Change the user ID for the update
            var newUserId = "different-user";
            UpdateCurrentUser(services, newUserId);
            
            // Modify the entity
            entity.Name = "Updated Name";
            
            // Act
            await dbContext.SaveChangesAsync();
            
            // Assert
            Assert.Equal(originalCreatedBy, entity.CreatedBy);
            Assert.Equal(originalCreatedOn, entity.CreatedOn);
            Assert.Equal(newUserId, entity.LastModifiedBy);
            Assert.NotNull(entity.LastModifiedOn);
        }
        
        [Fact]
        public async Task SaveChanges_WithNonAuditableEntity_DoesNotApplyAuditInformation()
        {
            // Arrange
            var userId = "integration-test-user";
            var services = SetupServices(userId);
            
            // Get the DbContext
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            // Add a new non-auditable entity
            var entity = new TestNonAuditableEntity { Description = "Non-auditable Entity" };
            dbContext.NonAuditableEntities.Add(entity);
            
            // Act & Assert - No exception should be thrown
            await dbContext.SaveChangesAsync();
        }
        
        private IServiceProvider SetupServices(string userId)
        {
            var services = new ServiceCollection();
            
            // Add HttpContextAccessor with mock user
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    "TestAuthentication"
                )
            );
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            services.AddSingleton(httpContextAccessor.Object);
            
            // Add audit interceptor
            services.AddAuditInterceptor();
            
            // Add test DbContext
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<TestDbContext>(options => 
            {
                options.UseInMemoryDatabase(dbName);
            });
            
            // Configure audit interceptor for the DbContext
            services.UseAuditInterceptor<TestDbContext>();
            
            return services.BuildServiceProvider();
        }
        
        private void UpdateCurrentUser(IServiceProvider services, string userId)
        {
            var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    "TestAuthentication"
                )
            );
            
            // Update the HttpContext
            var mockAccessor = Mock.Get(httpContextAccessor);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        }
        
        // Test DbContext and entities
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
            
            public DbSet<TestAuditableEntity> AuditableEntities { get; set; }
            public DbSet<TestNonAuditableEntity> NonAuditableEntities { get; set; }
        }
        
        public class TestAuditableEntity : IAuditable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime? CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime? LastModifiedOn { get; set; }
        }
        
        public class TestNonAuditableEntity
        {
            public int Id { get; set; }
            public string Description { get; set; }
        }
    }
}
