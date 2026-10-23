// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// What a node's work means to a networked game: whether it changes the gameplay every peer has to agree on, only
/// shows something to whoever is watching, or is something only the server may decide.
/// </summary>
public enum NodeNetworkKind : byte
{
	/// <summary>
	/// Changes gameplay: attributes, tags, effects, movement, or anything the graph reads back. This is the default.
	/// </summary>
	Simulation = 0,

	/// <summary>
	/// Only shows something - an animation, a sound, a visual property - and never feeds back into the graph. A
	/// presentation state node does its engine work in the presentation pass, on net change, through
	/// <c>OnPresentationActivated</c> and <c>OnPresentationDeactivated</c>.
	/// </summary>
	Presentation = 1,

	/// <summary>
	/// Does something only the server may decide, such as spawning or freeing a networked entity.
	/// </summary>
	Authority = 2,
}
