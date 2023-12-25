using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace GemLevelProtScraper;

internal interface IValueEnumerableSerializationAggregator<TItem, TValue>
    where TValue : struct, IEnumerable<TItem>
{
    void Add(TItem item);
    TValue FinalizeResult();
}

internal abstract class ValueEnumerableSerializerBase<TValue, TItem> : SerializerBase<TValue>, IBsonArraySerializer, IBsonSerializer
    where TValue : struct, IEnumerable<TItem>
{
    private readonly Lazy<IBsonSerializer<TItem>> _lazyItemSerializer;

    protected ValueEnumerableSerializerBase()
        : this(BsonSerializer.SerializerRegistry)
    {
    }

    protected ValueEnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
    {
        ArgumentNullException.ThrowIfNull(itemSerializer);

        _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(() => itemSerializer);
    }

    protected ValueEnumerableSerializerBase(IBsonSerializerRegistry serializerRegistry)
    {
        ArgumentNullException.ThrowIfNull(serializerRegistry);

        _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(serializerRegistry.GetSerializer<TItem>);
    }

    public IBsonSerializer<TItem> ItemSerializer => _lazyItemSerializer.Value;

    public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context?.Reader ?? throw new ArgumentNullException(nameof(context));

        var currentBsonType = reader.GetCurrentBsonType();
        return currentBsonType switch
        {
            BsonType.Null => DeserializeBsonNull(reader),
            BsonType.Array => DeserializeBsonArray(context, reader),
            _ => throw CreateCannotDeserializeFromBsonTypeException(currentBsonType)
        };

        static TValue DeserializeBsonNull(IBsonReader reader)
        {
            reader.ReadNull();
            return default;
        }

        TValue DeserializeBsonArray(BsonDeserializationContext context, IBsonReader reader)
        {
            reader.ReadStartArray();
            var agg = CreateAggregator();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var item = _lazyItemSerializer.Value.Deserialize(context);
                agg.Add(item);
            }
            reader.ReadEndArray();
            return agg.FinalizeResult();
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
    {
        var writer = context?.Writer ?? throw new ArgumentNullException(nameof(context));

        if (value.Equals(default))
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartArray();
            foreach (var item in EnumerateItemsInSerializationOrder(value))
            {
                _lazyItemSerializer.Value.Serialize(context, item);
            }

            writer.WriteEndArray();
        }
    }

    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        var itemSerializer = _lazyItemSerializer.Value;
        serializationInfo = new BsonSerializationInfo(null, itemSerializer, itemSerializer.ValueType);
        return true;
    }

    protected abstract IValueEnumerableSerializationAggregator<TItem, TValue> CreateAggregator();

    protected abstract IEnumerable<TItem> EnumerateItemsInSerializationOrder(TValue value);
}

internal sealed class ImmutableArraySerializationProvider : IBsonSerializationProvider
{
    private static readonly ConcurrentDictionary<Type, Type?> s_serializerTypeCache = new();

    [ThreadStatic]
    private static (Type Type, Type? Serializer)? t_lastSerializer;

    private static Type? GetSerializerType(Type type)
    {
        // take thread cache
        if (t_lastSerializer is { } lastSerializer && lastSerializer.Type == type)
        {
            return lastSerializer.Serializer;
        }
        t_lastSerializer = null;

        // read from global cache
        if (s_serializerTypeCache.TryGetValue(type, out var cachedSerializerType))
        {
            t_lastSerializer = (type, cachedSerializerType);
            return cachedSerializerType;
        }

        if (!type.IsGenericType)
        {
            s_serializerTypeCache.TryAdd(type, null);
            t_lastSerializer = (type, null);
            return null;
        }

        var serializerType = type.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? MakeSerializerTypeOrDefault(type.GetGenericArguments()[0], typeof(NullableImmutableArraySerializer<>))
            : MakeSerializerTypeOrDefault(type, typeof(ImmutableArraySerializer<>));

        s_serializerTypeCache.TryAdd(type, serializerType);
        t_lastSerializer = (type, serializerType);
        return serializerType;

        static Type? MakeSerializerTypeOrDefault(Type type, Type serializerTypeDefinition)
        {
            if (GetSerializerItemType(type) is { } itemType)
            {
                return serializerTypeDefinition.MakeGenericType(itemType);
            }
            return null;
        }

        static Type? GetSerializerItemType(Type maybeImmutableArrayType)
        {
            if (!maybeImmutableArrayType.IsGenericType)
            {
                return null;
            }

            if (maybeImmutableArrayType.GetGenericTypeDefinition() != typeof(ImmutableArray<>))
            {
                return null;
            }
            return maybeImmutableArrayType.GetGenericArguments()[0];
        }
    }


    public IBsonSerializer? GetSerializer(Type type)
    {
        if (GetSerializerType(type) is { } serializerType)
        {
            return (IBsonSerializer)Activator.CreateInstance(serializerType)!;
        }
        return null;
    }
}

internal sealed class ImmutableArraySerializer<TItem> : ValueEnumerableSerializerBase<ImmutableArray<TItem>, TItem>
{
    public ImmutableArraySerializer() : base() { }

