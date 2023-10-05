﻿namespace Dice;

public record struct DiceResult(float Value, string Expression);
public record struct DiceResultValue(float Value);
public record struct DiceResultInt(int Value, string Expression);
