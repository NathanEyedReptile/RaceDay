
namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrId { get; set; }

    public int Participant { get; set; }

    public int EventId { get; set; }

    public DateTime EnrDate { get; set; }

    public string Status { get; set; } = "Pending";

    public int CategoryId { get; set; }
}
