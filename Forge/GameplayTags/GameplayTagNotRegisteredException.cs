// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// Exception for when a <see cref="GameplayTag"/> is not registered with the <see cref="GameplayTagsManager"/>.
/// </summary>
public class GameplayTagNotRegisteredException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagNotRegisteredException"/> class.
	/// </summary>
	/// <param name="tagKey">Key for the missing tag.</param>
	public GameplayTagNotRegisteredException(StringKey tagKey)
		: base($"GameplayTag for StringKey \"{tagKey}\" could not be found within the tags tree.")
	{
	}

	/// <inheritdoc/>
	public GameplayTagNotRegisteredException()
	{
	}

	/// <inheritdoc/>
	public GameplayTagNotRegisteredException(string? message)
		: base(message)
	{
	}

	/// <inheritdoc/>
	public GameplayTagNotRegisteredException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
