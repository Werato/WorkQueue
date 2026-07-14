using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkQueue.Infrastructure;
using WorkQueue.Domain.Enums;
using WorkQueue.DTO;

namespace WorkQueue.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly WorkQueueDbContext _context;

        public DashboardController(WorkQueueDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            // Global Query Filter automatically isolates by Tenant
            var baseQuery = _context.WorkItems.AsQueryable();

            var newCount = await baseQuery.CountAsync(w => w.Status == WorkItemStatus.New);
            var inProgressCount = await baseQuery.CountAsync(w => w.Status == WorkItemStatus.InProgress);

            // Cast to int if 'Blocked' is not in your enum yet, e.g., (int)w.Status == 3
            var blockedCount = await baseQuery.CountAsync(w => (int)w.Status == 3);

            var doneCount = await baseQuery.CountAsync(w => w.Status == WorkItemStatus.Done);

            var overdueCount = await baseQuery.CountAsync(w =>
                w.Status != WorkItemStatus.Done &&
                w.DueDate.HasValue &&
                w.DueDate.Value < DateTime.UtcNow);

            var response = new DashboardSummaryResponse(
                newCount,
                inProgressCount,
                blockedCount,
                doneCount,
                overdueCount
            );

            return Ok(response);
        }
    }
}