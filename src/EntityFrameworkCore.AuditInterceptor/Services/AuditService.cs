using System;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntityFrameworkCore.AuditInterceptor.Services;

public class AuditService(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    AuditEntities(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    AuditEntities(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void AuditEntities(DbContext? context)
  {
    if (context == null) return;

    var userId = currentUserService.GetUserId() ?? "System";

    foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
    {
      if (entry.State == EntityState.Added)
      {
        entry.Entity.CreatedBy = userId;
        entry.Entity.CreatedAt = DateTime.UtcNow;
        entry.Entity.UpdatedBy = userId;
        entry.Entity.UpdatedAt = DateTime.UtcNow;
      }
      else if(entry.State == EntityState.Modified)
      {
        entry.Entity.UpdatedBy = userId;
        entry.Entity.UpdatedAt = DateTime.UtcNow;
      }
    }
  }
}