    public ImmutableArraySerializer(IBsonSerializer<TItem> itemSerializer) : base(itemSerializer) { }

    public ImmutableArraySerializer(IBsonSerializerRegistry serializerRegistry) : base(serializerRegistry) { }

    protected override IValueEnumerableSerializationAggregator<TItem, ImmutableArray<TItem>> CreateAggregator()
    {
        return new Aggregator();
    }

    protected override IEnumerable<TItem> EnumerateItemsInSerializationOrder(ImmutableArray<TItem> value)
    {
        return value.IsDefaultOrEmpty ? Enumerable.Empty<TItem>() : value;
    }

    private sealed class Aggregator : IValueEnumerableSerializationAggregator<TItem, ImmutableArray<TItem>>
    {
        private readonly ImmutableArray<TItem>.Builder _builder = ImmutableArray.CreateBuilder<TItem>();

        public void Add(TItem item)
        {
            _builder.Add(item);
        }

        public ImmutableArray<TItem> FinalizeResult()
        {
            // swap to immutable, attempt no realloc
            var length = _builder.Count;
            // extract the immutable array without copying
            _builder.Count = _builder.Capacity;
            var extractedImmutable = _builder.MoveToImmutable();
            // attempt an array resize on the array of extractedImmutable
            ref var extractedArray = ref Unsafe.As<ImmutableArray<TItem>, TItem[]>(ref extractedImmutable);
            Array.Resize(ref extractedArray, length);
            return extractedImmutable;
        }
    }

}

internal sealed class NullableImmutableArraySerializer<TItem> : SerializerBase<ImmutableArray<TItem>?>
{
    private readonly ImmutableArraySerializer<TItem> _innerSerializer;


    public NullableImmutableArraySerializer()
    {
        _innerSerializer = new();
    }

    public NullableImmutableArraySerializer(IBsonSerializer<TItem> itemSerializer)
    {
        _innerSerializer = new(itemSerializer);
    }

    public NullableImmutableArraySerializer(IBsonSerializerRegistry serializerRegistry)
    {
        _innerSerializer = new(serializerRegistry);
    }

    public override ImmutableArray<TItem>? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var deserialized = _innerSerializer.Deserialize(context, args);
        return deserialized.IsDefault ? null : deserialized;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ImmutableArray<TItem>? value)
    {
        _innerSerializer.Serialize(context, args, value ?? default);
    }
}

internal sealed class NullableStructSerializationProvider : IBsonSerializationProvider
{
    private static readonly Lazy<MethodInfo> s_getNullableSerializerMethod = new(()
        => typeof(NullableStructSerializationProvider)
            .GetMethod(nameof(GetNullableSerializer), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException()
    );

    private static readonly ConcurrentDictionary<Type, Func<IBsonSerializer>> s_serializerFactoryCache = new();
    [ThreadStatic]
    private static (Type Type, Func<IBsonSerializer> SerializerFactory)? t_lastSerializerFactory;

    public IBsonSerializer? GetSerializer(Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }
        if (type.GetGenericTypeDefinition() != typeof(Nullable<>))
        {
            return null;
        }
        var factory = GetOrCreateSerializerFactory(type.GetGenericArguments()[0]);
        return factory();
    }

    private static Func<IBsonSerializer> GetOrCreateSerializerFactory(Type type)
    {
        if (t_lastSerializerFactory is { } last && last.Type == type)
        {
            return last.SerializerFactory;
        }

        if (s_serializerFactoryCache.TryGetValue(type, out var serializerFactory))
        {
            t_lastSerializerFactory = (type, serializerFactory);
            return serializerFactory;
        }

        var method = s_getNullableSerializerMethod.Value.MakeGenericMethod(type);
        serializerFactory = (Func<IBsonSerializer>)Delegate.CreateDelegate(typeof(Func<IBsonSerializer>), method);

        t_lastSerializerFactory = (type, serializerFactory);
        s_serializerFactoryCache.TryAdd(type, serializerFactory);
        return serializerFactory;
    }
#pragma warning disable CA1859
    private static IBsonSerializer GetNullableSerializer<TValue>()
#pragma warning restore CA1859
        where TValue : struct
    {
        var serializer = BsonSerializer.LookupSerializer<TValue>() ?? throw new InvalidOperationException($"No serializer is regiersterd for the type {typeof(TValue)}.");
        return new NullableStructSerializer<TValue>(serializer);
    }
}

internal sealed class NullableStructSerializer<TValue>(IBsonSerializer<TValue> innerSerializer) : IBsonSerializer<TValue?>
    where TValue : struct
{
    public Type ValueType => typeof(TValue?);

    public TValue? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return context.Reader.CurrentBsonType == BsonType.Null ? null : innerSerializer.Deserialize(context, args);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue? value)
    {
        if (value is { } v)
        {
            innerSerializer.Serialize(context, args, v);
        }
        else
        {
            context.Writer.WriteNull();
        }
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is TValue v)
        {
            Serialize(context, args, v);
            return;
        }
        if (value is null)
        {
            Serialize(context, args, default);
            return;
        }

        throw new InvalidOperationException($"The value is not an incompatible type {value.GetType()}. Expected type {ValueType}.");
    }

    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}
