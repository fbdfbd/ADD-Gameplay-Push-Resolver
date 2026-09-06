using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class CascadePushTests
{
    [Test]
    public void Solve_MovesConnectedItemsAsCascade()
    {
        var fixture = new SolverTestFixture();
        var items = new[]
        {
            SolverTestFixture.Item(1, 0f, 0f),
            SolverTestFixture.Item(2, 0.7f, 0f),
            SolverTestFixture.Item(3, 1.45f, 0f)
        };

        var result = fixture.Solve(items, 1, Vector2.UnitX);

        fixture.AssertValidResult(items, 1, result);
        Assert.That(result.Positions[2].X, Is.GreaterThan(items[1].Position.X));
        Assert.That(result.Positions[3].X, Is.GreaterThan(items[2].Position.X));
    }
}
