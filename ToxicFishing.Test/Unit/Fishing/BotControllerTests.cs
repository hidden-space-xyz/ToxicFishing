using NSubstitute;

using ToxicFishing.Activity.Abstractions;
using ToxicFishing.Fishing.Abstractions;
using ToxicFishing.Fishing.Services;
using ToxicFishing.Input.Abstractions;
using ToxicFishing.Vision.Abstractions;

namespace ToxicFishing.Test.Unit.Fishing;

public sealed class BotControllerTests
{
    private static BotController CreateController(
        IFishingSession? session = null,
        IInput? input = null,
        IVision? vision = null,
        IActivityReporter? activity = null)
    {
        return new BotController(
            session ?? Substitute.For<IFishingSession>(),
            input ?? Substitute.For<IInput>(),
            vision ?? Substitute.For<IVision>(),
            activity ?? Substitute.For<IActivityReporter>());
    }

    [Test]
    public void IsRunning_BeforeStart_IsFalse()
    {
        Assert.That(CreateController().IsRunning, Is.False);
    }

    [Test]
    public void Stop_WhenNotRunning_DoesNotThrow()
    {
        var controller = CreateController();

        Assert.DoesNotThrow(controller.Stop);
    }

    [Test]
    public void SetBobberTemplate_DelegatesToVision()
    {
        var vision = Substitute.For<IVision>();
        var controller = CreateController(vision: vision);

        controller.SetBobberTemplate("custom.png");

        vision.Received(1).LoadTemplate("custom.png");
    }

    [Test]
    public void Start_RaisesRunningChangedTrueAndMarksRunning()
    {
        var controller = CreateController();
        using var started = new ManualResetEventSlim(false);
        controller.RunningChanged += (_, running) =>
        {
            if (running)
            {
                started.Set();
            }
        };

        controller.Start();

        try
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(controller.IsRunning, Is.True);
            }
        }
        finally
        {
            controller.Stop();
        }
    }

    [Test]
    public void Start_WhenAlreadyRunning_DoesNotStartASecondSession()
    {
        var session = Substitute.For<IFishingSession>();
        session
            .When(s => s.Run(Arg.Any<CancellationToken>()))
            .Do(call => ((CancellationToken)call[0]).WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
        var controller = CreateController(session: session);

        controller.Start();
        controller.Start();
        controller.Stop();

        // Even with two Start calls, only one session loop may have been launched.
        Assert.That(session.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IFishingSession.Run)),
            Is.LessThanOrEqualTo(1));
    }
}
