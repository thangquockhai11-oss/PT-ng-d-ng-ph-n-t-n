using Support.Shared.Enums;
namespace Support.Shared.Models;

public class Ticket
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // Người tạo Ticket
    public int CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;

    // Agent được giao
    public int? AssignedToId { get; set; }

    public User? AssignedTo { get; set; }

    // Danh mục
    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    // Lịch sử Ticket
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
}