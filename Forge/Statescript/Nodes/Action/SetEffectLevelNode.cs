// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Levels up one or more <see cref="Effect"/> instances, or sets their level to a resolved value.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="Effect"/> or an array of effects.</para>
/// <para>Level changes on effects that are already active re-evaluate non-snapshot-level applications through
/// <see cref="Effect.OnLevelChanged"/>.</para>
/// <para>The level input is only read for <see cref="SetEffectLevelOperation.SetLevel"/> and must resolve to an
/// <see langword="int"/>.</para>
/// </remarks>
/// <param name="operation">How to change the effect level. Defaults to <see cref="SetEffectLevelOperation.LevelUp"/>.
/// </param>
public class SetEffectLevelNode(SetEffectLevelOperation operation = SetEffectLevelOperation.LevelUp) : ActionNode
{
	/// <summary>
	/// Input property index for the effect instance(s).
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Input property index for the level value used by <see cref="SetEffectLevelOperation.SetLevel"/>.
	/// </summary>
	public const byte LevelInput = 1;

	private readonly SetEffectLevelOperation _operation = operation;

	/// <inheritdoc/>
	public override string Description => "Levels up effects or sets their level to a resolved value.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(Effect)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!EffectApplicationUtilities.TryResolveEffects(
			graphContext,
			InputProperties[EffectInput].BoundName,
			out IReadOnlyList<Effect> effects))
		{
			return;
		}

		if (_operation == SetEffectLevelOperation.SetLevel)
		{
			if (!graphContext.TryResolve(InputProperties[LevelInput].BoundName, out int level))
			{
				return;
			}

			for (int i = 0; i < effects.Count; i++)
			{
				effects[i].SetLevel(level);
			}

			return;
		}

		for (int i = 0; i < effects.Count; i++)
		{
			effects[i].LevelUp();
		}
	}
}
