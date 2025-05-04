using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration
{
    public class AuditInterceptorIntegrationTests
    {
        [Fact]
        public async Task SaveChanges_WithNewEntity_SetsAuditFields()
        {
            // Arrange
            var services = SetupServices("testuser123");
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            var entity = new TestEntity { Name = "Test Entity" };
            
            // Act
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            // Assert
            var savedEntity = await dbContext.TestEntities.FirstOrDefaultAsync();
            Assert.NotNull(savedEntity);
            Assert.Equal("testuser123", savedEntity.CreatedBy);
            Assert.NotEqual(default, savedEntity.CreatedOn);
            Assert.Null(savedEntity.LastModifiedBy);
            Assert.Equal(default, savedEntity.LastModifiedOn);
        }
        
        [Fact]
        public async Task SaveChanges_WithModifiedEntity_SetsModifiedFields()
        {
            // Arrange
            var services = SetupServices("testuser456");
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            // Create initial entity
            var entity = new TestEntity { Name = "Initial Name" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            // Clear tracking
            dbContext.ChangeTracker.Clear();
            
            // Change user and modify entity
            UpdateHttpContextUser(services, "modifieruser789");
            
            var savedEntity = await dbContext.TestEntities.FirstAsync();
            savedEntity.Name = "Updated Name";
            dbContext.TestEntities.Update(savedEntity);
            
            // Act
            await dbContext.SaveChangesAsync();
            
            // Assert
            var updatedEntity = await dbContext.TestEntities.FirstAsync();
            Assert.Equal("Updated Name", updatedEntity.Name);
            Assert.Equal("testuser456", updatedEntity.CreatedBy); // Should not change
            Assert.NotEqual(default, updatedEntity.CreatedOn);
            Assert.Equal("modifieruser789", updatedEntity.LastModifiedBy);
            Assert.NotEqual(default, updatedEntity.LastModifiedOn);
        }
        
        [Fact]
        public async Task SaveChanges_WithNoAuthenticatedUser_UsesDefaultUserId()
        {
            // Arrange
            var services = SetupServices(null, "SystemDefault");
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            var entity = new TestEntity { Name = "Anonymous Entity" };
            
            // Act
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            // Assert
            var savedEntity = await dbContext.TestEntities.FirstOrDefaultAsync();
            Assert.NotNull(savedEntity);
            Assert.Equal("SystemDefault", savedEntity.CreatedBy);
            Assert.NotEqual(default, savedEntity.CreatedOn);
        }
        
        private IServiceProvider SetupServices(string userId, string defaultUserId = "System")
        {
            var services = new ServiceCollection();
            
            // Setup HttpContextAccessor with user claims if userId is provided
            var httpContextAccessor = new HttpContextAccessor();
            if (userId != null)
            {
                var httpContext = new DefaultHttpContext();
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
                httpContextAccessor.HttpContext = httpContext;
            }
            
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            
            // Add audit interceptor with custom options if provided
            services.AddAuditInterceptor(options =>
            {
                options.DefaultUserId = defaultUserId;
            });
            
            // Add test DbContext with in-memory database
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("AuditInterceptorIntegrationTests");
                options.UseAuditInterceptor(sp);
            });
            
            return services.BuildServiceProvider();
        }
        
        private void UpdateHttpContextUser(IServiceProvider services, string userId)
        {
            var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            httpContextAccessor.HttpContext = httpContext;
        }
        
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
            
            public DbSet<TestEntity> TestEntities { get; set; }
        }
        
        public class TestEntity : IAuditable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime LastModifiedOn { get; set; }
        }
    }
}
