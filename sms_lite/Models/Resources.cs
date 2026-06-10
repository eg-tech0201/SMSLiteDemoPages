namespace SMSLiteStaticDemo.Models;

public class SurveyTeamMember
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Cawi { get; set; }
    public bool Cati { get; set; }
    public bool Capi { get; set; }
    public bool Paper { get; set; }
}

public class SurveyTool
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
}
