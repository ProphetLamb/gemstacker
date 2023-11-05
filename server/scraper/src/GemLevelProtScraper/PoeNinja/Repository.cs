using GemLevelProtScraper.PoeDb;
using Microsoft.Extensions.Options;
using MongoDB.Migration;

namespace GemLevelProtScraper.PoeNinja;

public sealed class PoeNinjaRepository(IOptions<PoeNinjaDatabaseSettings> settings, IMongoMigrationCompletion completion)
{
}
