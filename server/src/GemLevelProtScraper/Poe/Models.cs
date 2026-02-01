using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.Poe;

public sealed record PoeLeagueList(string ApiUrl);
internal record struct PoeLeagueListRepsonse(ImmutableArray<PoeLeagueListResponseItem> Result);
internal sealed record PoeLeagueListResponseItem(string Id, string Realm, string Text);
public sealed record PoeLeagueListCompleted();

[BsonIgnoreExtraElements]
public sealed record PoeLeague(string Name, string Text, Realm Realm);

public enum Realm { Pc, Xbox, Sony }

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
