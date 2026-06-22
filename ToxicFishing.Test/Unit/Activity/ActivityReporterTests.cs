using ToxicFishing.Activity.Models;
using ToxicFishing.Activity.Services;

namespace ToxicFishing.Test.Unit.Activity;

public sealed class ActivityReporterTests
{
    [Test]
    public void Info_RaisesReportedWithInfoLevel()
    {
        var reporter = new ActivityReporter();
        var entries = new List<ActivityEntry>();
        reporter.Reported += entries.Add;

        reporter.Info("casting");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Level, Is.EqualTo(ActivityLevel.Info));
            Assert.That(entries[0].Message, Is.EqualTo("casting"));
        }
    }

    [Test]
    public void WarningAndError_CarryTheMatchingLevel()
    {
        var reporter = new ActivityReporter();
        var entries = new List<ActivityEntry>();
        reporter.Reported += entries.Add;

        reporter.Warning("lost bobber");
        reporter.Error("scan failed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries[0].Level, Is.EqualTo(ActivityLevel.Warning));
            Assert.That(entries[1].Level, Is.EqualTo(ActivityLevel.Error));
        }
    }

    [Test]
    public void Emit_ConsecutiveDuplicateMessage_RaisedOnlyOnce()
    {
        var reporter = new ActivityReporter();
        var entries = new List<ActivityEntry>();
        reporter.Reported += entries.Add;

        reporter.Info("casting");
        reporter.Info("casting");

        Assert.That(entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void Emit_DifferentMessages_RaisedForEach()
    {
        var reporter = new ActivityReporter();
        var entries = new List<ActivityEntry>();
        reporter.Reported += entries.Add;

        reporter.Info("casting");
        reporter.Info("bite");

        Assert.That(entries, Has.Count.EqualTo(2));
    }
}
