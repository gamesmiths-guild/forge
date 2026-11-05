// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// Writes and reads body states, quantized.
/// </summary>
public static class BodyCodec
{
	/// <summary>
	/// The step positions are rounded to, in units.
	/// </summary>
	public const float PositionStep = 0.001f;

	/// <summary>
	/// The step velocities are rounded to, in units per second.
	/// </summary>
	public const float VelocityStep = 0.01f;

	/// <summary>
	/// How many bits each of a rotation's three smallest components takes.
	/// </summary>
	public const int RotationBits = 9;

	// The steps in a unit, whole, so a state rebuilt from its steps is the float nearest the decimal value it stands for.
	private const double PositionSteps = 1000;

	private const double VelocitySteps = 100;

	// A unit quaternion's three smallest components lie within this of zero.
	private const float RotationRange = 0.70710678f;

	private const int RotationLevels = (1 << RotationBits) - 1;

	/// <summary>
	/// Quantizes a body state.
	/// </summary>
	/// <param name="state">The state.</param>
	/// <returns>The state as it travels.</returns>
	public static QuantizedBody Quantize(in BodyState state)
	{
		var rotation = Quaternion.Normalize(state.Rotation);

		if (float.IsNaN(rotation.X))
		{
			rotation = Quaternion.Identity;
		}

		Span<float> components = [rotation.X, rotation.Y, rotation.Z, rotation.W];
		byte largest = 0;

		for (byte i = 1; i < 4; i++)
		{
			if (Math.Abs(components[i]) > Math.Abs(components[largest]))
			{
				largest = i;
			}
		}

		// A rotation and its negation are the same rotation, so the largest component is sent positive.
		if (components[largest] < 0)
		{
			for (int i = 0; i < 4; i++)
			{
				components[i] = -components[i];
			}
		}

		Span<ushort> levels = stackalloc ushort[3];
		int written = 0;

		for (int i = 0; i < 4; i++)
		{
			if (i != largest)
			{
				levels[written++] = Level(components[i]);
			}
		}

		return new QuantizedBody(
			Steps(state.Position.X, PositionSteps),
			Steps(state.Position.Y, PositionSteps),
			Steps(state.Position.Z, PositionSteps),
			largest,
			levels[0],
			levels[1],
			levels[2],
			Steps(state.Velocity.X, VelocitySteps),
			Steps(state.Velocity.Y, VelocitySteps),
			Steps(state.Velocity.Z, VelocitySteps),
			state.Grounded);
	}

	/// <summary>
	/// Rebuilds the body state a quantized one stands for.
	/// </summary>
	/// <param name="body">The quantized state.</param>
	/// <returns>The state, as the receiver sees it.</returns>
	public static BodyState Dequantize(in QuantizedBody body)
	{
		Span<float> components = stackalloc float[4];
		Span<float> smallest = [FromLevel(body.A), FromLevel(body.B), FromLevel(body.C)];
		int read = 0;
		float sumOfSquares = 0;

		for (int i = 0; i < 4; i++)
		{
			if (i != body.Largest)
			{
				components[i] = smallest[read++];
				sumOfSquares += components[i] * components[i];
			}
		}

		components[body.Largest] = MathF.Sqrt(Math.Max(0, 1 - sumOfSquares));

		return new BodyState(
			new Vector3((float)(body.X / PositionSteps), (float)(body.Y / PositionSteps), (float)(body.Z / PositionSteps)),
			Quaternion.Normalize(new Quaternion(components[0], components[1], components[2], components[3])),
			new Vector3(
				(float)(body.VelocityX / VelocitySteps),
				(float)(body.VelocityY / VelocitySteps),
				(float)(body.VelocityZ / VelocitySteps)),
			body.Grounded);
	}

	/// <summary>
	/// Writes a quantized body state.
	/// </summary>
	/// <param name="writer">The writer.</param>
	/// <param name="body">The state.</param>
	public static void Write(BitWriter writer, in QuantizedBody body)
	{
		writer.WriteVarInt(body.X);
		writer.WriteVarInt(body.Y);
		writer.WriteVarInt(body.Z);
		writer.WriteBits(body.Largest, 2);
		writer.WriteBits(body.A, RotationBits);
		writer.WriteBits(body.B, RotationBits);
		writer.WriteBits(body.C, RotationBits);
		writer.WriteVarInt(body.VelocityX);
		writer.WriteVarInt(body.VelocityY);
		writer.WriteVarInt(body.VelocityZ);
		writer.WriteBool(body.Grounded);
	}

	/// <summary>
	/// Reads a quantized body state.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <returns>The state.</returns>
	public static QuantizedBody Read(BitReader reader)
	{
		return new QuantizedBody(
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			(byte)reader.ReadBits(2),
			(ushort)reader.ReadBits(RotationBits),
			(ushort)reader.ReadBits(RotationBits),
			(ushort)reader.ReadBits(RotationBits),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadBool());
	}

	private static long Steps(float value, double stepsPerUnit)
	{
		return float.IsNaN(value) ? 0 : (long)Math.Round(value * stepsPerUnit);
	}

	private static ushort Level(float component)
	{
		double normalized = Math.Clamp((component + RotationRange) / (2 * RotationRange), 0, 1);
		return (ushort)Math.Round(normalized * RotationLevels);
	}

	private static float FromLevel(ushort level)
	{
		return (float)(((double)level / RotationLevels * 2 * RotationRange) - RotationRange);
	}
}
