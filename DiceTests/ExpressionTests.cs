using Dice;
using FluentAssertions;

namespace DiceTests;

public class ExpressionTests
{
    [Test]
    public void Should_evaluate_correctly()
    {
        for (int i = 0; i < 1000; i++)
        {
            // Arrange
            var expression = new DiceExpression(1, new DiceRange(1, 20, IDice.DefaultFormat), DiceExpression.Modes.Default());
            
            // Act
            var result = expression.Evaluate(new RandomRollHandler());
            
            // Assert
            result.Value.Should().BeInRange(1f, 20f);
        }
    }
}