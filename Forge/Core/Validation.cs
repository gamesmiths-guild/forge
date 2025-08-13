// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Provides development-time validation checks for Forge consumers.
/// <para>
/// Validation is <b>disabled by default</b>. Set <see cref="Enabled"/> to <c>true</c> in editor, test, or debug environments
/// to catch misconfigurations and logic errors. In production/release, leave it <c>false</c> for optimal performance.
/// </para>
/// <para>
/// When enabled, failed validations throw <see cref="ValidationException"/>.
/// </para>
/// </summary>
/// <remarks>
/// <para>Typical usage:</para>
/// <code>
/// Validation.Enabled = true; // Enable in editor or during tests
/// Validation.Assert(myCondition, "Detailed failure message.");
/// </code>
/// </remarks>
public static class Validation
{
	/// <summary>
	/// Gets or sets a value indicating whether validation checks are enabled.
	/// <para>
	/// Default is <c>false</c>. Set to <c>true</c> in editor, test, or debug environments to enable runtime validation.
	/// </para>
	/// </summary>
	public static bool Enabled { get; set; }

	/// <summary>
	/// Asserts that a condition is true, throwing a <see cref="ValidationException"/> if it is not and validation is enabled.
	/// </summary>
	/// <param name="condition">The condition to check.</param>
	/// <param name="message">The message to include in the exception if the condition is false.</param>
	/// <exception cref="ValidationException">
	/// Thrown if <paramref name="condition"/> is false and <see cref="Enabled"/> is <c>true</c>.
	/// </exception>
	public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
	{
		if (Enabled && !condition)
		{
			throw new ValidationException(message);
		}
	}

	/// <summary>
	/// Explicitly fails validation with a specific message, throwing a <see cref="ValidationException"/> if validation is enabled.
	/// </summary>
	/// <param name="message">The message to include in the exception.</param>
	/// <exception cref="ValidationException">
	/// Thrown if <see cref="Enabled"/> is <c>true</c>.
	/// </exception>
	public static void Fail(string message)
	{
		if (Enabled)
		{
			throw new ValidationException(message);
		}
	}
}
