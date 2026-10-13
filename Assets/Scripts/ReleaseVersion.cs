using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class ReleaseVersion
{
    // Canonical build version: yyyyMMdd[.revision]. Release tags also accept 1015ver.
    public static bool TryParse(string value, int year, out long number)
    {
        number = 0;
        var match = Regex.Match(value ?? "", @"^(?:v)?(\d{8}|\d{4})(?:\.(\d{1,4}))?(?:ver)?$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;
        string date = match.Groups[1].Value;
        if (date.Length == 4) date = year.ToString("D4", CultureInfo.InvariantCulture) + date;
        if (!DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _)) return false;
        int revision = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        number = long.Parse(date, CultureInfo.InvariantCulture) * 10000 + revision;
        return true;
    }
}
