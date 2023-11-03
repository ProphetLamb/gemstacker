using System.Reflection;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MessagePipe;

namespace GemLevelProtScraper.Migrations;

[BsonIgnoreExtraElements]
public sealed record DatabaseVersion(string Database, long Version, DateTimeOffset Started, DateTimeOffset? Completed = null);

public sealed class MigrationDefinitionAttribute(string database, long downVersion, long upVersion) : Attribute
{
    public string? Description { get; set; }

    public string Database => database;
    public long UpVersion => upVersion;
    public long DownVersion => downVersion;
}

public interface IMigration
{
    Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default);
    Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default);
}

internal sealed record Migration(string Database, long UpVersion, long DownVersion, IMigration MigrationService, string? Description = null);

public interface IDatabaseMigratable
{
    DatabaseMigrationSettings GetMigrationSettings();
}

public sealed record DatabaseMigrationSettings : IOptions<DatabaseMigrationSettings>
{
    public required string MirgrationStateCollectionName { get; init; }

    public required DatabaseAlias Database { get; init; }

    public required string ConnectionString { get; init; }

    DatabaseMigrationSettings IOptions<DatabaseMigrationSettings>.Value => this;
}

public sealed record DatabaseMigrationCompleted(string DatabaseName, string DatabaseAlias, long Version);

public sealed class DatabaseMigratableSettings(ImmutableArray<Type> types)
{
    public ImmutableArray<Type> MigratableTypes => types;
}

public static class MigrationExtensions
{
    public static IServiceCollection AddMigrations(this IServiceCollection services)
    {
        return services.AddMigrations(Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    }

    public static IServiceCollection AddMigrations(this IServiceCollection services, params Type[] typesInAssemblies)
    {
        var assemblies = typesInAssemblies
            .Select(t => t.Assembly)
            .Distinct()
            .ToArray();
        return services.AddMigrations(assemblies);
    }

    public static IServiceCollection AddMigrations(this IServiceCollection services, params Assembly[] assemblies)
    {
        var mirgationTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t
                => !t.IsAbstract
                && !t.IsInterface
                && t.IsAssignableTo(typeof(IMigration))
                && t.GetCustomAttribute<MigrationDefinitionAttribute>() is { }
            );

        foreach (var migrationType in mirgationTypes)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IMigration), migrationType, ServiceLifetime.Scoped));
        }

        DatabaseMigratableSettings databaseMigratables = new(
            services
                .Select(d => d.ServiceType)
                .Where(type =>
                {
                    if (!type.IsGenericType)
                    {
                        return false;
                    }

                    if (type.IsAssignableTo(typeof(IDatabaseMigratable)))
                    {
                        return true;
                    }

                    if (type.GetGenericTypeDefinition() != typeof(IOptions<>))
                    {
                        return false;
                    }

                    var optionsType = type.GetGenericArguments()[0];
                    return optionsType.IsAssignableTo(typeof(IDatabaseMigratable));
                })
                .Distinct()
                .ToImmutableArray()
        );

        return services
            .AddSingleton<IMigrationCompletionReciever, MigrationCompletionService>()
            .AddSingleton<IMigrationCompletion>(sp => sp
                .GetServices<IMigrationCompletionReciever>()
                .SelectTruthy(service => service as MigrationCompletionService)
                .Last()
            )
            .AddSingleton(databaseMigratables)
            .AddSingleton<DatabaseMigrationService>();
    }
}



public interface IMigrationCompletion
{
    ValueTask<DatabaseMigrationCompleted> WaitAsync(string databaseAlias);
}

public interface IMigrationCompletionReciever
{
    void Handle(DatabaseMigrationCompleted message);
}

internal sealed class MigrationCompletionService : IMigrationCompletion, IMigrationCompletionReciever
{
    private readonly SortedList<string, DatabaseMigrationCompleted> _completedMigrations = [];
    private readonly Dictionary<string, TaskCompletionSource<DatabaseMigrationCompleted>> _migrationCompletions = [];

    private void AddToCompletion(DatabaseMigrationCompleted migration)
    {
        lock (_completedMigrations)
        {
            _completedMigrations.Add(migration.DatabaseAlias, migration);
            if (_migrationCompletions.TryGetValue(migration.DatabaseAlias, out var completion))
            {
                _ = completion.TrySetResult(migration);
                _ = _migrationCompletions.Remove(migration.DatabaseAlias);
            }
        }
    }

