namespace MyThreadPoolTests;

public class Tests
{
    private readonly int _threadPoolNumber = Environment.ProcessorCount;
    private MyThreadPool.MyThreadPool _threadPool = null!;

    [SetUp]
    public void Initialization()
    {
        _threadPool = new MyThreadPool.MyThreadPool(_threadPoolNumber);
    }

    [TearDown]
    public void Cleanup()
    {
        _threadPool.Shutdown();
    }

    [Test]
    public void Submit_FunctionReturningValue_ExpectedValueReturned()
    {
        var result = _threadPool.Submit(() => 2 + 2);
        Assert.That(result.Result, Is.EqualTo(4));
    }

    [Test]
    public void ContinueWith_ParentAndContinuationTask_ParentTaskCompletesFirst()
    {
        var flag = false;

        var parentTask = _threadPool.Submit(() =>
        {
            Thread.Sleep(100);
            return 0;
        });

        parentTask.ContinueWith(result =>
        {
            if (parentTask.IsCompleted)
            {
                Volatile.Write(ref flag, true);
            }
            return 1;
        });

        Thread.Sleep(200);

        Assert.IsTrue(flag);
    }

    [Test]
    public void Shutdown_TasksSubmitted_AllTasksComplete()
    {
        var flag = false;

        _threadPool.Submit(() =>
        {
            Thread.Sleep(500);
            Volatile.Write(ref flag, true);
            return 0;
        });

        _threadPool.Shutdown();
        Assert.IsTrue(flag);
    }

    [Test]
    public void ContinueWith_TasksAndMainThread_MainThreadNotBlocked()
    {
        var mainThreadContinueSignal = new AutoResetEvent(false);

        var parentTask = _threadPool.Submit(() =>
        {
            Thread.Sleep(10000);
            return 0;
        });

        var continuationTask = parentTask.ContinueWith(result =>
        {
            mainThreadContinueSignal.Set();
            return 1;
        });

        bool flag = !mainThreadContinueSignal.WaitOne(1000);

        Assert.IsTrue(flag);
    }

    [Test]
    public void Submit_AfterShutdown_ThrowsInvalidOperationException()
    {
        _threadPool.Shutdown();
        Assert.Throws<InvalidOperationException>(() => _threadPool.Submit(() => 0));
    }

    [Test]
    public void ContinueWith_AfterShutdown_ThrowsInvalidOperationException()
    {
        var task = _threadPool.Submit(() => 0);
        _threadPool.Shutdown();
        Assert.Throws<InvalidOperationException>(() => task.ContinueWith(result => result + 1));
    }
}