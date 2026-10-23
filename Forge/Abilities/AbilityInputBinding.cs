// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Binds an ability to an input action: the ability tries to activate in the tick its owner's input reports the
/// action's <see cref="Edge"/>, before effects and graphs advance.
/// </summary>
/// <remarks>
/// <para>The binding runs in the input stage of <see cref="EntitySimulation.Advance"/>, so an entity activates its
/// bound abilities only when it has an <see cref="IForgeEntity.Input"/> and is ticked. A binding to an action the
/// entity's input does not carry never fires.</para>
/// <para>A binding made with activation data builds that data from the same tick's input, through the function it was
/// made with, and activates the ability with it.</para>
/// </remarks>
public readonly record struct AbilityInputBinding
{
	/// <summary>
	/// Gets the input action the ability is bound to.
	/// </summary>
	public string Action { get; }

	/// <summary>
	/// Gets the change of the action that activates the ability.
	/// </summary>
	public InputEdge Edge { get; }

	internal Func<Ability, EntityInput, bool> Activate { get; }

	private AbilityInputBinding(string action, InputEdge edge, Func<Ability, EntityInput, bool> activate)
	{
		Action = action;
		Edge = edge;
		Activate = activate;
	}

	/// <summary>
	/// Creates a binding that activates the ability without activation data.
	/// </summary>
	/// <param name="action">The input action.</param>
	/// <param name="edge">The change of the action that activates the ability.</param>
	/// <returns>The binding.</returns>
	public static AbilityInputBinding ForAction(string action, InputEdge edge = InputEdge.Pressed)
	{
		return new AbilityInputBinding(
			action,
			edge,
			static (ability, _) => ability.TryActivateAbility(null, out AbilityActivationFailures _, 0f));
	}

	/// <summary>
	/// Creates a binding that activates the ability with data built from the tick's input, such as where the entity
	/// aims.
	/// </summary>
	/// <typeparam name="TData">The type of the activation data.</typeparam>
	/// <param name="action">The input action.</param>
	/// <param name="activationData">Builds the activation data from the ability's owner and its input.</param>
	/// <param name="edge">The change of the action that activates the ability.</param>
	/// <returns>The binding.</returns>
	public static AbilityInputBinding ForAction<TData>(
		string action,
		Func<IForgeEntity, EntityInput, TData> activationData,
		InputEdge edge = InputEdge.Pressed)
	{
		// Captured here, where TData is still known at compile time, so AOT builds get the typed activation.
		return new AbilityInputBinding(
			action,
			edge,
			(ability, input) => ability.TryActivateAbility(null, out _, activationData(ability.Owner, input), 0f));
	}
}
