namespace BeeCloud.ApiTests;

public static class TestConfiguration
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BEE_CLOUD_API_URL")
        ?? "http://localhost:5141";
}