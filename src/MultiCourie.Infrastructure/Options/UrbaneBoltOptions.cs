namespace MultiCourie.Infrastructure.Options;

public sealed class UrbaneBoltOptions
{
    public const string SectionName = "Couriers:UrbaneBolt";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "https://uat.urbanebolt.in";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;
}
