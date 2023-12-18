namespace GemLevelProtScraper.Poe;

public sealed record UpdatePoeLeauges(string ApiUrl);
public sealed record PoeLeauge(string Id, string Text, Realm Realm, LeaugeMode Mode);

[Flags]
public enum LeaugeMode { Unknown, Standard, Softcore, Hardcore, Ruthless, HardcoreRuthless }
public enum Realm { Pc, Xbox, Sony }
