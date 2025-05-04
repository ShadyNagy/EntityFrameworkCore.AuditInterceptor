using System;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
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
        public async Task SaveChanges_ShouldAuditEntity_WhenEntityIsAdded()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            // Setup user
            var expectedUserId = "test-integration-user";
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, expectedUserId) }
                    )
                )
            });
            
            services.AddSingleton(mockHttpContextAccessor.Object);
            services.AddAuditing();
            
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase($"AuditingIntegrationTest_{Guid.NewGuid()}")
                    .UseApplicationServiceProvider(sp)
                    .AddAuditInterceptors();
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = new TestEntity { Id = "1", Name = "Integration Test Entity" };
                dbContext.TestEntities.Add(entity);
                await dbContext.SaveChangesAsync();
                
                // Assert
                Assert.Equal(expectedUserId, entity.CreatedBy);
                Assert.Equal(expectedUserId, entity.UpdatedBy);
                Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
                Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
            }
        }

        [Fact]
        public async Task SaveChanges_ShouldUpdateAuditFields_WhenEntityIsModified()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            // Setup initial user
            var initialUserId = "initial-user";
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, initialUserId) }
                    )
                )
            });
            
            services.AddSingleton(mockHttpContextAccessor.Object);
            services.AddAuditing();
            
            var dbName = $"AuditingIntegrationTest_{Guid.NewGuid()}";
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName)
                    .UseApplicationServiceProvider(sp)
                    .AddAuditInterceptors();
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act - First create an entity
            string entityId = "integration-test-id";
            DateTime initialTime;
            
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = new TestEntity { Id = entityId, Name = "Initial Name" };
                dbContext.TestEntities.Add(entity);
                await dbContext.SaveChangesAsync();
                initialTime = entity.CreatedAt;
            }
            
            // Now update with a different user
            var updatedUserId = "updated-user";
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, updatedUserId) }
                    )
                )
            });
            
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = await dbContext.TestEntities.FindAsync(entityId);
                entity.Name = "Updated Name";
                await dbContext.SaveChangesAsync();
                
                // Assert
                Assert.Equal(initialUserId, entity.CreatedBy);
                Assert.Equal(initialTime, entity.CreatedAt);
                Assert.Equal(updatedUserId, entity.UpdatedBy);
                Assert.True(entity.UpdatedAt > initialTime);
            }
        }

        // Test DbContext for integration tests
        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        // Test entity for integration tests
        public class TestEntity : IAuditable
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
