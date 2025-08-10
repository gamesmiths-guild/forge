// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Helper class for doing editor validation checks.
/// </summary>
public static class Validation
{
	/// <summary>
	/// Gets or sets a value indicating whether validation checks are enabled.
	/// </summary>
	public static bool Enabled { get; set; }

	/// <summary>
	/// Asserts that a condition is true, throwing a <see cref="ValidationException"/> if it is not.
	/// </summary>
	/// <param name="condition">The condition to check.</param>
	/// <param name="message">The message to include in the exception if the condition is false.</param>
	/// <exception cref="ValidationException">Thrown if the condition is false and validation is enabled.</exception>
	public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
	{
		if (Enabled && !condition)
		{
			throw new ValidationException(message);
		}
	}

	/// <summary>
	/// Fails validation with a specific message, throwing a <see cref="ValidationException"/> if validation is enabled.
	/// </summary>
	/// <param name="message">The message to include in the exception.</param>
	/// <exception cref="ValidationException">Thrown if validation is enabled.</exception>
	public static void Fail(string message)
	{
		if (Enabled)
		{
			throw new ValidationException(message);
		}
	}
}
