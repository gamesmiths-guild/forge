// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Sets a SetByCaller magnitude on one or more <see cref="Effect"/> instances.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="Effect"/> or an array of effects. The tag input accepts
/// either a single <see cref="Tag"/> or an array of tags. The resolved magnitude is written for every tag on every
/// effect.</para>
/// <para>Setting a magnitude on an effect that has not been applied yet configures the value its
/// <see cref="SetByCallerFloat"/> magnitudes will read on application. Setting it on an effect that is already active
/// live-updates non-snapshot SetByCaller magnitudes.</para>
/// <para>Effects held in variables are shared instances: writing their SetByCaller values affects every future
/// application of that instance.</para>
/// <para>The magnitude input must resolve to a <see langword="double"/> value, which is cast to
/// <see langword="float"/> when written.</para>
/// </remarks>
public class SetByCallerMagnitudeNode : ActionNode
{
	/// <summary>
	/// Input property index for the effect instance(s).
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Input property index for the SetByCaller identifier tag(s).
	/// </summary>
	public const byte TagInput = 1;

	/// <summary>
	/// Input property index for the magnitude value.
	/// </summary>
	public const byte MagnitudeInput = 2;

	/// <inheritdoc/>
	public override string Description => "Sets a SetByCaller magnitude on effects, keyed by tag.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(Effect)));
		inputProperties.Add(new InputProperty("Tag", typeof(Tag)));
		inputProperties.Add(new InputProperty("Magnitude", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!EffectApplicationUtilities.TryResolveEffects(
			graphContext,
			InputProperties[EffectInput].BoundName,
			out IReadOnlyList<Effect> effects)
			|| !EffectApplicationUtilities.TryResolveTags(
				graphContext,
				InputProperties[TagInput].BoundName,
				out IReadOnlyList<Tag> tags)
			|| !graphContext.TryResolve(InputProperties[MagnitudeInput].BoundName, out double magnitude))
		{
			return;
		}

		for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
		{
			for (int tagIndex = 0; tagIndex < tags.Count; tagIndex++)
			{
				effects[effectIndex].SetSetByCallerMagnitude(tags[tagIndex], (float)magnitude);
			}
		}
	}
}
