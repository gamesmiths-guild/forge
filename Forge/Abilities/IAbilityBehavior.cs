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

/// <summary>
/// Interface for defining custom behavior with a strongly-typed payload.
/// </summary>
/// <typeparam name="TPayload">The type of payload expected from the triggering event.</typeparam>
public interface IAbilityBehavior<in TPayload> : IAbilityBehavior
{
	/// <summary>
	/// Called when an ability instance has started with a typed payload.
	/// </summary>
	/// <param name="context">The context for the started ability instance.</param>
	/// <param name="payload">The strongly-typed payload from the triggering event.</param>
	void OnStarted(AbilityBehaviorContext context, TPayload payload);

	/// <inheritdoc/>
	void IAbilityBehavior.OnStarted(AbilityBehaviorContext context)
	{
		// Default implementation for non-payload activations
		OnStarted(context, default!);
	}
}
