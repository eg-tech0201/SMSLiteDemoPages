namespace SMSLiteStaticDemo.Models;

public class NotificationItem
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedOn { get; set; }
    public bool IsRead { get; set; }
}
