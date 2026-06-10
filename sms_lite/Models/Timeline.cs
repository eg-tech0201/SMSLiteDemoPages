namespace SMSLiteStaticDemo.Models;

public class Milestone
{
    public string Title { get; set; } = "";
    public DateTime HqStart { get; set; }
    public DateTime HqEnd { get; set; }
    public DateTime? RfoStart { get; set; }
    public DateTime? RfoEnd { get; set; }
    public bool IsInherited { get; set; }
}
