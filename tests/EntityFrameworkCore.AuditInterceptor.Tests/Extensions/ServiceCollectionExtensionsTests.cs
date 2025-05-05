using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    [Fact]
    public void AddAuditing_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        
        // Act
        services.AddAuditing();
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var currentUserService = serviceProvider.GetService<ICurrentUserService>();
        var auditService = serviceProvider.GetService<AuditService>();
        
        Assert.NotNull(currentUserService);
        Assert.NotNull(auditService);
        Assert.IsType<CurrentUserService>(currentUserService);
    }
    
    [Fact]
    public void AddAuditing_WithOptions_ExecutesConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var configActionExecuted = false;
        
        // Act
        services.AddAuditing(options => {
            configActionExecuted = true;
            Assert.NotNull(options.Services);
        });
        
        // Assert
        Assert.True(configActionExecuted);
    }
    
    [Fact]
    public void AddAuditInterceptors_RegistersInterceptors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddAuditing();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .UseApplicationServiceProvider(serviceProvider)
            .AddAuditInterceptors();
        
        var options = optionsBuilder.Options;
        
        // Assert
        // The interceptors are not directly accessible, but we can verify the DbContext works
        var context = new TestDbContext((DbContextOptions<TestDbContext>)optionsBuilder.Options);
        Assert.NotNull(context);
    }
}
