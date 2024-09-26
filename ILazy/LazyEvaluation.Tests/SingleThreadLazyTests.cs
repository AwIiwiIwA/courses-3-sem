using NUnit.Framework;

[TestFixture]
public class SingleThreadLazyTests
{
    [Test]
    public void ShouldReturnSameValueOnMultipleCalls()
    {
        var lazy = new SingleThreadLazy<int>(() => 42);
        Assert.AreEqual(42, lazy.Get());
        Assert.AreEqual(42, lazy.Get());
    }

    [Test]
    public void ShouldSupportNullValue()
    {
        var lazy = new SingleThreadLazy<string>(() => null);
        Assert.IsNull(lazy.Get());
    }

    [Test]
    public void ShouldEvaluateOnlyOnce()
    {
        int counter = 0;
        var lazy = new SingleThreadLazy<int>(() => { counter++; return 42; });

        Assert.AreEqual(42, lazy.Get());
        Assert.AreEqual(42, lazy.Get());
        Assert.AreEqual(1, counter);
    }
}
