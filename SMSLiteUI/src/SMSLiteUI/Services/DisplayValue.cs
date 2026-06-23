namespace SMSLiteUI.Services;

public static class DisplayValue
{
    public const string Missing = "--";

    public static string Text(object? value)
    {
        var text = Convert.ToString(value);
        return string.IsNullOrWhiteSpace(text) ? Missing : text;
    }

    public static string Date(DateTime? value, string format = "MM/dd/yyyy")
        => value.HasValue ? value.Value.ToString(format) : Missing;
}
