namespace GraphQL.Subscription;

// This file only serves to eliminate the dependency on System.Reactive

internal static class ObservableExtensions
{
    /// <summary>
    /// <para>
    /// Applies an asynchronous transformation on data events from an <see cref="IObservable{T}"/>.
    /// Maintains the order of the events produced by the <see cref="IObservable{T}"/>
    /// whether they are data, error or completion notifications.
    /// </para>
    /// <para>
    /// Ensures that after a subscription has been disposed,
    /// no more events will be raised (data, error or completion), and signals
    /// pending asynchronous transformations that a cancellation has been requested.
    /// </para>
    /// <para>
    /// Exceptions passed by the source through <see cref="IObserver{T}.OnError(Exception)"/> or
    /// generated by <paramref name="transformNext"/> are handled by <paramref name="transformError"/>.
    /// </para>
    /// </summary>
    public static IObservable<TOut> SelectCatchAsync<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, ValueTask<TOut>> transformNext, Func<Exception, CancellationToken, ValueTask<Exception>> transformError)
        => new SelectCatchAsyncObservable<TIn, TOut>(source, transformNext, transformError);

    private class SelectCatchAsyncObservable<TIn, TOut> : IObservable<TOut>
    {
        private readonly IObservable<TIn> _source;
        private readonly Func<TIn, CancellationToken, ValueTask<TOut>> _transformNext;
        private readonly Func<Exception, CancellationToken, ValueTask<Exception>> _transformError;

        public SelectCatchAsyncObservable(IObservable<TIn> source, Func<TIn, CancellationToken, ValueTask<TOut>> transformNext, Func<Exception, CancellationToken, ValueTask<Exception>> transformError)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _transformNext = transformNext ?? throw new ArgumentNullException(nameof(transformNext));
            _transformError = transformError ?? throw new ArgumentNullException(nameof(transformError));
        }

        /// <summary>
        /// Subscribes to the underlying <see cref="IObservable{T}"/> with the
        /// transformation specified by this instance.
        /// <br/><br/>
        /// Disconnection requests via the returned <see cref="IDisposable"/> interface
        /// are passed to the underlying <see cref="IObservable{T}"/> and also used
        /// to signal pending asynchronous tasks that cancellation has been requested
        /// and also used to prevent further event notifications.
        /// </summary>
        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            IDisposable? disposable = null;
            var newObserver = new Observer(observer, _transformNext, _transformError, () => disposable?.Dispose());
            disposable = _source.Subscribe(newObserver);
            return newObserver;
        }

        private class Observer : IObserver<TIn>, IDisposable
        {
            private CancellationTokenSource? _cancellationTokenSource = new();
            private readonly CancellationToken _token;
            //create a queue so that events will be sent in order
            private readonly Queue<QueueEvent> _queue = new();
            private readonly IObserver<TOut> _observer;
            private readonly Func<TIn, CancellationToken, ValueTask<TOut>> _transformNext;
            private readonly Func<Exception, CancellationToken, ValueTask<Exception>> _transformError;
            private Action? _disposeAction;

            public Observer(IObserver<TOut> observer, Func<TIn, CancellationToken, ValueTask<TOut>> transformNext, Func<Exception, CancellationToken, ValueTask<Exception>> transformError, Action disposeAction)
            {
                _token = _cancellationTokenSource.Token;
                _observer = observer;
                _disposeAction = disposeAction;
                // ensure that the transform cannot directly throw an exception without it being wrapped in a Task<Exception>
                _transformError = async (exception, token) => await transformError(exception, token).ConfigureAwait(false);
                // ensure that the transform cannot directly throw an exception without it being wrapped in a Task<TOut>
                _transformNext = async (data, token) => await transformNext(data, token).ConfigureAwait(false);
            }

            public void OnNext(TIn value) => Queue(QueueType.Data, _transformNext(value, _token), default);

            public void OnError(Exception error) => Queue(QueueType.Error, default, _transformError(error, _token));

            public void OnCompleted() => Queue(QueueType.Completion, default, default);

            /// <summary>
            /// Queues the specified event and if necessary starts watching for an event to complete.
            /// </summary>
            private void Queue(QueueType queueType, ValueTask<TOut> task, ValueTask<Exception> error)
            {
                var queueData = new QueueEvent { Type = queueType, Data = task, Error = error };
                bool attach = false;
                lock (_queue)
                {
                    _queue.Enqueue(queueData);
                    attach = _queue.Count == 1;
                }

                // start watching for an event to complete, if this is the first in the queue
                if (attach)
                {
                    // start sending data events (will await on the task queued if needed)
                    _ = ProcessAllEventsInQueueAsync();
                }
            }

            /// <summary>
            /// Processes data from the queue in order (or raises errors or completed notifications);
            /// executes until the queue is empty.
            /// </summary>
            private async Task ProcessAllEventsInQueueAsync()
            {
                // grab the event at the start of the queue, but don't remove it from the queue
                QueueEvent queueEvent;
                bool moreEvents;
                lock (_queue)
                {
                    // should always successfully peek from the queue here
                    moreEvents = _queue.TryPeek(out queueEvent);
                }
                while (moreEvents)
                {
                    // process the event
                    if (queueEvent.Type == QueueType.Data)
                    {
                        await ProcessDataAsync(queueEvent.Data).ConfigureAwait(false);
                    }
                    else if (queueEvent.Type == QueueType.Error)
                    {
                        await ProcessErrorAsync(queueEvent.Error).ConfigureAwait(false);
                    }
                    else if (queueEvent.Type == QueueType.Completion)
                    {
                        ProcessCompletion();
                    }
                    // once the event has been passed along, dequeue it
                    lock (_queue)
                    {
                        _ = _queue.Dequeue();
                        moreEvents = _queue.TryPeek(out queueEvent);
                    }
                    // if the queue is empty, immedately quit the loop, as any new
                    // events queued will start ReturnDataAsync
                }
            }

            /// <summary>
            /// Wait for the transform to complete and push the data (or error) back to the observer.
            /// If the observer has been disposed, then data and errors are ignored.
            /// </summary>
            private async Task ProcessDataAsync(ValueTask<TOut> dataTask)
            {
                if (_token.IsCancellationRequested)
                    return;
                TOut dataOut;
                try
                {
                    dataOut = await dataTask.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    if (!_token.IsCancellationRequested)
                        _observer.OnError(error);
                    return;
                }
                if (!_token.IsCancellationRequested)
                    _observer.OnNext(dataOut);
            }

            /// <summary>
            /// Wait for the transform to complete and push the error back to the observer.
            /// If the observer has been disposed, then errors are ignored.
            /// </summary>
            private async Task ProcessErrorAsync(ValueTask<Exception> errorTask)
            {
                if (_token.IsCancellationRequested)
                    return;
                Exception errorOut;
                try
                {
                    errorOut = await errorTask.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    if (!_token.IsCancellationRequested)
                        _observer.OnError(error);
                    return;
                }
                if (!_token.IsCancellationRequested)
                    _observer.OnError(errorOut);
            }

            /// <summary>
            /// Push a completion notice back to the observer.
            /// If the observer has been disposed, ignore.
            /// </summary>
            private void ProcessCompletion()
            {
                if (!_token.IsCancellationRequested)
                    _observer.OnCompleted();
            }

            /// <summary>
            /// Disposes of the underlying observable sequence
            /// </summary>
            public void Dispose()
            {
                var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
                if (cts == null)
                    return;
                // cancel pending operations and prevent pending operations
                // from returning data after the observable has been detached
                cts.Cancel();
                // dispose the cancellation token source
                cts.Dispose();
                // detach the observable sequence
                _disposeAction?.Invoke();
                // release references to the degree possible
                _disposeAction = null;
            }

            /// <summary>
            /// Represents a single event in the queue.
            /// </summary>
            /// <param name="Type">The type of the event.</param>
            /// <param name="Data">For data events, the <see cref="Task{TResult}"/> containing a <typeparamref name="TOut"/>.</param>
            /// <param name="Error">For error events, the <see cref="Task{TResult}"/> containing an <see cref="Exception"/>.</param>
            private readonly record struct QueueEvent(QueueType Type, ValueTask<TOut> Data, ValueTask<Exception> Error);

            /// <summary>
            /// The type of the event.
            /// </summary>
            private enum QueueType
            {
                Data = 0,
                Error = 1,
                Completion = 2,
            }
        }
    }

#if NETSTANDARD2_0
    internal static bool TryPeek<T>(this Queue<T> queue, out T? value)
    {
        if (queue.Count > 0)
        {
            value = queue.Peek();
            return true;
        }
        value = default;
        return false;
    }
#endif
}
