// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Specifies the comparison operation to perform between two resolved values.
/// </summary>
public enum ComparisonOperation : byte
{
	/// <summary>
	/// Tests whether the left operand is equal to the right operand.
	/// </summary>
	Equal = 0,

	/// <summary>
	/// Tests whether the left operand is not equal to the right operand.
	/// </summary>
	NotEqual = 1,

	/// <summary>
	/// Tests whether the left operand is less than the right operand.
	/// </summary>
	LessThan = 2,

	/// <summary>
	/// Tests whether the left operand is less than or equal to the right operand.
	/// </summary>
	LessThanOrEqual = 3,

	/// <summary>
	/// Tests whether the left operand is greater than the right operand.
	/// </summary>
	GreaterThan = 4,

	/// <summary>
	/// Tests whether the left operand is greater than or equal to the right operand.
	/// </summary>
	GreaterThanOrEqual = 5,
}
