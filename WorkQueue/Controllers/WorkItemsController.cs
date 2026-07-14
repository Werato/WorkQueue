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

        // POST: api/workitems/{id}/transition
        [HttpPost("{id}/transition")]
        public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionWorkItemRequest request)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return NotFound();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = _currentUserService.GetUserId();

            // 1. Authorization check: Member can only change their own tasks
            if (userRole == "Member" && workItem.AssigneeUserId != userId)
            {
                return StatusCode(403, "You cannot change the status of another user's task.");
            }

            var currentStatus = workItem.Status;
            var targetStatus = (WorkItemStatus)request.NewStatus;

            if (currentStatus == targetStatus) return Ok();

            // 2. Business rule: Member cannot reopen a Done task
            if (currentStatus == WorkItemStatus.Done && userRole == "Member")
            {
                return StatusCode(403, "Only a Manager can reopen a completed task.");
            }

            // 3. State Machine transitions
            bool isValidTransition = (currentStatus, targetStatus) switch
            {
                (WorkItemStatus.New, WorkItemStatus.InProgress) => true,

                (WorkItemStatus.InProgress, WorkItemStatus.Done) => true,
                (WorkItemStatus.InProgress, WorkItemStatus.Blocked) => true,

                (WorkItemStatus.Blocked, WorkItemStatus.InProgress) => true,

                (WorkItemStatus.Done, WorkItemStatus.InProgress) when userRole == "Manager" => true,

                _ => false
            };

            if (!isValidTransition)
            {
                return BadRequest($"Invalid status transition from {currentStatus} to {targetStatus}.");
            }

            // 4. Apply changes
            workItem.Status = targetStatus;

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
        // POST: api/workitems/{id}/assign
        [HttpPost("{id}/assign")]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignWorkItemRequest request)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // 1. Business rule: Only Manager can assign tasks
            if (userRole != "Manager")
            {
                return StatusCode(403, "Only a Manager can assign tasks.");
            }

            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return NotFound();

            // 2. Business rule: Assignee must be from the same Organization
            // EF Core Global Query Filter on 'User' entity automatically ensures this.
            // If the user belongs to another tenant, FindAsync will return null.
            var assignee = await _context.Users.FindAsync(request.AssigneeId);
            if (assignee == null)
            {
                return BadRequest("Target user not found or does not belong to your organization.");
            }

            // 3. Apply assignment
            workItem.AssigneeUserId = request.AssigneeId;

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
}