    public ValueTask<DatabaseMigrationCompleted> WaitAsync(string databaseAlias)
    {
        lock (_completedMigrations)
        {
            if (_completedMigrations.TryGetValue(databaseAlias, out var migration))
            {
                return new(migration);
            }
            if (!_migrationCompletions.TryGetValue(databaseAlias, out var completion))
            {
                completion = new();
                _migrationCompletions[databaseAlias] = completion;
            }
            return new(completion.Task);
        }
    }

    public void Handle(DatabaseMigrationCompleted message)
    {
        AddToCompletion(message);
    }
}

public sealed class DatabaseMigrationService(DatabaseMigratableSettings databaseMigratables, IServiceProvider serviceProvider, IMigrationCompletionReciever migrationCompletedPublisher, ILogger<DatabaseMigrationService> logger, ISystemClock? clock = null)
    : BackgroundService
{
    private static async Task<(long Count, DatabaseVersion? First, DatabaseVersion? Current)> GetMigrationStateAsync(IMongoCollection<DatabaseVersion> collection, string databaseName, CancellationToken cancellationToken)
    {
        var migrationsCount = await collection
            .Find(migration => migration.Database == databaseName)
            .CountDocumentsAsync(cancellationToken)
            .ConfigureAwait(false);
        var firstMigration = await collection
            .Find(migration => migration.Database == databaseName)
            .SortBy(migration => migration.Version)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        var currentMigration = await collection
            .Find(migration => migration.Database == databaseName)
            .SortByDescending(migration => migration.Version)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return (migrationsCount, firstMigration, currentMigration);
    }

    private Migration? ToMigrationOrDefault(IMigration service)
    {
        var serviceType = service.GetType();
        if (serviceType.GetCustomAttribute<MigrationDefinitionAttribute>() is not { } migrationDefinition)
        {
            return null;
        }
        return new(
            migrationDefinition.Database,
            migrationDefinition.UpVersion,
            migrationDefinition.DownVersion,
            service,
            migrationDefinition.Description
        );
    }

    public async Task UpToLatestAsync(DatabaseMigrationSettings options, CancellationToken stoppingToken)
    {
        long migratedVersion;
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            migratedVersion = await MigrateToLatestScoped(scope, options, stoppingToken).ConfigureAwait(false);
        }
        migrationCompletedPublisher.Handle(
            new(
                options.Database.Name,
                options.Database.Alias,
                migratedVersion
            )
        );

        async Task<long> MigrateToLatestScoped(AsyncServiceScope scope, DatabaseMigrationSettings options, CancellationToken stoppingToken)
        {
            logger.LogInformation("Determine available migration");

            var databaseName = options.Database.Name;
            var databaseAlias = options.Database.Alias;

            var migrations = scope.ServiceProvider.GetServices<IMigration>()
                .SelectTruthy(ToMigrationOrDefault)
                .Where(migration => migration.Database == databaseAlias)
                .ToImmutableArray();

            logger.LogInformation(
                "Found {MigrationCount} locally available migrations for {Database} from {LowestVersion} to {HighestVersion}",
                migrations.Length,
                databaseName,
                migrations.Select(m => m.DownVersion).Min(),
                migrations.Select(m => m.UpVersion).Max()
            );

            logger.LogInformation("Determine database migration state");

            MongoClient client = new(options.ConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<DatabaseVersion>(options.MirgrationStateCollectionName);

            var (migrationsCount, firstMigration, currentMigration) = await GetMigrationStateAsync(collection, databaseAlias, stoppingToken).ConfigureAwait(false);

            logger.LogInformation(
                "Found {MigrationCount} applied migrations for {Database} at version {CurrentVersion} from {LowestVersion} to {HighestVersion}",
                migrationsCount,
                databaseName,
                currentMigration?.Version,
                firstMigration?.Version,
                currentMigration?.Version
            );
            logger.LogInformation(
                "Determine all required migation"
            );
            var requiredMigrations = GetRequiredMigrations(migrations, currentMigration?.Version).ToImmutableArray();
            if (requiredMigrations.IsDefaultOrEmpty)
            {
                return currentMigration?.Version ?? 0;
            }
            logger.LogInformation(
                "Migrating {Database} in {RequiredMigrationsCount} steps {RequiredMigrationsVersions}",
                databaseName,
                requiredMigrations.Length,
                $"{requiredMigrations.First().DownVersion} -> {string.Join(" -> ", requiredMigrations.Select(m => m.UpVersion))}"
            );
            foreach (var migration in requiredMigrations)
            {
                logger.LogDebug(
                    "Begining migration for {Database} from {DownVersion} to {UpVersion}: {MigrationDescription}",
                    migration.Database,
                    migration.DownVersion,
                    migration.UpVersion,
                    migration.Description
                );

                var startedTimestamp = clock?.UtcNow ?? DateTimeOffset.UtcNow;
                DatabaseVersion startedVersion = new(databaseName, migration.UpVersion, startedTimestamp, null);
                await collection.InsertOneAsync(startedVersion, null, stoppingToken).ConfigureAwait(false);

                await migration.MigrationService.UpAsync(database, stoppingToken).ConfigureAwait(false);

                var completedTimestamp = clock?.UtcNow ?? DateTimeOffset.UtcNow;
                _ = await collection.UpdateOneAsync(
                    v => v.Version == startedVersion.Version,
                    Builders<DatabaseVersion>.Update.Set(d => d.Completed, completedTimestamp),
                    null,
                    stoppingToken
                )
                    .ConfigureAwait(false);

                logger.LogDebug(
                    "Completed migration for {Database} from {DownVersion} to {UpVersion}",
                    migration.Database,
                    migration.DownVersion,
                    migration.UpVersion
                );
            }
            logger.LogInformation(
                "Completed migration {Database}"
            );
            return requiredMigrations.Last().UpVersion;
        }
    }

    private static IEnumerable<Migration> GetRequiredMigrations(ImmutableArray<Migration> availableMigrations, long? currentVersion)
    {
        return MigrationGraph.CreateOrDefault(availableMigrations, currentVersion)?.GetMigrationTrace() ?? Enumerable.Empty<Migration>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            GetDatabaseMigratables(databaseMigratables, serviceProvider)
            .Select(m => m.GetMigrationSettings())
            .DistinctBy(s => s.Database.Alias)
            .Select(s => UpToLatestAsync(s, stoppingToken))
        )
            .ConfigureAwait(false);
    }

    private static IEnumerable<IDatabaseMigratable> GetDatabaseMigratables(DatabaseMigratableSettings databaseMigratables, IServiceProvider serviceProvider)
    {
        return databaseMigratables.MigratableTypes
            .Select(serviceProvider.GetServices)
            .SelectMany(s => s)
            .SelectTruthy(CastServiceToDatabaseMigratable);

        static IDatabaseMigratable? CastServiceToDatabaseMigratable(object? service)
        {
            if (service is IDatabaseMigratable m)
            {
                return m;
            }
            var implType = service?.GetType();
            if (implType is null
                || !implType.IsGenericType
                || implType.GetGenericTypeDefinition() != typeof(IOptions<>))
            {
                return null;
            }
            var optionsAccessor = implType
                .GetProperty(nameof(IOptions<object>.Value), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                ?? throw new InvalidOperationException("The type is no U: IOptions<T> where T: IDatabaseMigratable || U: IDatabaseMigratable, or failed to produce a value.");
            var result = (IDatabaseMigratable)(optionsAccessor.GetValue(service) ?? throw new InvalidOperationException("The type is no U: IOptions<T> where T: IDatabaseMigratable || U: IDatabaseMigratable, or failed to produce a value."));
            return result;
        }
    }
}

