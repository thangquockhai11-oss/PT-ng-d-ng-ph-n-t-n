using System.ComponentModel.DataAnnotations;
using Support.Shared.Enums;

namespace Support.API.DTOs.Ticket;

public class ChangeTicketStatusRequest
{
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; set; }
    [StringLength(1000)]
    public string? Note { get; set; }
}
