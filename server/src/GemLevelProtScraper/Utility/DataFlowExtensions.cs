using System.Threading.Channels;
using ScrapeAAS;

namespace GemLevelProtScraper;

public static class DataFlowExtensions
{

    public static Task PublishAllAsync<T>(this IDataflowPublisher<T> publisher, IEnumerable<T> seq, CancellationToken cancellationToken = default)
    {
        return PublishAllAsync(publisher, seq, new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        });
    }

    public static async Task PublishAllAsync<T>(this IDataflowPublisher<T> publisher, IEnumerable<T> seq, ParallelOptions parallelOptions)
    {
        var ch = Channel.CreateBounded<T>(parallelOptions.MaxDegreeOfParallelism);
        var deflators = Enumerable.Range(0, parallelOptions.MaxDegreeOfParallelism).Select(_ => Deflate(ch.Reader));
        var inflator = Inflate(ch.Writer);

        await Task.WhenAll(deflators).ConfigureAwait(false);
        await inflator.ConfigureAwait(false);

        async Task Inflate(ChannelWriter<T> writer)
        {
            try
            {
                foreach (var item in seq)
                {
                    await writer.WriteAsync(item, parallelOptions.CancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _ = writer.TryComplete();
            }
        }

        async Task Deflate(ChannelReader<T> reader)
        {
            await foreach (var item in reader.ReadAllAsync(parallelOptions.CancellationToken))
            {
                await publisher.PublishAsync(item).ConfigureAwait(false);
            }
        }
    }
}
