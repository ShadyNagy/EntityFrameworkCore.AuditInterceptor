# EntityFrameworkCore.AuditInterceptor

EntityFrameworkCore.AuditInterceptor is a .NET library designed to provide seamless auditing capabilities for Entity Framework Core. It allows you to automatically track changes to your entities, including who made the changes and when they were made. The library integrates effortlessly with .NET Dependency Injection and supports various auditing scenarios, making it an ideal choice for enterprise applications that require robust auditing features.

## Features

- Automatic tracking of entity changes
- Integration with .NET Dependency Injection
- Support for various auditing scenarios
- Easy configuration and setup

## Installation

To install the package, use the following command:

```sh
dotnet add package EntityFrameworkCore.AuditInterceptor
```

## Getting Started

Below is an example of how you might configure the auditing services and your DbContext in your Program.cs:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

ConfigurationManager configuration = builder.Configuration;
string connectionString = configuration.GetConnectionString("DefaultConnection")!;
builder.Services
  .AddHttpContextAccessor()
  .AddAuditing() // Registers the required services and options
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
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

After configuring the services, you can begin creating auditable entities by implementing the `IAuditable` interface. Use the built-in `SaveChanges` or `SaveChangesAsync` methods to automatically track who created or updated the entities, along with timestamps.

```csharp
// MyEntity.cs
using EntityFrameworkCore.AuditInterceptor.Interfaces;

public class MyEntity : IAuditable
{
  public string Id { get; set; } = string.Empty;
  public string CreatedBy { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public string UpdatedBy { get; set; } = string.Empty;
  public DateTime UpdatedAt { get; set; }
  public string Name { get; set; } = string.Empty;
}
```

```csharp
// MyDbContext.cs
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
  public DbSet<MyEntity> MyEntities { get; set; }

  public MyDbContext(DbContextOptions<MyDbContext> options)
    : base(options)
  {
  }
}
```

```csharp
// Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
  private readonly ILogger<IndexModel> _logger;
  private readonly MyDbContext _dbContext;

  public IndexModel(ILogger<IndexModel> logger, MyDbContext dbContext)
  {
    _logger = logger;
    _dbContext = dbContext;
  }

  public void OnGet()
  {
  }

  public async Task<IActionResult> OnPostAddEntityAsync()
  {
    var newEntity = new MyEntity
    {
      Name = "Test"
    };

    _dbContext.MyEntities.Add(newEntity);
    await _dbContext.SaveChangesAsync();

    return RedirectToPage();
  }
}
```

```aspnetcorerazor
// Index.cshtml
@page
@model IndexModel
@{
  ViewData["Title"] = "Home page";
}

<h1>Home Page</h1>

<form method="post">
  <button type="submit" asp-page-handler="AddEntity">Add New Entity</button>
</form>
```
