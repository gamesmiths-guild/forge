// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Sets the inhibition state of one or more active effects through their <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>The handle input accepts either a single <see cref="ActiveEffectHandle"/> or an array of handles. Invalid
/// handles are skipped silently.</para>
/// <para>Inhibited effects keep their remaining duration ticking but suspend their modifiers and periodic executions
/// until the inhibition is lifted.</para>
/// <para>The inhibited input must resolve to a <see langword="bool"/>.</para>
/// </remarks>
public class SetEffectInhibitionNode : ActionNode
{
	/// <summary>
	/// Input property index for the active effect handle(s).
	/// </summary>
	public const byte HandleInput = 0;

	/// <summary>
	/// Input property index for the desired inhibition state.
	/// </summary>
	public const byte InhibitedInput = 1;

	/// <inheritdoc/>
	public override string Description => "Sets the inhibition state of active effects.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Active Effect", typeof(ActiveEffectHandle)));
		inputProperties.Add(new InputProperty("Inhibited", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!EffectApplicationUtilities.TryResolveHandles(
			graphContext,
			InputProperties[HandleInput].BoundName,
			out IReadOnlyList<ActiveEffectHandle> handles)
			|| !graphContext.TryResolve(InputProperties[InhibitedInput].BoundName, out bool inhibited))
		{
			return;
		}

		for (int i = 0; i < handles.Count; i++)
		{
			handles[i].SetInhibit(inhibited);
		}
	}
}
