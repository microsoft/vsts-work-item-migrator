using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

public static class TaskExtensions
{
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int degreeOfParallelism, Func<T, int, Task> func)
    {
        var partitionId = 0;
        var partitioner = Partitioner.Create(source);
        var partitions = partitioner.GetPartitions(degreeOfParallelism);

        var tasks = partitions.Select((partition) => Task.Run(async () =>
        {
            int current;
            while (partition.MoveNext())
            {
                current = Interlocked.Increment(ref partitionId);
                await func(partition.Current, current);
            }
        }));

        return Task.WhenAll(tasks);
    }
}