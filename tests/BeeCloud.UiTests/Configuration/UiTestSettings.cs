namespace BeeCloud.UiTests.Configuration;

public static class UiTestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BEECLOUD_UI_URL")
        ?? "http://localhost:3001";

    public static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("BEECLOUD_API_URL")
        ?? "http://localhost:8081";

    public static bool Headless =>
        !string.Equals(
            Environment.GetEnvironmentVariable("BEECLOUD_HEADLESS"),
            "false",
            StringComparison.OrdinalIgnoreCase);

    public static float DefaultTimeoutMs => 10_000;
    public static float ProvisioningTimeoutMs => 90_000;
    public static float LifecycleTimeoutMs => 45_000;
}
