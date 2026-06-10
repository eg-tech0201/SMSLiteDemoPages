namespace SMSLiteStaticDemo.Models;

public class SurveyRecord
{
    public int RecordId { get; set; }
    public string RespondentId { get; set; } = "";
    public string County { get; set; } = "";
    public string State { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public string Survey { get; set; } = "";
}

public class RecordAnnotation
{
    public int RecordId { get; set; }
    public string Note { get; set; } = "";
    public string Tag { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public DateTime UpdatedOn { get; set; }
}

public class AuditEntry
{
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
}
