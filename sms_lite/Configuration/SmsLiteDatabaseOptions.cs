namespace sms_lite.Server.Configuration;

public sealed class SmsLiteDatabaseOptions
{
    public const string SectionName = "SmsLiteDatabase";

    public string Host { get; set; } = "mysqldev.nass.usda.gov";
    public uint Port { get; set; } = 3306;
    public string Database { get; set; } = "sms";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string LogDirectory { get; set; } = "";
}
