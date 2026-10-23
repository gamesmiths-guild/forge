// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// The input actions and vectors an entity's input carries each tick, in the order that gives each its slot. Every
/// machine that simulates an entity uses the same set, so a slot means the same action everywhere.
/// </summary>
/// <remarks>
/// Actions travel as buttons: held, pressed and released. Analog values travel only through <see cref="Vectors"/>; an
/// action that is part of no vector reads as fully on while held and off otherwise.
/// </remarks>
public sealed class InputActionSet
{
	/// <summary>
	/// The most actions a set can carry, one bit each.
	/// </summary>
	public const int MaxActions = 64;

	/// <summary>
	/// The most vectors a set can carry.
	/// </summary>
	public const int MaxVectors = 4;

	private readonly Dictionary<string, int> _actionIndices = [];

	/// <summary>
	/// Gets the actions, in slot order.
	/// </summary>
	public IReadOnlyList<string> Actions { get; }

	/// <summary>
	/// Gets the analog vectors, in slot order.
	/// </summary>
	public IReadOnlyList<InputVector> Vectors { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="InputActionSet"/> class.
	/// </summary>
	/// <param name="actions">The actions, in slot order. At most <see cref="MaxActions"/>, each named once.</param>
	/// <param name="vectors">The analog vectors, in slot order. At most <see cref="MaxVectors"/>.</param>
	/// <exception cref="ArgumentException">Thrown when there are too many actions or vectors, or an action is named
	/// twice.</exception>
	public InputActionSet(IReadOnlyList<string> actions, IReadOnlyList<InputVector>? vectors = null)
	{
		vectors ??= [];

		if (actions.Count > MaxActions)
		{
			throw new ArgumentException($"An input action set holds at most {MaxActions} actions.", nameof(actions));
		}

		if (vectors.Count > MaxVectors)
		{
			throw new ArgumentException($"An input action set holds at most {MaxVectors} vectors.", nameof(vectors));
		}

		for (int i = 0; i < actions.Count; i++)
		{
			if (!_actionIndices.TryAdd(actions[i], i))
			{
				throw new ArgumentException($"The action '{actions[i]}' is listed twice.", nameof(actions));
			}
		}

		Actions = [.. actions];
		Vectors = [.. vectors];
	}

	/// <summary>
	/// Gets an action's slot.
	/// </summary>
	/// <param name="action">The action's name.</param>
	/// <returns>The action's slot, or -1 if the set does not carry it.</returns>
	public int IndexOf(string action)
	{
		return _actionIndices.TryGetValue(action, out int index) ? index : -1;
	}
}
