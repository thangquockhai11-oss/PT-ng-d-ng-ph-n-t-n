using Support.Shared.Enums;

namespace Support.Shared.Models;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Tickets do người dùng tạo
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

    // Tickets được giao cho Agent
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
}