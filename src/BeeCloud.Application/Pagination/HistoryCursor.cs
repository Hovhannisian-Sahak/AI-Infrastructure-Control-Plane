using System.Globalization;

namespace BeeCloud.Application.Pagination;

public readonly record struct HistoryCursor(DateTime RecordedAt, Guid Id);

public static class HistoryCursorCodec
{
    public static HistoryCursor Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            throw new ArgumentException("Invalid history cursor.", nameof(value));

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(
                    value.Replace('-', '+').Replace('_', '/') +
                    new string('=', (4 - value.Length % 4) % 4)));
            var parts = decoded.Split(':');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParseExact(parts[1], "N", out var id))
            {
                throw new ArgumentException("Invalid history cursor.", nameof(value));
            }

            return new HistoryCursor(new DateTime(ticks, DateTimeKind.Utc), id);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Invalid history cursor.", nameof(value), exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException("Invalid history cursor.", nameof(value), exception);
        }
    }

    public static string Encode(DateTime timestamp, Guid id)
    {
        var value = $"{timestamp.Ticks}:{id:N}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

}
