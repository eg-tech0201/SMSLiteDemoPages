namespace SMSLiteUI.Services;

public static class SmsLiteAuthorization
{
    public static class Roles
    {
        public const string Admin = "SMSLite.Admin";
        public const string SurveyViewer = "SMSLite.SurveyViewer";
        public const string SurveyEditor = "SMSLite.SurveyEditor";
    }

    public static class Policies
    {
        public const string CanViewSurveys = "CanViewSurveys";
        public const string CanEditSurveys = "CanEditSurveys";
        public const string CanAdministerSurveys = "CanAdministerSurveys";
    }
}
