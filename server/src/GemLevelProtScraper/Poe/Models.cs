using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.Poe;

public sealed record PoeLeaugeList(string ApiUrl);
[BsonIgnoreExtraElements]
public sealed record PoeLeauge(string Name, string Text, Realm Realm, LeaugeMode Mode);

[Flags]
public enum LeaugeMode
{
    None = 0,
    Standard = 1 << 0,
    League = 1 << 1,
    Softcore = 1 << 2,
    Hardcore = 1 << 3,
    Ruthless = 1 << 4,
    HardcoreRuthless = Hardcore | Ruthless
}
public enum Realm { Pc, Xbox, Sony }

public static class LeagueModeHelper
{
    public static bool TryParse(ReadOnlySpan<char> text, ReadOnlySpan<char> league, out LeaugeMode mode)
    {
        mode = LeaugeMode.None;
        if (IsEqual(text, league))
        {
            mode = LeaugeMode.League | LeaugeMode.Softcore;
        }
        else if (IsEqual(text, $"Hardcore {league}"))
        {
            mode = LeaugeMode.League | LeaugeMode.Hardcore;
        }
        else if (IsEqual(text, $"Ruthless {league}"))
        {
            mode = LeaugeMode.League | LeaugeMode.Ruthless;
        }
        else if (IsEqual(text, $"HC Ruthless {league}"))
        {
            mode = LeaugeMode.League | LeaugeMode.HardcoreRuthless;
        }
        else if (IsEqual(text, "Standard"))
        {
            mode = LeaugeMode.Standard | LeaugeMode.Softcore;
        }
        else if (IsEqual(text, "Hardcore"))
        {
            mode = LeaugeMode.Standard | LeaugeMode.Hardcore;
        }
        else if (IsEqual(text, "Ruthless"))
        {
            mode = LeaugeMode.Softcore | LeaugeMode.Ruthless;
        }
        else if (IsEqual(text, "Hardcore Ruthless"))
        {
            mode = LeaugeMode.Softcore | LeaugeMode.Ruthless;
        }

        return (mode & (LeaugeMode.Standard | LeaugeMode.League)) != 0;

        static bool IsEqual(ReadOnlySpan<char> probeWithSpace, ReadOnlySpan<char> text)
        {
            var probeTrimmed = probeWithSpace.Trim();
            return text.Equals(probeTrimmed, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static LeaugeMode Parse(ReadOnlySpan<char> text, ReadOnlySpan<char> league)
    {
        if (TryParse(text, league, out var mode))
        {
            return mode;
        }
        throw new ArgumentException($"Failed to parse {nameof(LeaugeMode)} from text in league `{league}`", nameof(text));
    }
}

public static class RealmHelper
{
    public static bool TryParse(ReadOnlySpan<char> text, out Realm realm)
    {
        if (text.Equals("pc", StringComparison.OrdinalIgnoreCase))
        {
            realm = Realm.Pc;
            return true;
        }
        if (text.Equals("xbox", StringComparison.OrdinalIgnoreCase))
        {
            realm = Realm.Xbox;
            return true;
        }
        if (text.Equals("sony", StringComparison.OrdinalIgnoreCase))
        {
            realm = Realm.Sony;
            return true;
        }

        realm = default;
        return false;
    }

    public static Realm Parse(ReadOnlySpan<char> text)
    {
        if (TryParse(text, out var realm))
        {
            return realm;
        }
        throw new ArgumentException("Unknown realm text", nameof(text));
    }
}
