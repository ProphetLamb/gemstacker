using System.Collections.Concurrent;
using System.Collections.Immutable;
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
        if (itemSerializer is null)
        {
            throw new ArgumentNullException(nameof(itemSerializer));
        }

        _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(() => itemSerializer);
    }

    protected ValueEnumerableSerializerBase(IBsonSerializerRegistry serializerRegistry)
    {
        if (serializerRegistry is null)
        {
            throw new ArgumentNullException(nameof(serializerRegistry));
        }

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
