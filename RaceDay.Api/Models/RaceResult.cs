
namespace RaceDay.Api.Models;

public class RaceResult
{
    public int ResultId { get; set; }

    public int EnrId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    public int? Position { get; set; }

    public bool Published { get; set; }
}
