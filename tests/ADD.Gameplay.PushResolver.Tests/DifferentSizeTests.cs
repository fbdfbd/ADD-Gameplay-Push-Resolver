using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class DifferentSizeTests
{
    [Test]
    public void Solve_SeparatesItemsUsingBothHalfSizesAndPadding()
    {
        const float padding = 0.12f;
        var fixture = new SolverTestFixture(padding: padding);
        var items = new[]
        {
            SolverTestFixture.Item(1, 0f, 0f, 0.7f, 0.4f),
            SolverTestFixture.Item(2, 0.8f, 0f, 0.3f, 0.8f)
        };

        var result = fixture.Solve(items, 1, Vector2.UnitX);

        fixture.AssertValidResult(items, 1, result);
        var requiredDistance = items[0].HalfSize.X + items[1].HalfSize.X + padding;
        Assert.That(result.Positions[2].X - result.Positions[1].X,
            Is.GreaterThanOrEqualTo(requiredDistance - 0.0001f));
    }
}
