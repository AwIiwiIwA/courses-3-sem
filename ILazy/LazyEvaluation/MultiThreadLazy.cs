public class MultiThreadLazy<T> : ILazy<T>
{
    private Func<T> supplier;
    private T value;
    private bool isEvaluated;
    private readonly object lockObject = new object();

    public MultiThreadLazy(Func<T> supplier)
    {
        this.supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
    }

    public T Get()
    {
        if (!isEvaluated)
        {
            lock (lockObject)
            {
                if (!isEvaluated)
                {
                    value = supplier();
                    supplier = null; // Освобождаем supplier, так как больше не нужно
                    isEvaluated = true;
                }
            }
        }
        return value;
    }
}
