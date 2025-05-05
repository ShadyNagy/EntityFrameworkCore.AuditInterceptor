using EntityFrameworkCore.AuditInterceptor.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.AuditInterceptor.Web;

public class Program
{
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddRazorPages();

    ConfigurationManager configuration = builder.Configuration;
    string connectionString = configuration.GetConnectionString("DefaultConnection")!;
    builder.Services
      .AddHttpContextAccessor()
      .AddAuditing()
      .AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(connectionString)
          .AddAuditInterceptors())
      .AddLogging(configure => configure.AddConsole());

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
      dbContext.Database.Migrate();
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler("/Error");
      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();

    app.Run();
  }
}
