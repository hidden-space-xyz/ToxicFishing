using ToxicFishing.Humanization.Services;
using ToxicFishing.Shared.Configuration;

namespace ToxicFishing.Test.Unit.Humanization;

public sealed class HumanizerTests
{
    [Test]
    public void NextDelayMs_ManySamples_AlwaysWithinBounds()
    {
        var humanizer = new Humanizer();
        humanizer.StartSession();

        for (var i = 0; i < 10_000; i++)
        {
            Assert.That(humanizer.NextDelayMs(100, 50, 200), Is.InRange(50, 200));
        }
    }

    [Test]
    public void NextDelayMs_MinEqualsMax_ReturnsThatValue()
    {
        var humanizer = new Humanizer();

        Assert.That(humanizer.NextDelayMs(80, 50, 50), Is.EqualTo(50));
    }

    [Test]
    public void NextReactionMs_ManySamples_WithinConfiguredBounds()
    {
        var humanizer = new Humanizer();
        humanizer.StartSession();

        for (var i = 0; i < 10_000; i++)
        {
            Assert.That(
                humanizer.NextReactionMs(),
                Is.InRange(AppOptions.Humanizer.ReactionMinMs, AppOptions.Humanizer.ReactionMaxMs));
        }
    }

    [Test]
    public void StartSession_CalledRepeatedly_KeepsDelaysWithinBounds()
    {
        var humanizer = new Humanizer();

        humanizer.StartSession();
        var first = humanizer.NextDelayMs(100, 50, 200);
        humanizer.StartSession();
        var second = humanizer.NextDelayMs(100, 50, 200);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.InRange(50, 200));
            Assert.That(second, Is.InRange(50, 200));
        }
    }
}
