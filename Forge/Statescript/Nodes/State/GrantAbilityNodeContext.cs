// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="GrantAbilityNode"/>. Tracks the granted handle, its grant source and the entity it
/// was granted on so the grant can be revoked on deactivation.
/// </summary>
public class GrantAbilityNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the handle of the ability granted by this activation, or <see langword="null"/> when nothing was
	/// granted.
	/// </summary>
	public AbilityHandle? GrantedHandle { get; set; }

	/// <summary>
	/// Gets or sets the entity the ability was granted on.
	/// </summary>
	public IForgeEntity? GrantedOn { get; set; }

	internal IAbilityGrantSource? GrantSource { get; set; }
}
