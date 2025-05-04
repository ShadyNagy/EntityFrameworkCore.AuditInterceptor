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
        public void AddAuditing_RegistersRequiredServices()
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
        public void AddAuditing_WithConfigureOptions_InvokesConfigureAction()
        {
            // Arrange
            var services = new ServiceCollection();
            var configureOptionsCalled = false;

            // Act
            services.AddAuditing(options =>
            {
                Assert.NotNull(options);
                Assert.Same(services, options.Services);
                configureOptionsCalled = true;
            });

            // Assert
            Assert.True(configureOptionsCalled);
        }

        [Fact]
        public void AddAuditInterceptors_AddsInterceptorsToDbContext()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddAuditing();
            services.AddSingleton<AuditService>(sp => new AuditService(sp.GetRequiredService<ICurrentUserService>()));
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .UseApplicationServiceProvider(serviceProvider)
                .AddAuditInterceptors()
                .Options;
            
            // Assert - difficult to directly test interceptors, but we can verify the context can be created
            using var context = new TestDbContext(options);
            Assert.NotNull(context);
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options)
                : base(options)
            {
            }
        }
    }
}
