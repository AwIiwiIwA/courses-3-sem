namespace MyThreadPool
{
    /// <summary>
    /// Represents a task that is executed by the thread pool.
    /// </summary>
    /// <typeparam name="TResult">The result type of the task.</typeparam>
    public interface IMyTask<TResult>
    {
        /// <summary>
        /// Gets a value indicating whether the task has completed execution.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the result of the task execution. If the task is not yet completed,
        /// this property will block the calling thread until the result is available.
        /// </summary>
        TResult? Result { get; }

        /// <summary>
        /// Adds a continuation task that will be executed once this task has completed.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the result of the continuation task.</typeparam>
        /// <param name="continueFunction">The function to apply to the result of this task when it completes.</param>
        /// <returns>A new task representing the continuation of this task.</returns>
        IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult?, TNewResult> continueFunction);
    }
}
