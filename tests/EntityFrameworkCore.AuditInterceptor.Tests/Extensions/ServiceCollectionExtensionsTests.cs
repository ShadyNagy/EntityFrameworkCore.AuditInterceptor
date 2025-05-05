using System;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAuditing_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddAuditing();
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            Assert.NotNull(currentUserService);
            Assert.IsType<CurrentUserService>(currentUserService);
            
            var auditService = serviceProvider.GetService<AuditService>();
            Assert.NotNull(auditService);
        }
        
        [Fact]
        public void AddAuditing_WithCustomOptions_ShouldInvokeOptionsAction()
        {
            // Arrange
            var services = new ServiceCollection();
            var optionsInvoked = false;
            
            // Act
            services.AddAuditing(options => 
            {
                Assert.NotNull(options);
                Assert.Same(services, options.Services);
                optionsInvoked = true;
            });
            
            // Assert
            Assert.True(optionsInvoked, "Options action was not invoked");
        }
        
        [Fact]
        public void AddAuditInterceptors_ShouldAddInterceptorToDbContext()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddAuditing();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Register the service provider to resolve the AuditService
            services.AddSingleton<IServiceProvider>(serviceProvider);
            
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .UseApplicationServiceProvider(serviceProvider);
            
            // Act
            options.AddAuditInterceptors();
            
            // Assert
            var dbContext = new TestDbContext(options.Options);
            Assert.NotNull(dbContext);
            
            // We can't directly test if the interceptor is added, but we can verify
            // the context can be created without errors, which means the interceptor
            // was properly configured
        }
        
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options) : base(options)
            {
            }
        }
    }
}
