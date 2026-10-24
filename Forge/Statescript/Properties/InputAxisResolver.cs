// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the signed strength of a pair of opposed input actions in an entity's input, from -1 to 1.
/// </summary>
/// <remarks>
/// The two actions cancel, so holding both reads zero. When one of the input's vectors has the pair as an axis, the
/// value is that axis of the vector.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="negativeActionName">The action driving the value towards -1.</param>
/// <param name="positiveActionName">The action driving the value towards 1.</param>
public class InputAxisResolver(IEntityResolver inputSource, string negativeActionName, string positiveActionName)
	: EntityInputResolver(inputSource)
{
	private readonly string _negativeActionName = negativeActionName;
	private readonly string _positiveActionName = positiveActionName;

	/// <inheritdoc/>
	public override Type ValueType => typeof(float);

	/// <summary>
	/// Initializes a new instance of the <see cref="InputAxisResolver"/> class reading the ability owner's input.
	/// </summary>
	/// <param name="negativeActionName">The action driving the value towards -1.</param>
	/// <param name="positiveActionName">The action driving the value towards 1.</param>
	public InputAxisResolver(string negativeActionName, string positiveActionName)
		: this(AbilityOwner, negativeActionName, positiveActionName)
	{
	}

	/// <inheritdoc/>
	protected override Variant128 ResolveInput(EntityInput input)
	{
		return new Variant128(input.GetAxis(_negativeActionName, _positiveActionName));
	}
}
