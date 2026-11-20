using Support.Shared.Enums;

namespace Support.Shared.Models;

public class TicketHistory
{
    public int Id { get; set; }

    // Ticket liên quan
    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    // Người thực hiện thay đổi
    public int ChangedById { get; set; }

    public User ChangedBy { get; set; } = null!;

    // Nội dung hành động
    public string Action { get; set; } = string.Empty;

    public string? Note { get; set; }

    // Trạng thái trước khi thay đổi
    public TicketStatus? OldStatus { get; set; }

    // Trạng thái sau khi thay đổi
    public TicketStatus? NewStatus { get; set; }

    // Thời điểm thay đổi
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}