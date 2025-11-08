// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Interface for defining custom behavior when an ability instance starts and ends.
/// </summary>
public interface IAbilityBehavior
{
	/// <summary>
	/// Called when an ability instance has started.
	/// </summary>
	/// <param name="context">The context for the started ability instance.</param>
	void OnStarted(AbilityBehaviorContext context);

	/// <summary>
	/// Called when an ability instance has ended.
	/// </summary>
	/// <param name="context">The context for the ended ability instance.</param>
	void OnEnded(AbilityBehaviorContext context);
}
