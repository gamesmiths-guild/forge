// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Represents a generic interface for evaluating curves in a gameplay context.
/// This interface abstracts engine-specific curve implementations, providing
/// a consistent API for curve evaluation across different engines.
/// </summary>
public interface ICurve
{
	/// <summary>
	/// Evaluates the curve at a specified point, returning the interpolated result.
	/// </summary>
	/// <param name="value">The input value used to evaluate the curve, typically ranging from 0 to 1.</param>
	/// <returns>The output value of the curve at the specified input.</returns>
	float Evaluate(float value);
}
