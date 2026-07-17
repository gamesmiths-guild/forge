// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Routes to the True port with a resolved probability — ergonomic sugar for random branching.
/// </summary>
/// <remarks>
/// <para>The chance input must resolve to a <see langword="double"/> from 0 to 1. Unresolvable chances route to False.
/// </para>
/// <para>When no random provider is given, a non-deterministic <see cref="SystemRandom"/> is used. Inject a seeded
/// <see cref="IRandom"/> for deterministic behavior.</para>
/// </remarks>
/// <param name="randomProvider">The random provider used to roll the branch.</param>
public class RandomBranchNode(IRandom? randomProvider = null) : ConditionNode
{
	/// <summary>
	/// Input property index for the probability of routing to the True port.
	/// </summary>
	public const byte ChanceInput = 0;

	private readonly IRandom _randomProvider = randomProvider ?? new SystemRandom();

	/// <inheritdoc/>
	public override string Description => "Routes to True with the resolved probability.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Chance", typeof(double)));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[ChanceInput].BoundName, out double chance))
		{
			return false;
		}

		return _randomProvider.NextDouble() < chance;
	}
}