/// <summary>
/// Computes a continioues path/trace of migration from one version to another.
/// </summary>
/// <param name="orderedMigrations">
/// Available migrations in ascending order <see cref="Migration.DownVersion"/>.
/// The first migration is the start/current migration.
/// </param>
sealed file class MigrationGraph
{
    private readonly IReadOnlyDictionary<long, ImmutableArray<Node>> _migrationByDownVersion;
    private readonly IReadOnlyDictionary<long, ImmutableArray<Node>> _migrationByUpVersion;
    private readonly long _startVersion;
    private readonly long _endVersion;
    private readonly ImmutableArray<Migration> _orderedMigrations;

    public MigrationGraph(ImmutableArray<Migration> migrations, long startVersion, long endVersion)
    {
        _orderedMigrations = migrations;
        _migrationByDownVersion = migrations
            .ToImmutableMap(m => m.DownVersion, m => new Node(m));
        _migrationByUpVersion = migrations
            .ToImmutableMap(m => m.UpVersion, m => NodesByDown(m.DownVersion).First(other => ReferenceEquals(other.Migration, m)));
        _startVersion = startVersion;
        _endVersion = endVersion;
    }

    /// <summary>
    /// Creates a <see cref="MigrationGraph"/> from an arbitrary sequence of migrations, starting at <see cref="Migration.DownVersion"/> greater or equal to <paramref name="currentVersion"/> and ending with <see cref="Migration.UpVersion"/> less then or equal to <paramref name="targetVersion"/> if specified.
    /// </summary>
    /// <param name="migrations">The sequence of migrations.</param>
    /// <param name="currentVersion">The minimum respected <see cref="Migration.DownVersion"/>.</param>
    /// <param name="targetVersion">The maximum respected <see cref="Migration.UpVersion"/>.</param>
    /// <returns></returns>
    public static MigrationGraph? CreateOrDefault(IEnumerable<Migration> migrations, long? currentVersion, long? targetVersion = null)
    {
        SortedList<long, Migration> orderedMigrations = [];
        foreach (var migration in migrations)
        {
            if ((currentVersion is { } c && c > migration.DownVersion)
                || (targetVersion is { } t && t < migration.UpVersion))
            {
                continue;
            }
            orderedMigrations.Add(migration.DownVersion, migration);
        }
        if (orderedMigrations.Count == 0)
        {
            return null;
        }
        return new(orderedMigrations.Values.ToImmutableArray(), migrations.First().DownVersion, migrations.Max(m => m.UpVersion));
    }

    private ImmutableArray<Node> NodesByDown(long version)
    {
        return _migrationByDownVersion.TryGetValue(version, out var nodes) ? nodes : ImmutableArray<Node>.Empty;
    }

    private ImmutableArray<Node> NodesByUp(long version)
    {
        return _migrationByUpVersion.TryGetValue(version, out var nodes) ? nodes : ImmutableArray<Node>.Empty;
    }

    /// <summary>
    /// Dijkstra algorithm.
    /// Trace linked list <see cref="Node.Previous"/>
    /// Distance to start <see cref="Node.Distance"/>
    /// </summary>
    private void TraceDistance()
    {
        PriorityQueue<Node, nuint> queue = new(_orderedMigrations.Length);
        Apply(node =>
        {
            node.IsVisited = false;
            node.Previous = null;
            if (node.Migration.DownVersion == _startVersion)
            {
                node.Distance = 0;
                queue.Enqueue(node, 0);
            }
            else
            {
                node.Distance = nuint.MaxValue;
            }
        });

        while (queue.TryDequeue(out var root, out var distance))
        {
            root.IsVisited = true;
            var nextDistance = distance + 1;
            foreach (var node in NodesByDown(root.Migration.UpVersion))
            {
                if (node.Distance <= nextDistance)
                {
                    continue;
                }
                node.Distance = nextDistance;
                node.Previous = root;
                if (!node.IsVisited)
                {
                    queue.Enqueue(node, node.Distance);
                }
            }
        }
    }

    private void EnsureTracePlausible()
    {
        var errors = ValidateTrace().ToArray();
        if (errors.Length == 0)
        {
            return;
        }
        if (errors.Length == 1)
        {
            throw errors[0];
        }
        throw new AggregateException($"Invalid migration set: no path from {_startVersion} to {_endVersion} exists", errors);

        IEnumerable<Exception> ValidateTrace()
        {
            // validate that the start and end nodes are connected
            if (!NodesByUp(_endVersion).Any(node => node.Previous is not null))
            {
                yield return new InvalidOperationException($"Invalid migration set: No path to the target version ({_endVersion}) exists with the available mirgrations.");
            }
            // validate that the start and end nodes are connected
            if (!NodesByDown(_startVersion).Any(node => node.IsVisited))
            {
                yield return new InvalidOperationException($"Invalid migration set: No path from the current version ({_startVersion}) exists with the available mirgrations.");
            }
        }
    }

    public IEnumerable<Migration> GetMigrationTrace()
    {
        TraceDistance();
        EnsureTracePlausible();
        Stack<Migration> trace = [];
        var downVersion = _endVersion;
        while (downVersion > _startVersion)
        {
            var closestNode = NodesByUp(downVersion)
                .Where(node => node.IsVisited)
                .MinBy(node => node.Distance);
            if (closestNode is null)
            {
                break;
            }
            trace.Push(closestNode.Migration);
            downVersion = closestNode.Migration.DownVersion;
        }
        if (downVersion != _startVersion)
        {
            throw new InvalidOperationException($"No path between the current version ({_startVersion}) and the intermediary version ({downVersion}) exists.");
        }
        return trace;
    }

    private void Apply(Action<Node> apply)
    {
        foreach (var nodes in _migrationByDownVersion.Values)
        {
            foreach (var node in nodes)
            {
                apply(node);
            }
        }
    }


    private sealed class Node(Migration migration)
    {
        public Migration Migration => migration;
        public nuint Distance { get; set; } = nuint.MaxValue;
        public bool IsVisited { get; set; }
        public Node? Previous { get; set; }
    }
}
