using System;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration
{
    public class AuditingIntegrationTests
    {
        [Fact]
        public async Task FullIntegration_ShouldAuditEntitiesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Mock HTTP context accessor
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "integration-test-user") }
                )
            );
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            
            // Register services
            services.AddSingleton(httpContextAccessorMock.Object);
            services.AddAuditing();
            
            // Configure DbContext
            var dbName = $"AuditingIntegrationTest_{Guid.NewGuid()}";
            services.AddDbContext<IntegrationTestDbContext>(options =>
                options.UseInMemoryDatabase(dbName)
                    .AddAuditInterceptors());
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
                
                // Add entity
                var entity = new IntegrationTestEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Integration Test Entity"
                };
                
                dbContext.TestEntities.Add(entity);
                await dbContext.SaveChangesAsync();
                
                // Modify entity
                entity.Name = "Updated Integration Test Entity";
                await dbContext.SaveChangesAsync();
                
                // Assert
                var savedEntity = await dbContext.TestEntities.FindAsync(entity.Id);
                Assert.NotNull(savedEntity);
                Assert.Equal("integration-test-user", savedEntity!.CreatedBy);
                Assert.Equal("integration-test-user", savedEntity.UpdatedBy);
                Assert.Equal("Updated Integration Test Entity", savedEntity.Name);
                Assert.True(savedEntity.UpdatedAt >= savedEntity.CreatedAt);
            }
        }
        
        public class IntegrationTestDbContext : DbContext
        {
            public DbSet<IntegrationTestEntity> TestEntities { get; set; } = null!;
            
            public IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options)
                : base(options)
            {
            }
        }
        
        public class IntegrationTestEntity : IAuditable
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string CreatedBy { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string UpdatedBy { get; set; } = string.Empty;
            public DateTime UpdatedAt { get; set; }
        }
    }
}
