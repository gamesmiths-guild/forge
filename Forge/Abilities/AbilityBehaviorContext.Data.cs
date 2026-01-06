// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Context that carries strongly-typed additional data.
/// Created automatically when using <see cref="AbilityHandle.Activate{TData}"/>.
/// </summary>
/// <typeparam name="TData">The activation data type.</typeparam>
public sealed class AbilityBehaviorContext<TData> : AbilityBehaviorContext
{
	/// <summary>
	/// Gets the additional data passed during ability activation.
	/// </summary>
	public TData Data { get; }

	internal AbilityBehaviorContext(
		Ability ability,
		AbilityInstance instance,
		TData data,
		float magnitude)
		: base(ability, instance, magnitude)
	{
		Data = data;
	}
}
