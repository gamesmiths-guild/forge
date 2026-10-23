// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// An entity's input: the frame its current tick reads, and the frame waiting for the next tick.
/// </summary>
/// <remarks>
/// <para>Whatever collects input <see cref="Submit"/>s frames between ticks, and <see cref="EntitySimulation.Advance"/>
/// makes the waiting frame <see cref="Current"/> as the tick begins. Frames submitted more than once before a tick are
/// merged: the latest one's state stands, but every press and release either of them saw is kept. A tick that finds
/// nothing waiting keeps the last frame's state with no new presses or releases, so held actions stay held.</para>
/// <para>The readers that take names answer the way an engine's input API does, from the current frame: a strength,
/// an axis and a vector come from the set's analog <see cref="InputActionSet.Vectors"/> when one of them covers the
/// actions asked about, and otherwise from whether the actions are held.</para>
/// </remarks>
/// <param name="actions">The actions and vectors this input carries.</param>
public sealed class EntityInput(InputActionSet actions)
{
	private InputFrame? _waiting;

	/// <summary>
	/// Gets the actions and vectors this input carries.
	/// </summary>
	public InputActionSet Actions { get; } = actions;

	/// <summary>
	/// Gets the frame the current tick reads.
	/// </summary>
	public InputFrame Current { get; private set; }

	/// <summary>
	/// Submits the input for the next tick, merging it with any frame already waiting for that tick.
	/// </summary>
	/// <param name="frame">The input.</param>
	public void Submit(in InputFrame frame)
	{
		_waiting = _waiting is InputFrame waiting ? waiting.FollowedBy(frame) : frame;
	}

	/// <summary>
	/// Gets whether an action is held this tick.
	/// </summary>
	/// <param name="action">The action's name.</param>
	/// <returns><see langword="true"/> if the action is held; <see langword="false"/> if not, or if this input does not
	/// carry it.</returns>
	public bool IsHeld(string action)
	{
		int index = Actions.IndexOf(action);
		return index >= 0 && Current.IsHeld(index);
	}

	/// <summary>
	/// Gets whether an action was pressed since the last tick.
	/// </summary>
	/// <param name="action">The action's name.</param>
	/// <returns><see langword="true"/> if the action was pressed; <see langword="false"/> if not, or if this input
	/// does not carry it.</returns>
	public bool WasPressed(string action)
	{
		int index = Actions.IndexOf(action);
		return index >= 0 && Current.WasPressed(index);
	}

	/// <summary>
	/// Gets whether an action was released since the last tick.
	/// </summary>
	/// <param name="action">The action's name.</param>
	/// <returns><see langword="true"/> if the action was released; <see langword="false"/> if not, or if this input
	/// does not carry it.</returns>
	public bool WasReleased(string action)
	{
		int index = Actions.IndexOf(action);
		return index >= 0 && Current.WasReleased(index);
	}

	/// <summary>
	/// Gets how strongly an action is applied this tick.
	/// </summary>
	/// <param name="action">The action's name.</param>
	/// <returns>Between 0 and 1: from a vector that has the action as one of its four directions, or otherwise 1 while
	/// the action is held and 0 when it is not.</returns>
	public float GetStrength(string action)
	{
		for (int i = 0; i < Actions.Vectors.Count; i++)
		{
			InputVector vector = Actions.Vectors[i];
			Vector2 value = Current.GetVector(i);

			if (vector.PositiveX == action)
			{
				return Math.Max(value.X, 0f);
			}

			if (vector.NegativeX == action)
			{
				return Math.Max(-value.X, 0f);
			}

			if (vector.PositiveY == action)
			{
				return Math.Max(value.Y, 0f);
			}

			if (vector.NegativeY == action)
			{
				return Math.Max(-value.Y, 0f);
			}
		}

		return IsHeld(action) ? 1f : 0f;
	}

	/// <summary>
	/// Gets an axis made of two actions this tick.
	/// </summary>
	/// <param name="negative">The action that pushes the axis towards -1.</param>
	/// <param name="positive">The action that pushes the axis towards 1.</param>
	/// <returns>Between -1 and 1: a vector's component when a vector has the two as one of its axes, or otherwise the
	/// positive action's strength minus the negative one's.</returns>
	public float GetAxis(string negative, string positive)
	{
		for (int i = 0; i < Actions.Vectors.Count; i++)
		{
			InputVector vector = Actions.Vectors[i];

			if (vector.NegativeX == negative && vector.PositiveX == positive)
			{
				return Current.GetVector(i).X;
			}

			if (vector.NegativeY == negative && vector.PositiveY == positive)
			{
				return Current.GetVector(i).Y;
			}
		}

		return GetStrength(positive) - GetStrength(negative);
	}

	/// <summary>
	/// Gets a vector made of four actions this tick.
	/// </summary>
	/// <param name="negativeX">The action that pushes X towards -1.</param>
	/// <param name="positiveX">The action that pushes X towards 1.</param>
	/// <param name="negativeY">The action that pushes Y towards -1.</param>
	/// <param name="positiveY">The action that pushes Y towards 1.</param>
	/// <returns>The vector the set carries for these four actions, or otherwise one built from the two axes, no longer
	/// than 1.</returns>
	public Vector2 GetVector(string negativeX, string positiveX, string negativeY, string positiveY)
	{
		int index = IndexOfVector(new InputVector(negativeX, positiveX, negativeY, positiveY));

		if (index >= 0)
		{
			return Current.GetVector(index);
		}

		var value = new Vector2(GetAxis(negativeX, positiveX), GetAxis(negativeY, positiveY));

		return value.LengthSquared() > 1f ? Vector2.Normalize(value) : value;
	}

	// The waiting frame becomes the tick's input; with none waiting, the last one carries on without new edges.
	internal void BeginTick()
	{
		Current = _waiting ?? Current.WithoutEdges();
		_waiting = null;
	}

	private int IndexOfVector(InputVector vector)
	{
		for (int i = 0; i < Actions.Vectors.Count; i++)
		{
			if (Actions.Vectors[i] == vector)
			{
				return i;
			}
		}

		return -1;
	}
}
