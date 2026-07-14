using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkQueue.Domain.Entities;
using WorkQueue.Domain.Enums;
using WorkQueue.DTO;
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
        //[HttpGet]
        //public async Task<IActionResult> Get([FromQuery] int? status, [FromQuery] int? priority)
        //{
        //    // WHERE OrganizationId = ...
        //    var query = _context.WorkItems.Include(w => w.AssigneeUser).AsQueryable();

        //    if (status.HasValue)
        //        query = query.Where(w => (int)w.Status == status.Value);

        //    if (priority.HasValue)
        //        query = query.Where(w => (int)w.Priority == priority.Value);

        //    var items = await query.Select(w => new
        //    {
        //        w.Id,
        //        w.Title,
        //        w.Description,
        //        Status = (int)w.Status,
        //        Priority = (int)w.Priority,
        //        w.DueDate,
        //        AssigneeId = w.AssigneeUser != null ? w.AssigneeUser.Id : (Guid?)null,
        //        AssigneeName = w.AssigneeUser != null ? w.AssigneeUser.Name : null
        //    }).ToListAsync();

        //    return Ok(items);
        //}
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] WorkItemQueryParameters parameters)
        {
            // EF Core Global Query Filter handles Tenant Isolation automatically
            var query = _context.WorkItems
                .Include(w => w.AssigneeUser)
                .AsQueryable();

            // Search by title or description
            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var searchTerm = parameters.Search.ToLower();
                query = query.Where(w =>
                    w.Title.ToLower().Contains(searchTerm) ||
                    (w.Description != null && w.Description.ToLower().Contains(searchTerm)));
            }

            // Filter by status, priority, and assignee
            if (parameters.Status.HasValue)
                query = query.Where(w => (int)w.Status == parameters.Status.Value);

            if (parameters.Priority.HasValue)
                query = query.Where(w => (int)w.Priority == parameters.Priority.Value);

            if (parameters.AssigneeId.HasValue)
                query = query.Where(w => w.AssigneeUserId == parameters.AssigneeId.Value);

            // Sorting
            query = parameters.SortBy?.ToLower() switch
            {
                "createddate" => query.OrderBy(w => w.CreatedAt),
                "duedate" => query.OrderBy(w => w.DueDate),
                "priority" => query.OrderBy(w => w.Priority),
                "status" => query.OrderBy(w => w.Status),
                _ => query.OrderByDescending(w => w.CreatedAt) // Default
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var items = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(w => new
                {
                    w.Id,
                    w.Title,
                    w.Description,
                    Status = (int)w.Status,
                    Priority = (int)w.Priority,
                    w.DueDate,
                    AssigneeId = w.AssigneeUser != null ? w.AssigneeUser.Id : (Guid?)null,
                    AssigneeName = w.AssigneeUser != null ? w.AssigneeUser.Name : null
                }).ToListAsync();

            var response = new PagedResponse<object>
            {
                Items = items,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };

            return Ok(response);
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

        // Patch: api/workitems/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkItemRequest request)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return NotFound();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = _currentUserService.GetUserId();

            // Проверка бизнес-логики по ТЗ
            if (userRole == "Manager")
            {
                workItem.Title = request.Title;
                workItem.Description = request.Description;
                workItem.Priority = (WorkItemPriority)request.Priority;
                workItem.Status = (WorkItemStatus)request.Status;
                workItem.DueDate = request.DueDate;
                workItem.AssigneeUserId = request.AssigneeId;
            }
            else if (userRole == "Member")
            {
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

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("The record was modified by another user.");
            }

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