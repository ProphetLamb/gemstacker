using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.Poe;

public sealed record PoeLeaugeList(string ApiUrl);
[BsonIgnoreExtraElements]
public sealed record PoeLeauge(string Name, string Text, Realm Realm, LeaugeMode Mode);

[Flags]
public enum LeaugeMode { None, Standard, Softcore, Hardcore, Ruthless, HardcoreRuthless }
public enum Realm { Pc, Xbox, Sony }
