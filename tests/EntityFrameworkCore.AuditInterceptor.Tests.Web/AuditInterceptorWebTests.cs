using EntityFrameworkCore.AuditInterceptor.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Web
{
    public class AuditInterceptorWebTests
    {
        [Fact]
        public async Task WebApplication_WithAuditInterceptor_AppliesAuditInformation()
        {
            // Arrange
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TestStartup>();

            using var testServer = new TestServer(webHostBuilder);
            using var scope = testServer.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            
            // Clear any existing data
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            
            // Act
            var entity = new MyEntity { Name = "Web Test Entity" };
            dbContext.MyEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            // Assert
            var savedEntity = await dbContext.MyEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
            Assert.NotNull(savedEntity);
            Assert.Equal("System", savedEntity.CreatedBy);
            Assert.NotNull(savedEntity.CreatedOn);
            Assert.Equal("System", savedEntity.LastModifiedBy);
            Assert.NotNull(savedEntity.LastModifiedOn);
        }
    }
    
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddAuditInterceptor();
            
            services.AddDbContext<MyDbContext>(options =>
            {
                options.UseInMemoryDatabase("WebTestDb");
            });
            
            services.UseAuditInterceptor<MyDbContext>();
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Minimal configuration for testing
        }
    }
}
