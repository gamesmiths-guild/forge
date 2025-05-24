// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Exception for when a <see cref="Tag"/> is not registered with the <see cref="TagsManager"/>.
/// </summary>
public class TagNotRegisteredException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TagNotRegisteredException"/> class.
	/// </summary>
	/// <param name="tagKey">Key for the missing tag.</param>
	public TagNotRegisteredException(StringKey tagKey)
		: base($"{nameof(Tag)} for {nameof(StringKey)} \"{tagKey}\" could not be found within the tags tree.")
	{
	}

	/// <inheritdoc/>
	public TagNotRegisteredException()
	{
	}

	/// <inheritdoc/>
	public TagNotRegisteredException(string? message)
		: base(message)
	{
	}

	/// <inheritdoc/>
	public TagNotRegisteredException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
