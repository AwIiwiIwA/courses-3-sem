using NUnit.Framework;
using System.Threading.Tasks;

[TestFixture]
public class MultiThreadLazyTests
{
    [Test]
    public void ShouldReturnSameValueOnMultipleCalls()
    {
        var lazy = new MultiThreadLazy<int>(() => 42);
        Assert.AreEqual(42, lazy.Get());
        Assert.AreEqual(42, lazy.Get());
    }

    [Test]
    public void ShouldEvaluateOnlyOnceInMultithreadedEnvironment()
    {
        int counter = 0;
        var lazy = new MultiThreadLazy<int>(() => { counter++; return 42; });

        Parallel.Invoke(
            () => Assert.AreEqual(42, lazy.Get()),
            () => Assert.AreEqual(42, lazy.Get())
        );

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void ShouldSupportNullValueInMultithreadedEnvironment()
    {
        var lazy = new MultiThreadLazy<string>(() => null);

        Parallel.Invoke(
            () => Assert.IsNull(lazy.Get()),
            () => Assert.IsNull(lazy.Get())
        );
    }
}
