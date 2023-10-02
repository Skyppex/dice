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
            var expression = new DiceExpression(1, 20);
            
            // Act
            var result = expression.Evaluate();
            
            // Assert
            result.Result.Should().BeInRange(1f, 20f);
        }
    }
}