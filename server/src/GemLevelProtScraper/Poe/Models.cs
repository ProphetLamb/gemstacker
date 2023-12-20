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
