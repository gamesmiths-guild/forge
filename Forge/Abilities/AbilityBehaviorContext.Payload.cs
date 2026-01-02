// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Context that carries strongly-typed activation data.
/// Created automatically when using <see cref="AbilityHandle.Activate{TContextData}"/>.
/// </summary>
/// <typeparam name="TPayload">The activation data type.</typeparam>
public sealed class AbilityBehaviorContext<TPayload> : AbilityBehaviorContext
{
	/// <summary>
	/// Gets the activation data passed during ability activation.
	/// </summary>
	public TPayload Payload { get; }

	internal AbilityBehaviorContext(
		Ability ability,
		AbilityInstance instance,
		TPayload payload)
		: base(ability, instance)
	{
		Payload = payload;
	}
}
