using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkQueue.Domain.Entities;
using WorkQueue.Domain.Enums;
using WorkQueue.Infrastructure;

namespace WorkQueue.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Требуем JWT токен для всех методов
    public class WorkItemsController : ControllerBase
    {
        private readonly WorkQueueDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public WorkItemsController(WorkQueueDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: api/workitems
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? status, [FromQuery] int? priority)
        {
            // Обрати внимание: мы НЕ пишем WHERE OrganizationId = ...
            var query = _context.WorkItems.Include(w => w.AssigneeUser).AsQueryable();

            if (status.HasValue)
                query = query.Where(w => (int)w.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(w => (int)w.Priority == priority.Value);

            var items = await query.Select(w => new
            {
                w.Id,
                w.Title,
                w.Description,
                Status = (int)w.Status,
                Priority = (int)w.Priority,
                w.DueDate,
                AssigneeName = w.AssigneeUser != null ? w.AssigneeUser.Name : null
            }).ToListAsync();

            return Ok(items);
        }

        // POST: api/workitems
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkItemRequest request)
        {
            var orgId = _currentUserService.GetOrganizationId();
            var userId = _currentUserService.GetUserId();

            if (orgId == null || userId == null) return Unauthorized();

            var workItem = new WorkItem
            {
                Title = request.Title,
                Description = request.Description,
                Priority = (WorkItemPriority)request.Priority, // Предполагаем, что у тебя есть enum Priority
                Status = WorkItemStatus.New,          // Новая задача всегда ToDo (или твой enum)
                DueDate = request.DueDate,
                OrganizationId = orgId.Value,
                CreatedByUserId = userId.Value,
                AssigneeUserId = request.AssigneeId
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            return Ok(workItem.Id);
        }

        // PUT: api/workitems/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkItemRequest request)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return NotFound();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = _currentUserService.GetUserId();

            // Проверка бизнес-логики по ТЗ
            if (userRole == "Manager")
            {
                // Менеджер может менять всё
                workItem.Title = request.Title;
                workItem.Description = request.Description;
                workItem.Priority = (WorkItemPriority)request.Priority;
                workItem.Status = (WorkItemStatus)request.Status;
                workItem.DueDate = request.DueDate;
                workItem.AssigneeUserId = request.AssigneeId;
            }
            else if (userRole == "Member")
            {
                // Member может менять только статус, и только если он исполнитель
                if (workItem.AssigneeUserId != userId)
                {
                    return Forbid("Вы не можете менять чужую задачу.");
                }

                workItem.Status = (WorkItemStatus)request.Status;
            }
            else
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    // (DTO) 
    public class CreateWorkItemRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssigneeId { get; set; }
    }

    public class UpdateWorkItemRequest : CreateWorkItemRequest
    {
        public int Status { get; set; }
    }
}