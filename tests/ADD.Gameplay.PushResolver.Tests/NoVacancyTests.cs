using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class NoVacancyTests
{
    [Test]
    public void Solve_ReturnsFailureWithoutPartialPositionsWhenBoardHasNoVacancy()
    {
        var fixture = new SolverTestFixture(boardHalfExtent: 1f);
        var items = new[]
        {
            SolverTestFixture.Item(1, 0f, 0f),
            SolverTestFixture.Item(2, 0.4f, 0f)
        };

        var result = fixture.Solve(
            items,
            1,
            Vector2.UnitX,
            ResolveDirectionPolicy.TryAlternates);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Positions, Is.Empty);
        Assert.That(items[0].Position, Is.EqualTo(Vector2.Zero));
        Assert.That(items[1].Position, Is.EqualTo(new Vector2(0.4f, 0f)));
    }
}
