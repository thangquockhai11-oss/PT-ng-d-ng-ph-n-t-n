using System.ComponentModel.DataAnnotations;

namespace Support.API.DTOs.Ticket;

public class AssignTicketRequest
{
    [Range(1, int.MaxValue)]
    public int AgentId { get; set; }
}
