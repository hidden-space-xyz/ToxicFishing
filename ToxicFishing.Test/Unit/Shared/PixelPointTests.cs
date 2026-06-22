using ToxicFishing.Shared.Primitives;

namespace ToxicFishing.Test.Unit.Shared;

public sealed class PixelPointTests
{
    [Test]
    public void Empty_IsOriginAndReportsEmpty()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(PixelPoint.Empty.X, Is.Zero);
            Assert.That(PixelPoint.Empty.Y, Is.Zero);
            Assert.That(PixelPoint.Empty.IsEmpty, Is.True);
        }
    }

    [Test]
    public void IsEmpty_NonOrigin_IsFalse()
    {
        Assert.That(new PixelPoint(0, 5).IsEmpty, Is.False);
    }

    [TestCase(0, 0, 3, 4, 5.0)]
    [TestCase(1, 1, 1, 1, 0.0)]
    [TestCase(0, 0, 0, 5, 5.0)]
    [TestCase(-3, -4, 0, 0, 5.0)]
    public void DistanceTo_ReturnsEuclideanDistance(int x1, int y1, int x2, int y2, double expected)
    {
        Assert.That(new PixelPoint(x1, y1).DistanceTo(new PixelPoint(x2, y2)), Is.EqualTo(expected));
    }

    [Test]
    public void Equality_ComparesByCoordinateValue()
    {
        var point = new PixelPoint(3, 4);
        var same = new PixelPoint(3, 4);
        var different = new PixelPoint(4, 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(point, Is.EqualTo(same));
            Assert.That(point, Is.Not.EqualTo(different));
        }
    }

    [Test]
    public void ToString_FormatsAsCoordinatePair()
    {
        Assert.That(new PixelPoint(3, 4).ToString(), Is.EqualTo("(3, 4)"));
    }
}
