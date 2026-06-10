namespace SMSLiteStaticDemo.Models;

public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime SurveyDate { get; set; }
    public string OmbNumber { get; set; } = "";
    public DateTime OmbExpires { get; set; }
    public string SampleName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime StopDate { get; set; }
    public string Status { get; set; } = "";
}

public class SurveyStats
{
    public int InstrumentsServed { get; set; }
    public int RespondentsSampled { get; set; }
    public int ResponsesToDate { get; set; }
    public decimal ResponseRatePercent { get; set; }
    public string AverageCustomerExperience { get; set; } = "Not available";
}

public class SurveyDocketLink
{
    public int SurveyId { get; set; }
    public string DocketId { get; set; } = "";
    public string DocketStatus { get; set; } = "";
    public DateTime LinkedOn { get; set; }
}
