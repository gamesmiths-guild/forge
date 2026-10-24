// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves whether an input action is held, or was pressed or released since the last tick, in an entity's input.
/// </summary>
/// <remarks>
/// This is the composable half of <see cref="Nodes.State.InputActionNode"/>: a bool an expression or a condition
/// monitor can gate on, where the node is what a graph waits on.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="actionName">The input action to read.</param>
/// <param name="state">Which state of the action to read.</param>
public class InputActionResolver(
	IEntityResolver inputSource,
	string actionName,
	InputActionState state = InputActionState.Held) : EntityInputResolver(inputSource)
{
	private readonly string _actionName = actionName;
	private readonly InputActionState _state = state;

	/// <inheritdoc/>
	public override Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="InputActionResolver"/> class reading the ability owner's input.
	/// </summary>
	/// <param name="actionName">The input action to read.</param>
	/// <param name="state">Which state of the action to read.</param>
	public InputActionResolver(string actionName, InputActionState state = InputActionState.Held)
		: this(AbilityOwner, actionName, state)
	{
	}

	/// <inheritdoc/>
	protected override Variant128 ResolveInput(EntityInput input)
	{
		return new Variant128(_state switch
		{
			InputActionState.Pressed => input.WasPressed(_actionName),
			InputActionState.Released => input.WasReleased(_actionName),
			_ => input.IsHeld(_actionName),
		});
	}
}
