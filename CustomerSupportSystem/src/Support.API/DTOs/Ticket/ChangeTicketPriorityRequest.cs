using System.ComponentModel.DataAnnotations;
using Support.Shared.Enums;

namespace Support.API.DTOs.Ticket;

public class ChangeTicketPriorityRequest
{
    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; set; }
}
