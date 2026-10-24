// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves how far an input action is pressed in an entity's input, from 0 to 1.
/// </summary>
/// <remarks>
/// Only an action that is a direction of one of the input's vectors has values in between, such as a stick pushed part
/// way. Any other action is a button: 1 while held and 0 otherwise.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="actionName">The input action to read.</param>
public class InputStrengthResolver(IEntityResolver inputSource, string actionName) : EntityInputResolver(inputSource)
{
	private readonly string _actionName = actionName;

	/// <inheritdoc/>
	public override Type ValueType => typeof(float);

	/// <summary>
	/// Initializes a new instance of the <see cref="InputStrengthResolver"/> class reading the ability owner's input.
	/// </summary>
	/// <param name="actionName">The input action to read.</param>
	public InputStrengthResolver(string actionName)
		: this(AbilityOwner, actionName)
	{
	}

	/// <inheritdoc/>
	protected override Variant128 ResolveInput(EntityInput input)
	{
		return new Variant128(input.GetStrength(_actionName));
	}
}
