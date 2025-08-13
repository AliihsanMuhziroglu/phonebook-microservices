namespace ReportService.Models;

public class Report
{
    public Guid UUID { get; set; } = Guid.NewGuid();
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public ReportStatus Status { get; set; } = ReportStatus.Preparing;
    public List<ReportItem> Items { get; set; } = new();
}

public class ReportItem
{
    public Guid UUID { get; set; } = Guid.NewGuid();
    public Guid ReportUUID { get; set; }
    public string Location { get; set; } = default!;
    public int PersonCount { get; set; }
    public int PhoneCount { get; set; }
}
