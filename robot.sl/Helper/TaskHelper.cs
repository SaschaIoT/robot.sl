using System;
using System.Threading;
using System.Threading.Tasks;

namespace robot.sl
{
    public static class TaskHelper
    {
        public static async Task WithTimeoutAfterStart(Func<CancellationToken, Task> operation, TimeSpan timeout)
        {
            var source = new CancellationTokenSource();
            var task = operation(source.Token);
            //After task starts timeout begin to tick
            source.CancelAfter(timeout);
            await task;
        }

        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            await task;
        }
    }
}
