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

	/// <summary>
	/// Called once per frame (or tick) to advance the behavior's internal state. The default implementation does
	/// nothing, which is correct for behaviors that complete synchronously during <see cref="OnStarted"/>.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	void OnUpdate(double deltaTime)
	{
	}
}

/// <summary>
/// Interface for defining custom behavior with strongly-typed additional data.
/// </summary>
/// <typeparam name="TData">The type of the additional data expected.</typeparam>
public interface IAbilityBehavior<in TData> : IAbilityBehavior
{
	/// <summary>
	/// Called when an ability instance has started with a typed data.
	/// </summary>
	/// <param name="context">The context for the started ability instance.</param>
	/// <param name="data">The strongly-typed additional data from the triggering event.</param>
	void OnStarted(AbilityBehaviorContext context, TData data);

	/// <inheritdoc/>
	void IAbilityBehavior.OnStarted(AbilityBehaviorContext context)
	{
		// Default implementation for non-payload activations
		OnStarted(context, default!);
	}
}
