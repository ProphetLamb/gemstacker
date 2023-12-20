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
    Softcore = 1 << 1,
    Hardcore = 1 << 2,
    Ruthless = 1 << 3,
    HardcoreRuthless = Hardcore | Ruthless
}
public enum Realm { Pc, Xbox, Sony }

public static class LeagueModeHelper
{
    public static bool TryParse(ReadOnlySpan<char> text, out LeaugeMode mode)
    {
        mode = default;
        if (EqualsTrimmedOrStartsWith("HC Ruthless ", text) || EqualsTrimmedOrStartsWith("Hardcore Ruthless ", text))
        {
            mode |= LeaugeMode.HardcoreRuthless;
        }
        else if (EqualsTrimmedOrStartsWith("Hardcore ", text))
        {
            mode |= LeaugeMode.Hardcore;
        }
        else if (EqualsTrimmedOrStartsWith("Ruthless ", text))
        {
            mode |= LeaugeMode.Ruthless;
        }
        else
        {
            mode |= LeaugeMode.Softcore;
        }

        if (EqualsTrimmedOrEndsWith(" Standard", text))
        {
            mode |= LeaugeMode.Standard;
        }
        return true;

        static bool EqualsTrimmedOrStartsWith(ReadOnlySpan<char> probeWithSpace, ReadOnlySpan<char> text)
        {
            var probeTrimmed = probeWithSpace.Trim();
            return text.StartsWith(probeWithSpace, StringComparison.InvariantCultureIgnoreCase)
                || text.Equals(probeTrimmed, StringComparison.InvariantCultureIgnoreCase);
        }
        static bool EqualsTrimmedOrEndsWith(ReadOnlySpan<char> probeWithSpace, ReadOnlySpan<char> text)
        {
            var probeTrimmed = probeWithSpace.Trim();
            return text.EndsWith(probeWithSpace, StringComparison.InvariantCultureIgnoreCase)
                || text.Equals(probeTrimmed, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static LeaugeMode Parse(ReadOnlySpan<char> text)
    {
        if (TryParse(text, out var mode))
        {
            return mode;
        }
        throw new ArgumentException($"Failed to parse {nameof(LeaugeMode)} from text", nameof(text));
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
