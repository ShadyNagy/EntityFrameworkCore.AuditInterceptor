using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration
{
    public class AuditInterceptorIntegrationTests
    {
        [Fact]
        public async Task SaveChanges_WithAuditableEntity_TracksAuditInformation()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            var services = new ServiceCollection();
            
            // Setup HttpContextAccessor with a user
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId) },
                        "TestAuthentication"
                    )
                )
            };
            
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = httpContext
            };
            
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            
            // Add auditing services
            services.AddAuditing();
            
            // Add in-memory database
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName)
                    .UseApplicationServiceProvider(sp)
                    .AddAuditInterceptors();
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            // Create and save a new entity
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                
                var entity = new TestEntity { Name = "Test Entity" };
                dbContext.TestEntities.Add(entity);
                
                await dbContext.SaveChangesAsync();
                
                // Assert
                Assert.Equal(expectedUserId, entity.CreatedBy);
                Assert.Equal(expectedUserId, entity.UpdatedBy);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) <= entity.CreatedAt);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) <= entity.UpdatedAt);
            }
            
            // Update the entity
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                
                var entity = await dbContext.TestEntities.FirstAsync();
                var originalCreatedBy = entity.CreatedBy;
                var originalCreatedAt = entity.CreatedAt;
                var originalUpdatedAt = entity.UpdatedAt;
                
                // Modify the entity
                entity.Name = "Updated Test Entity";
                
                // Wait to ensure timestamps will be different
                await Task.Delay(10);
                
                await dbContext.SaveChangesAsync();
                
                // Assert
                Assert.Equal(originalCreatedBy, entity.CreatedBy); // Should not change
                Assert.Equal(originalCreatedAt, entity.CreatedAt); // Should not change
                Assert.Equal(expectedUserId, entity.UpdatedBy);
                Assert.True(entity.UpdatedAt > originalUpdatedAt); // Should be updated
            }
        }

        // Test DbContext
        private class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }
            
            public TestDbContext(DbContextOptions<TestDbContext> options) 
                : base(options)
            {
            }
        }

        // Test Entity
        private class TestEntity : IAuditable
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = string.Empty;
            public string CreatedBy { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string UpdatedBy { get; set; } = string.Empty;
            public DateTime UpdatedAt { get; set; }
        }
    }
}
