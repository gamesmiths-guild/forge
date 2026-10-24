// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// One tick of an entity's input: which actions are held, which were pressed or released since the last tick, the
/// analog vectors, and where the entity is aiming.
/// </summary>
/// <remarks>
/// <para>Bit <c>n</c> of <see cref="Held"/>, <see cref="Pressed"/> and <see cref="Released"/> is the action in slot
/// <c>n</c> of the entity's <see cref="InputActionSet"/>. A press and a release that both happen between two ticks set
/// both bits with the action no longer held, so a tap shorter than a tick still reaches the simulation.</para>
/// <para>Vectors and aim are stored already quantized - vectors in steps of 1/127, aim points in thousandths of a unit,
/// aim directions in steps of 1/32767 - so the machine that collects the input simulates exactly the values every other
/// machine receives.</para>
/// </remarks>
public readonly record struct InputFrame
{
	private const float VectorSteps = 127f;

	private const double AimPointSteps = 1000;

	private const double AimDirectionSteps = 32767;

	/// <summary>
	/// Gets the actions held when the tick's input was collected, one bit per action slot.
	/// </summary>
	public ulong Held { get; init; }

	/// <summary>
	/// Gets the actions pressed since the last tick, one bit per action slot.
	/// </summary>
	public ulong Pressed { get; init; }

	/// <summary>
	/// Gets the actions released since the last tick, one bit per action slot.
	/// </summary>
	public ulong Released { get; init; }

	/// <summary>
	/// Gets a value indicating whether the frame carries an aim, set by <see cref="WithAim"/>.
	/// </summary>
	public bool HasAim { get; private init; }

	/// <summary>
	/// Gets the point the entity aims at, in world space.
	/// </summary>
	public Vector3 AimPoint { get; private init; }

	/// <summary>
	/// Gets the direction the entity aims in, in world space.
	/// </summary>
	public Vector3 AimDirection { get; private init; }

	// Two signed bytes per vector, x then y, with vector 0 in the lowest bits.
	private ulong PackedVectors { get; init; }

	/// <summary>
	/// Gets whether an action is held.
	/// </summary>
	/// <param name="action">The action's slot.</param>
	/// <returns><see langword="true"/> if the action is held.</returns>
	public bool IsHeld(int action)
	{
		return (Held & Bit(action)) != 0;
	}

	/// <summary>
	/// Gets whether an action was pressed since the last tick.
	/// </summary>
	/// <param name="action">The action's slot.</param>
	/// <returns><see langword="true"/> if the action was pressed.</returns>
	public bool WasPressed(int action)
	{
		return (Pressed & Bit(action)) != 0;
	}

	/// <summary>
	/// Gets whether an action was released since the last tick.
	/// </summary>
	/// <param name="action">The action's slot.</param>
	/// <returns><see langword="true"/> if the action was released.</returns>
	public bool WasReleased(int action)
	{
		return (Released & Bit(action)) != 0;
	}

	/// <summary>
	/// Gets an analog vector.
	/// </summary>
	/// <param name="vector">The vector's slot.</param>
	/// <returns>The vector, each component between -1 and 1.</returns>
	public Vector2 GetVector(int vector)
	{
		int shift = VectorShift(vector);

		return new Vector2(
			(sbyte)(PackedVectors >> shift) / VectorSteps,
			(sbyte)(PackedVectors >> (shift + 8)) / VectorSteps);
	}

	/// <summary>
	/// Returns a copy of this frame with an analog vector set, quantized to steps of 1/127.
	/// </summary>
	/// <param name="vector">The vector's slot.</param>
	/// <param name="value">The vector, each component clamped between -1 and 1.</param>
	/// <returns>The new frame.</returns>
	public InputFrame WithVector(int vector, Vector2 value)
	{
		int shift = VectorShift(vector);
		ulong packed = (byte)Quantize(value.X) | ((ulong)(byte)Quantize(value.Y) << 8);

		return this with { PackedVectors = (PackedVectors & ~(0xFFFFUL << shift)) | (packed << shift) };
	}

	/// <summary>
	/// Returns a copy of this frame with an aim, quantized: the point to thousandths of a unit, the direction to steps
	/// of 1/32767.
	/// </summary>
	/// <param name="point">The point the entity aims at, in world space.</param>
	/// <param name="direction">The direction the entity aims in, in world space.</param>
	/// <returns>The new frame.</returns>
	public InputFrame WithAim(Vector3 point, Vector3 direction)
	{
		return this with
		{
			HasAim = true,
			AimPoint = Quantize(point, AimPointSteps),
			AimDirection = Quantize(direction, AimDirectionSteps),
		};
	}

	internal InputFrame WithoutEdges()
	{
		return this with { Pressed = 0, Released = 0 };
	}

	// The later frame's state stands, but no edge either frame saw is lost.
	internal InputFrame FollowedBy(InputFrame later)
	{
		return later with { Pressed = Pressed | later.Pressed, Released = Released | later.Released };
	}

	private static int VectorShift(int vector)
	{
		Validation.Assert(vector is >= 0 and < InputActionSet.MaxVectors, "An input vector slot is between 0 and 3.");

		return vector * 16;
	}

	private static ulong Bit(int action)
	{
		Validation.Assert(action is >= 0 and < InputActionSet.MaxActions, "An input action slot is between 0 and 63.");

		return 1UL << action;
	}

	private static sbyte Quantize(float component)
	{
		// NaN converts to an integer differently on each platform, so it is read as zero.
		return float.IsNaN(component) ? (sbyte)0 : (sbyte)Math.Round(Math.Clamp(component, -1f, 1f) * VectorSteps);
	}

	private static Vector3 Quantize(Vector3 value, double steps)
	{
		return new Vector3(Quantize(value.X, steps), Quantize(value.Y, steps), Quantize(value.Z, steps));
	}

	private static float Quantize(float value, double steps)
	{
		return (float)(Math.Round(value * steps) / steps);
	}
}
