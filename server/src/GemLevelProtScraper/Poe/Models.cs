namespace GemLevelProtScraper.Poe;

public sealed record PoeLeaugeList(string ApiUrl);
public sealed record PoeLeauge(string Id, string Text, Realm Realm, LeaugeMode Mode);

[Flags]
public enum LeaugeMode { None, Standard, Softcore, Hardcore, Ruthless, HardcoreRuthless }
public enum Realm { Pc, Xbox, Sony }
