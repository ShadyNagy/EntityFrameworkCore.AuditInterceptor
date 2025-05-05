using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntityFrameworkCore.AuditInterceptor.Web.Pages;

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
