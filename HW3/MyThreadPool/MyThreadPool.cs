namespace MyThreadPool;

using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// Represents a thread pool for executing tasks concurrently.
/// </summary>
public class MyThreadPool
{
    private readonly Thread[] _threads;
    private readonly ConcurrentQueue<Action> _taskQueue;
    private readonly CancellationTokenSource _cts;
    private readonly object _lockObject = new();
    private bool _isShutdown = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPool"/> class.
    /// </summary>
    /// <param name="threadNumber">The number of threads in the pool.</param>
    public MyThreadPool(int threadNumber)
    {
        if (threadNumber < 1)
        {
            throw new ArgumentException("Thread pool must have at least 1 thread.");
        }

        _threads = new Thread[threadNumber];
        _taskQueue = new ConcurrentQueue<Action>();
        _cts = new CancellationTokenSource();

        for (int i = 0; i < threadNumber; i++)
        {
            _threads[i] = new Thread(Work);
            _threads[i].IsBackground = true;
            _threads[i].Start();
        }
    }

    /// <summary>
    /// Submits a new task to the thread pool.
    /// </summary>
    /// <typeparam name="TResult">The type of task result.</typeparam>
    /// <param name="function">The function representing the task.</param>
    public IMyTask<TResult> Submit<TResult>(Func<TResult> function)
    {
        if (_cts.Token.IsCancellationRequested || _isShutdown)
        {
            throw new InvalidOperationException("Thread pool was shut down.");
        }

        var myTask = new MyTask<TResult>(function, this);
        _taskQueue.Enqueue(myTask.Execute);
        lock (_lockObject)
        {
            Monitor.Pulse(_lockObject);
        }
        return myTask;
    }

    /// <summary>
    /// Shuts down the thread pool and waits for all threads to complete execution.
    /// </summary>
    public void Shutdown()
    {
        lock (_lockObject)
        {
            _isShutdown = true;
            _cts.Cancel();
            Monitor.PulseAll(_lockObject);
        }

        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    private void Work()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            Action? task = null;

            lock (_lockObject)
            {
                while (!_taskQueue.TryDequeue(out task) && !_cts.Token.IsCancellationRequested)
                {
                    Monitor.Wait(_lockObject);
                }
            }

            task?.Invoke();
        }
    }

    internal void SubmitContinuation(Action action)
    {
        if (_isShutdown)
        {
            return;
        }

        _taskQueue.Enqueue(action);
        lock (_lockObject)
        {
            Monitor.Pulse(_lockObject);
        }
    }

    private class MyTask<TResult> : IMyTask<TResult>
    {
        private readonly Func<TResult> _function;
        private readonly MyThreadPool _threadPool;
        private TResult? _result;
        private bool _isCompleted;
        private Exception? _exception;
        private readonly ConcurrentQueue<Action> _continuations = new();
        private readonly object _syncObject = new();

        public MyTask(Func<TResult> function, MyThreadPool threadPool)
        {
            _function = function;
            _threadPool = threadPool;
        }

        public bool IsCompleted
        {
            get
            {
                lock (_syncObject)
                {
                    return _isCompleted;
                }
            }
        }

        public TResult? Result
        {
            get
            {
                lock (_syncObject)
                {
                    while (!_isCompleted)
                    {
                        Monitor.Wait(_syncObject);
                    }

                    if (_exception != null)
                    {
                        throw new AggregateException(_exception);
                    }

                    return _result;
                }
            }
        }

        internal void Execute()
        {
            try
            {
                _result = _function();
            }
            catch (Exception e)
            {
                _exception = e;
            }
            finally
            {
                lock (_syncObject)
                {
                    _isCompleted = true;
                    Monitor.PulseAll(_syncObject);
                    RunContinuations();
                }
            }
        }

        private void RunContinuations()
        {
            while (_continuations.TryDequeue(out var continuation))
            {
                _threadPool.SubmitContinuation(continuation);
            }
        }

        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult?, TNewResult> continuationFunction)
        {
            lock (_syncObject)
            {
                if (_threadPool._isShutdown)
                {
                    throw new InvalidOperationException("Thread pool was shut down.");
                }

                var continuationTask = new MyTask<TNewResult>(() => continuationFunction(Result), _threadPool);

                if (_isCompleted)
                {
                    _threadPool.SubmitContinuation(continuationTask.Execute);
                }
                else
                {
                    _continuations.Enqueue(continuationTask.Execute);
                }

                return continuationTask;
            }
        }

    }
}
