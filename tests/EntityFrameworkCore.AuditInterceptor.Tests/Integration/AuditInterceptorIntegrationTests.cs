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
        public async Task SaveChanges_WithAuditableEntities_AppliesAuditInformation()
        {
            // Arrange
            var services = new ServiceCollection();
            var userId = "test-integration-user";
            
            // Configure services
            services.AddHttpContextAccessor();
            services.AddScoped<IHttpContextAccessor>(provider => 
            {
                var accessor = new HttpContextAccessor();
                accessor.HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
                    }, "TestAuthentication"))
                };
                return accessor;
            });
            
            services.AddAuditInterceptor();
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb")
                       .UseAuditInterceptor(sp);
            });

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            // Act - Add entity
            var entity = new TestAuditableEntity { Name = "Test Entity" };
            dbContext.AuditableEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            // Assert - Created properties set
            var savedEntity = await dbContext.AuditableEntities.FindAsync(entity.Id);
            Assert.Equal(userId, savedEntity.CreatedBy);
            Assert.NotEqual(default, savedEntity.CreatedOn);
            Assert.Null(savedEntity.LastModifiedBy);
            Assert.Equal(default, savedEntity.LastModifiedOn);

            // Act - Update entity
            savedEntity.Name = "Updated Test Entity";
            await dbContext.SaveChangesAsync();

            // Assert - Modified properties set
            var updatedEntity = await dbContext.AuditableEntities.FindAsync(entity.Id);
            Assert.Equal(userId, updatedEntity.CreatedBy);
            Assert.NotEqual(default, updatedEntity.CreatedOn);
            Assert.Equal(userId, updatedEntity.LastModifiedBy);
            Assert.NotEqual(default, updatedEntity.LastModifiedOn);
        }

        [Fact]
        public async Task SaveChanges_WithNonAuditableEntities_DoesNotApplyAuditInformation()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Configure services
            services.AddHttpContextAccessor();
            services.AddAuditInterceptor();
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("NonAuditableTestDb")
                       .UseAuditInterceptor(sp);
            });

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            // Act - Add non-auditable entity
            var entity = new TestNonAuditableEntity { Name = "Non-Auditable Entity" };
            dbContext.NonAuditableEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            // Assert - Entity saved without errors
            var savedEntity = await dbContext.NonAuditableEntities.FindAsync(entity.Id);
            Assert.Equal("Non-Auditable Entity", savedEntity.Name);
        }

        public class TestDbContext : DbContext
        {
            public DbSet<TestAuditableEntity> AuditableEntities { get; set; }
            public DbSet<TestNonAuditableEntity> NonAuditableEntities { get; set; }

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<TestAuditableEntity>()
                    .HasKey(e => e.Id);

                modelBuilder.Entity<TestNonAuditableEntity>()
                    .HasKey(e => e.Id);
            }
        }

        public class TestAuditableEntity : IAuditable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime LastModifiedOn { get; set; }
        }

        public class TestNonAuditableEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
