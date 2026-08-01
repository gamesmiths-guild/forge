// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// One effect an <see cref="AdditionalEffectsEffectComponent"/> applies off the back of its own, together with the
/// condition gating it, where it lands, and what becomes of it afterwards.
/// </summary>
/// <remarks>
/// <para>
/// The child always inherits its applier's ownership and evaluated level. Whether it also inherits the applier's
/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes is decided once, for the whole component, by
/// <c>copyDataFromOriginalEffect</c>.
/// </para>
/// <para>
/// <paramref name="StacksToRemove"/> only means anything under
/// <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/>.
/// </para>
/// </remarks>
/// <param name="EffectData">The effect to apply.</param>
/// <param name="SourceTagRequirements">Requirements the effect's source must meet for this one to be applied. When
/// <see langword="null"/> or empty, the effect is always applied.</param>
/// <param name="RemovalPolicy">Whether this effect outlives the one that applied it.</param>
/// <param name="StacksToRemove">How many stacks to take when the applier ends under
/// <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/>. Any negative value, the default, removes the effect
/// entirely regardless of its stack count.</param>
/// <param name="Target">Which entity receives the effect.</param>
public readonly record struct ConditionalEffect(
	EffectData EffectData,
	TagRequirements? SourceTagRequirements = null,
	ConditionalEffectRemovalPolicy RemovalPolicy = ConditionalEffectRemovalPolicy.Ignore,
	int StacksToRemove = -1,
	EffectApplicationTarget Target = EffectApplicationTarget.Target);
