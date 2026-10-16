
namespace RaceDay.Api.Models;

public class SupportingDocument
{
    public int DocumentId { get; set; }

    public int EnrId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";
}
