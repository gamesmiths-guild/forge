// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Exception for when a Validation fails.
/// </summary>
public class ValidationException : Exception
{
	/// <inheritdoc/>
	public ValidationException()
	{
	}

	/// <inheritdoc/>
	public ValidationException(string? message)
		: base(message)
	{
	}

	/// <inheritdoc/>
	public ValidationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
