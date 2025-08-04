using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorStatic.Utilities;

internal static class TaskUtils
{
    public static async Task RunBatchAsync(IReadOnlyCollection<Task> tasks, int batchSize)
    {
        if (tasks.Count <= 0)
        {
            return;
        }

        for (var i = 0; i < tasks.Count; i += batchSize)
        {
            await Task.WhenAll(tasks.Skip(i).Take(batchSize)).ConfigureAwait(false);
        }
    }
}