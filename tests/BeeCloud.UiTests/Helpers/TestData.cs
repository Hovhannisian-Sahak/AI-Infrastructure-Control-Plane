namespace BeeCloud.UiTests.Helpers;

public static class TestData
{
    public static string NodeName() => $"e2e-node-{Guid.NewGuid():N}"[..20];
    public static string NetworkName() => $"e2e-net-{Guid.NewGuid():N}"[..20];
}
