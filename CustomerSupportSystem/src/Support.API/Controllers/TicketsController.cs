using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Support.API.Data;
using Support.API.DTOs.Ticket;
using Support.Shared.Enums;
using Support.Shared.Models;

namespace Support.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController(ApplicationDbContext context) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IQueryable<Ticket> FullTicket(ApplicationDbContext db) => db.Tickets
        .Include(x => x.Category).Include(x => x.CreatedBy).Include(x => x.AssignedTo)
        .Include(x => x.Histories).ThenInclude(x => x.ChangedBy);

    private static object ToResponse(Ticket x) => new
    {
        x.Id, x.Title, x.Description, status = x.Status.ToString(), priority = x.Priority.ToString(),
        x.CreatedAt, x.UpdatedAt, x.ResolvedAt, x.CreatedById,
        createdBy = x.CreatedBy is null ? null : new { x.CreatedBy.Id, x.CreatedBy.FullName, x.CreatedBy.Email },
        x.AssignedToId,
        assignedTo = x.AssignedTo is null ? null : new { x.AssignedTo.Id, x.AssignedTo.FullName },
        x.CategoryId,
        category = x.Category is null ? null : new { x.Category.Id, x.Category.Name },
        histories = x.Histories.OrderBy(h => h.ChangedAt).Select(h => new
        {
            h.Id, h.Action, h.Note, oldStatus = h.OldStatus?.ToString(), newStatus = h.NewStatus?.ToString(),
            h.ChangedAt, h.ChangedById, changedBy = h.ChangedBy == null ? null : h.ChangedBy.FullName
        })
    };

    [HttpPost, Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        if (!await context.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return BadRequest(new { message = "Category không tồn tại." });
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Title = request.Title.Trim(), Description = request.Description.Trim(), CategoryId = request.CategoryId,
            CreatedById = CurrentUserId, Status = TicketStatus.New, Priority = TicketPriority.Medium,
            CreatedAt = now, UpdatedAt = now
        };
        ticket.Histories.Add(new TicketHistory
        {
            ChangedById = CurrentUserId, Action = "Ticket Created", Note = "Khách hàng tạo Ticket",
            NewStatus = TicketStatus.New, ChangedAt = now
        });
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
        var created = await FullTicket(context).FirstAsync(x => x.Id == ticket.Id);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToResponse(created));
    }

    [HttpGet("my"), Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMine() => Ok((await FullTicket(context)
        .Where(x => x.CreatedById == CurrentUserId).OrderByDescending(x => x.CreatedAt)
        .ToListAsync()).Select(ToResponse));

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll() => Ok((await FullTicket(context)
        .OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(ToResponse));

    [HttpGet("assigned"), Authorize(Roles = "Agent")]
    public async Task<IActionResult> GetAssigned() => Ok((await FullTicket(context)
        .Where(x => x.AssignedToId == CurrentUserId).OrderByDescending(x => x.CreatedAt)
        .ToListAsync()).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await FullTicket(context).FirstOrDefaultAsync(x => x.Id == id);
        if (ticket is null) return NotFound(new { message = "Không tìm thấy Ticket." });
        var allowed = User.IsInRole(nameof(UserRole.Admin)) ||
            (User.IsInRole(nameof(UserRole.Customer)) && ticket.CreatedById == CurrentUserId) ||
            (User.IsInRole(nameof(UserRole.Agent)) && ticket.AssignedToId == CurrentUserId);
        return allowed ? Ok(ToResponse(ticket)) : Forbid();
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Customer")]
    public async Task<IActionResult> Update(int id, UpdateTicketRequest request)
    {
        var ticket = await context.Tickets.Include(x => x.Histories).FirstOrDefaultAsync(x => x.Id == id);
        if (ticket is null) return NotFound(new { message = "Không tìm thấy Ticket." });
        if (ticket.CreatedById != CurrentUserId) return Forbid();
        if (ticket.Status != TicketStatus.New) return Conflict(new { message = "Chỉ có thể sửa Ticket ở trạng thái New." });
        if (!await context.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return BadRequest(new { message = "Category không tồn tại." });
        ticket.Title = request.Title.Trim(); ticket.Description = request.Description.Trim();
        ticket.CategoryId = request.CategoryId; ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Histories.Add(new TicketHistory { ChangedById = CurrentUserId, Action = "Ticket Updated", Note = "Khách hàng cập nhật nội dung", ChangedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        return Ok(ToResponse(await FullTicket(context).FirstAsync(x => x.Id == id)));
    }

    [HttpPut("{id:int}/assign"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(int id, AssignTicketRequest request)
    {
        var ticket = await context.Tickets.Include(x => x.Histories).FirstOrDefaultAsync(x => x.Id == id);
        if (ticket is null) return NotFound(new { message = "Không tìm thấy Ticket." });
        if (ticket.Status == TicketStatus.Resolved) return Conflict(new { message = "Không thể phân công Ticket đã giải quyết." });
        var agent = await context.Users.FirstOrDefaultAsync(x => x.Id == request.AgentId && x.Role == UserRole.Agent);
        if (agent is null) return BadRequest(new { message = "Agent không tồn tại." });
        var oldStatus = ticket.Status; var now = DateTime.UtcNow;
        ticket.AssignedToId = agent.Id; ticket.Status = TicketStatus.Assigned; ticket.UpdatedAt = now; ticket.ResolvedAt = null;
        ticket.Histories.Add(new TicketHistory { ChangedById = CurrentUserId, Action = "Ticket Assigned", Note = $"Ticket được phân công cho {agent.FullName}", OldStatus = oldStatus, NewStatus = TicketStatus.Assigned, ChangedAt = now });
        await context.SaveChangesAsync();
        return Ok(ToResponse(await FullTicket(context).FirstAsync(x => x.Id == id)));
    }

    [HttpPut("{id:int}/status"), Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeTicketStatusRequest request)
    {
        var ticket = await context.Tickets.Include(x => x.Histories).FirstOrDefaultAsync(x => x.Id == id);
        if (ticket is null) return NotFound(new { message = "Không tìm thấy Ticket." });
        if (User.IsInRole(nameof(UserRole.Agent)) && ticket.AssignedToId != CurrentUserId) return Forbid();
        var valid = ticket.Status switch
        {
            TicketStatus.New => request.Status == TicketStatus.Assigned,
            TicketStatus.Assigned => request.Status == TicketStatus.InProgress,
            TicketStatus.InProgress => request.Status == TicketStatus.Resolved,
            TicketStatus.Resolved => request.Status == TicketStatus.InProgress,
            _ => false
        };
        if (!valid) return BadRequest(new { message = "Chuyển trạng thái Ticket không hợp lệ." });
        var old = ticket.Status; var now = DateTime.UtcNow;
        ticket.Status = request.Status; ticket.UpdatedAt = now;
        ticket.ResolvedAt = request.Status == TicketStatus.Resolved ? now : null;
        ticket.Histories.Add(new TicketHistory { ChangedById = CurrentUserId, Action = "Status Changed", Note = request.Note, OldStatus = old, NewStatus = request.Status, ChangedAt = now });
        await context.SaveChangesAsync();
        return Ok(ToResponse(await FullTicket(context).FirstAsync(x => x.Id == id)));
    }

    [HttpPut("{id:int}/priority"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangePriority(int id, ChangeTicketPriorityRequest request)
    {
        var ticket = await context.Tickets.Include(x => x.Histories).FirstOrDefaultAsync(x => x.Id == id);
        if (ticket is null) return NotFound(new { message = "Không tìm thấy Ticket." });
        var old = ticket.Priority; var now = DateTime.UtcNow;
        ticket.Priority = request.Priority; ticket.UpdatedAt = now;
        ticket.Histories.Add(new TicketHistory { ChangedById = CurrentUserId, Action = "Priority Changed", Note = $"Mức ưu tiên: {old} → {request.Priority}", ChangedAt = now });
        await context.SaveChangesAsync();
        return Ok(ToResponse(await FullTicket(context).FirstAsync(x => x.Id == id)));
    }
}
