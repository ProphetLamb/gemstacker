using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.Poe;

public sealed record PoeLeagueList(string ApiUrl);
internal record struct PoeLeagueListRepsonse(ImmutableArray<PoeLeagueListResponseItem> Result);
internal sealed record PoeLeagueListResponseItem(string Id, string Realm, string Text);
public sealed record PoeLeagueListCompleted();

[BsonIgnoreExtraElements]
public sealed record PoeLeague(string Name, string Text, Realm Realm, LeagueMode Mode);

[Flags]
public enum LeagueMode
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
    public static ImmutableArray<LeagueMode> WellknownLeagues { get; } = ImmutableArray.Create([
        LeagueMode.League | LeagueMode.Softcore,
        LeagueMode.League | LeagueMode.Hardcore,
        LeagueMode.League | LeagueMode.HardcoreRuthless,
        LeagueMode.Standard | LeagueMode.Softcore,
        LeagueMode.Standard | LeagueMode.Hardcore,
        LeagueMode.Standard | LeagueMode.HardcoreRuthless
    ]);

    public static bool TryParse(ReadOnlySpan<char> text, ReadOnlySpan<char> league, out LeagueMode mode)
    {
        mode = LeagueMode.None;
        if (IsEqual(text, league))
        {
            mode = LeagueMode.League | LeagueMode.Softcore;
        }
        else if (IsEqual(text, $"Hardcore {league}"))
        {
            mode = LeagueMode.League | LeagueMode.Hardcore;
        }
        else if (IsEqual(text, $"Ruthless {league}"))
        {
            mode = LeagueMode.League | LeagueMode.Ruthless;
        }
        else if (IsEqual(text, $"HC Ruthless {league}"))
        {
            mode = LeagueMode.League | LeagueMode.HardcoreRuthless;
        }
        else if (IsEqual(text, "Standard"))
        {
            mode = LeagueMode.Standard | LeagueMode.Softcore;
        }
        else if (IsEqual(text, "Hardcore"))
        {
            mode = LeagueMode.Standard | LeagueMode.Hardcore;
        }
        else if (IsEqual(text, "Ruthless"))
        {
            mode = LeagueMode.Standard | LeagueMode.Ruthless;
        }
        else if (IsEqual(text, "Hardcore Ruthless"))
        {
            mode = LeagueMode.Standard | LeagueMode.HardcoreRuthless;
        }

        return (mode & (LeagueMode.Standard | LeagueMode.League)) != 0;

        static bool IsEqual(ReadOnlySpan<char> probeWithSpace, ReadOnlySpan<char> text)
        {
            var probeTrimmed = probeWithSpace.Trim();
            return text.Equals(probeTrimmed, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static LeagueMode Parse(ReadOnlySpan<char> text, ReadOnlySpan<char> league)
    {
        if (TryParse(text, league, out var mode))
        {
            return mode;
        }
        throw new ArgumentException($"Failed to parse {nameof(LeagueMode)} from text in league `{league}`", nameof(text));
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
