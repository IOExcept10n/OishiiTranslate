using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MyTranslateExtension.Pages
{
    /// <summary>
    /// Represents a class that delays event actions to accumulate multiple events happened simultaneously into one handler.
    /// </summary>
    public class DelayedNotifier : IDisposable
    {
        private readonly System.Timers.Timer eventTimer;
        private readonly Action? onNotify;
        private readonly Func<CancellationToken, Task>? onNotifyAsync;

        private CancellationTokenSource cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedNotifier"/> class.
        /// </summary>
        /// <param name="onNotify">Action to raise on notification completes.</param>
        /// <param name="delayMilliseconds">Delay to accumulate within.</param>
        public DelayedNotifier(Action onNotify, double delayMilliseconds)
        {
            this.onNotify = onNotify;
            cts = new();
            eventTimer = new(delayMilliseconds)
            {
                AutoReset = false,
            };
            eventTimer.Elapsed += OnTimerElapsed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedNotifier"/> class with asynchronous handler.
        /// </summary>
        /// <param name="onNotifyAsync">Async function to raise on notification.</param>
        /// <param name="delayMilliseconds">Delay to accumulate within.</param>
        public DelayedNotifier(Func<CancellationToken, Task> onNotifyAsync, double delayMilliseconds)
        {
            this.onNotifyAsync = onNotifyAsync;
            cts = new();
            eventTimer = new(delayMilliseconds)
            {
                AutoReset = false,
            };
            eventTimer.Elapsed += OnTimerElapsed;
        }

        [MemberNotNullWhen(true, nameof(onNotifyAsync))]
        [MemberNotNullWhen(false, nameof(onNotify))]
        private bool IsAsync => onNotifyAsync != null;

        /// <summary>
        /// Cancels raising next event.
        /// </summary>
        public void Cancel()
        {
            eventTimer.Stop();
            cts.Cancel();
            cts.Dispose();
            cts = new();
        }

        public void Dispose()
        {
            eventTimer.Dispose();
            cts.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resets notification delay.
        /// </summary>
        public void NotifyUpdate()
        {
            eventTimer.Stop();
            eventTimer.Start();
        }

        /// <summary>
        /// Raises event immediately.
        /// </summary>
        /// <returns>Asynchronous task to wait if handler was asynchronous.</returns>
        public async Task RaiseAsync()
        {
            Cancel();
            if (IsAsync)
            {
                await onNotifyAsync(cts.Token);
            }
            else
            {
                onNotify();
            }
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await RaiseAsync();
        }
    }
}
