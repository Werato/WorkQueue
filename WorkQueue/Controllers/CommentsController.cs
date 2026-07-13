using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkQueue.Domain.Entities;
using WorkQueue.Infrastructure;

namespace WorkQueue.Controllers
{
    [Route("api/workitems/{workItemId}/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly WorkQueueDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CommentsController(WorkQueueDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: api/workitems/{workItemId}/comments
        [HttpGet]
        public async Task<IActionResult> GetComments(Guid workItemId)
        {
            // Проверяем, существует ли задача в рамках текущей организации
            var taskExists = await _context.WorkItems.AnyAsync(w => w.Id == workItemId);
            if (!taskExists) return NotFound("Access Denied");

            var comments = await _context.Comments
                .Where(c => c.WorkItemId == workItemId)
                .Include(c => c.CreatedByUser)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Text,
                    c.CreatedAt,
                    AuthorName = c.CreatedByUser != null ? c.CreatedByUser.Name : "System"
                })
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/workitems/{workItemId}/comments
        [HttpPost]
        public async Task<IActionResult> AddComment(Guid workItemId, [FromBody] CreateCommentRequest request)
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null) return Unauthorized();

            var taskExists = await _context.WorkItems.AnyAsync(w => w.Id == workItemId);
            if (!taskExists) return NotFound("Task not found");

            var comment = new Comment
            {
                Text = request.Text,
                WorkItemId = workItemId,
                CreatedByUserId = userId.Value, 
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var authorName = await _context.Users.IgnoreQueryFilters().Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync();

            return Ok(new
            {
                comment.Id,
                comment.Text,
                comment.CreatedAt,
                AuthorName = authorName ?? "Me"
            });
        }
    }

    public class CreateCommentRequest
    {
        public string Text { get; set; }
    }
}