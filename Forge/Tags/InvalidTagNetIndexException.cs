// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Exception for when a valid <see cref="Tag"/> cannot be found for a given net index.
/// </summary>
public class InvalidTagNetIndexException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InvalidTagNetIndexException"/> class.
	/// </summary>
	/// <param name="tagIndex">The invalid tag net index.</param>
	public InvalidTagNetIndexException(ushort tagIndex)
		: base($"Received invalid tag net index {tagIndex}! Tag index is out of sync on client!")
	{
	}

	/// <inheritdoc/>
	public InvalidTagNetIndexException()
	{
	}

	/// <inheritdoc/>
	public InvalidTagNetIndexException(string? message)
		: base(message)
	{
	}

	/// <inheritdoc/>
	public InvalidTagNetIndexException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
