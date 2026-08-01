// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Which entity involved in an effect application receives a <see cref="ConditionalEffect"/>.
/// </summary>
/// <remarks>
/// Pointing a conditional effect back at the source is what turns
/// <see cref="AdditionalEffectsEffectComponent"/> into lifesteal, recoil, or thorns without writing a
/// <see cref="Calculator.CustomExecution"/>.
/// </remarks>
public enum EffectApplicationTarget : byte
{
	/// <summary>
	/// The entity receiving the effect that carries the component.
	/// </summary>
	Target = 0,

	/// <summary>
	/// <see cref="EffectOwnership.Source"/> — what actually caused the effect.
	/// </summary>
	Source = 1,

	/// <summary>
	/// <see cref="EffectOwnership.Owner"/> — who triggered the action that caused the effect.
	/// </summary>
	Owner = 2,
}
