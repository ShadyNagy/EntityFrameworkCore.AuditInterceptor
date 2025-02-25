using System;
using Microsoft.Extensions.DependencyInjection;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.AuditInterceptor.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAuditing(this IServiceCollection services, Action<AuditOptions>? configureOptions = null)
	{
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<AuditService>();

		configureOptions?.Invoke(new AuditOptions(services));

		return services;
	}

	public static DbContextOptionsBuilder AddAuditInterceptors(this DbContextOptionsBuilder optionsBuilder)
	{
		var coreOptionsExtension = optionsBuilder.Options.GetExtension<CoreOptionsExtension>();
		var clonedCoreOptionsExtension = new CoreOptionsExtension()
      .WithApplicationServiceProvider(coreOptionsExtension.ApplicationServiceProvider);

		((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
      .AddOrUpdateExtension(clonedCoreOptionsExtension);

		optionsBuilder.AddInterceptors(
      coreOptionsExtension.ApplicationServiceProvider!.GetRequiredService<AuditService>());

		return optionsBuilder;
	}
}
