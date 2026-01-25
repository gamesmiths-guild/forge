// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Events;

/// <summary>
/// Represents data associated with an event within the Forge framework.
/// </summary>
public readonly record struct EventData
{
	/// <summary>
	/// Gets the tags associated with the event.
	/// </summary>
	public TagContainer EventTags { get; init; }

	/// <summary>
	/// Gets the source entity that triggered the event.
	/// </summary>
	public IForgeEntity? Source { get; init; }

	/// <summary>
	/// Gets the target entity of the event.
	/// </summary>
	public IForgeEntity? Target { get; init; }

	/// <summary>
	/// Gets the magnitude or intensity of the event.
	/// </summary>
	public float EventMagnitude { get; init; }

	/// <summary>
	/// Gets any additional payload data associated with the event.
	/// </summary>
	public object? Payload { get; init; }
}

/// <summary>
/// Represents data associated with an event within the Forge framework.
/// </summary>
/// <typeparam name="TPayload">The type of the payload data associated with the event.</typeparam>
public readonly record struct EventData<TPayload>
{
	/// <summary>
	/// Gets the tags associated with the event.
	/// </summary>
	public TagContainer EventTags { get; init; }

	/// <summary>
	/// Gets the source entity that triggered the event.
	/// </summary>
	public IForgeEntity? Source { get; init; }

	/// <summary>
	/// Gets the target entity of the event.
	/// </summary>
	public IForgeEntity? Target { get; init; }

	/// <summary>
	/// Gets the magnitude or intensity of the event.
	/// </summary>
	public float EventMagnitude { get; init; }

	/// <summary>
	/// Gets the additional payload data associated with the event.
	/// </summary>
	public TPayload Payload { get; init; }
}
