// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Stacking;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Removes one or more active effects through their <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>The handle input accepts either a single <see cref="ActiveEffectHandle"/> or an array of handles (such as
/// the handle output of an ApplyEffect node or the result of an active-effect query). Invalid handles are skipped
/// silently.</para>
/// <para>For stackable effects with <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>, a
/// non-forced removal removes a single stack; a forced removal removes the entire active effect.</para>
/// </remarks>
/// <param name="forceRemoval">Whether to force removal of the entire active effect regardless of its stacking
/// expiration policy.</param>
public class RemoveEffectNode(bool forceRemoval = false) : ActionNode
{
	/// <summary>
	/// Input property index for the active effect handle(s).
	/// </summary>
	public const byte HandleInput = 0;

	private readonly bool _forceRemoval = forceRemoval;

	/// <inheritdoc/>
	public override string Description => "Removes active effects through their handles.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Active Effect", typeof(ActiveEffectHandle)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!EffectApplicationUtilities.TryResolveHandles(
			graphContext,
			InputProperties[HandleInput].BoundName,
			out IReadOnlyList<ActiveEffectHandle> handles))
		{
			return;
		}

		for (int i = 0; i < handles.Count; i++)
		{
			ActiveEffectHandle handle = handles[i];

			handle.Target?.EffectsManager.RemoveEffect(handle, _forceRemoval);
		}
	}
}
