using System.Threading.Channels;
using BenchmarkDotNet.Attributes;

namespace PrimusSaaS.Logging.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class AsyncChannelBenchmarks
{
    private Channel<int> _channel = null!;

    [GlobalSetup]
    public void Setup()
    {
        _channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    [Benchmark]
    public void TryWriteDropOldest()
    {
        _channel.Writer.TryWrite(1);
    }
}
