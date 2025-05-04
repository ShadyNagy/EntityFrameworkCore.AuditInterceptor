using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAuditInterceptor_RegistersRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            
            // Act
            services.AddAuditInterceptor();
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var auditService = serviceProvider.GetService<AuditService>();
            Assert.NotNull(auditService);
            
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            Assert.NotNull(currentUserService);
            Assert.IsType<CurrentUserService>(currentUserService);
        }
        
        [Fact]
        public void AddAuditInterceptor_WithOptions_ConfiguresAuditOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            
            // Act
            services.AddAuditInterceptor(options => 
            {
                options.DefaultUser = "CustomDefaultUser";
            });
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var auditOptions = serviceProvider.GetRequiredService<AuditOptions>();
            
            Assert.Equal("CustomDefaultUser", auditOptions.DefaultUser);
        }
        
        [Fact]
        public void UseAuditInterceptor_RegistersInterceptorWithDbContext()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddAuditInterceptor();
            
            // Add a test DbContext
            services.AddDbContext<TestDbContext>(options => 
            {
                options.UseInMemoryDatabase("TestDb");
            });
            
            // Act
            services.UseAuditInterceptor<TestDbContext>();
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            
            // Verify the interceptor is registered (indirectly by checking options)
            var options = dbContext.GetService<DbContextOptions>();
            var extensions = options.Extensions;
            
            // At least one extension should be the interceptor extension
            Assert.Contains(extensions, e => e.GetType().Name.Contains("Interceptor"));
        }
        
        // Helper class for testing
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }
    }
}
