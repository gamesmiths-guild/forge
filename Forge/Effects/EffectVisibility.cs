// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Who hears about an effect while it's active on a networked entity. It decides who gets the effect itself: what it
/// did to the entity's attributes and tags, and its cues, are the entity's state, which reaches every client that has
/// the entity whatever this says.
/// </summary>
public enum EffectVisibility : byte
{
	/// <summary>
	/// What the server's <see cref="Networking.Replication.ReplicationServer.DefaultEffectVisibility"/> says.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Every client that has the entity.
	/// </summary>
	Public = 1,

	/// <summary>
	/// Only the client that owns the entity.
	/// </summary>
	OwnerOnly = 2,

	/// <summary>
	/// No client.
	/// </summary>
	ServerOnly = 3,
}
