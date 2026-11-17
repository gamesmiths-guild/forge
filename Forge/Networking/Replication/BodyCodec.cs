// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// Writes and reads body states, quantized to a session's <see cref="BodyPrecision"/>.
/// </summary>
public static class BodyCodec
{
	// A unit quaternion's three smallest components lie within this of zero.
	private const float RotationRange = 0.70710678f;

	/// <summary>
	/// Quantizes a body state.
	/// </summary>
	/// <param name="state">The state.</param>
	/// <param name="precision">The session's precision.</param>
	/// <returns>The state as it travels.</returns>
	public static QuantizedBody Quantize(in BodyState state, in BodyPrecision precision)
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
				levels[written++] = Level(components[i], precision.RotationLevels);
			}
		}

		return new QuantizedBody(
			Steps(state.Position.X, precision.PositionSteps),
			Steps(state.Position.Y, precision.PositionSteps),
			Steps(state.Position.Z, precision.PositionSteps),
			largest,
			levels[0],
			levels[1],
			levels[2],
			Steps(state.Velocity.X, precision.VelocitySteps),
			Steps(state.Velocity.Y, precision.VelocitySteps),
			Steps(state.Velocity.Z, precision.VelocitySteps),
			state.Grounded);
	}

	/// <summary>
	/// Rebuilds the body state a quantized one stands for.
	/// </summary>
	/// <param name="body">The quantized state.</param>
	/// <param name="precision">The session's precision.</param>
	/// <returns>The state, as the receiver sees it.</returns>
	public static BodyState Dequantize(in QuantizedBody body, in BodyPrecision precision)
	{
		int levels = precision.RotationLevels;

		// Whole steps in a unit, so a state rebuilt from its steps is the float nearest the decimal value it stands
		// for.
		double positionSteps = precision.PositionSteps;
		double velocitySteps = precision.VelocitySteps;
		Span<float> components = stackalloc float[4];
		Span<float> smallest = [FromLevel(body.A, levels), FromLevel(body.B, levels), FromLevel(body.C, levels)];
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
			new Vector3(
				(float)(body.X / positionSteps),
				(float)(body.Y / positionSteps),
				(float)(body.Z / positionSteps)),
			Quaternion.Normalize(new Quaternion(components[0], components[1], components[2], components[3])),
			new Vector3(
				(float)(body.VelocityX / velocitySteps),
				(float)(body.VelocityY / velocitySteps),
				(float)(body.VelocityZ / velocitySteps)),
			body.Grounded);
	}

	/// <summary>
	/// Writes a quantized body state.
	/// </summary>
	/// <param name="writer">The writer.</param>
	/// <param name="body">The state.</param>
	/// <param name="precision">The session's precision.</param>
	public static void Write(BitWriter writer, in QuantizedBody body, in BodyPrecision precision)
	{
		writer.WriteVarInt(body.X);
		writer.WriteVarInt(body.Y);
		writer.WriteVarInt(body.Z);
		writer.WriteBits(body.Largest, 2);
		writer.WriteBits(body.A, precision.RotationBits);
		writer.WriteBits(body.B, precision.RotationBits);
		writer.WriteBits(body.C, precision.RotationBits);
		writer.WriteVarInt(body.VelocityX);
		writer.WriteVarInt(body.VelocityY);
		writer.WriteVarInt(body.VelocityZ);
		writer.WriteBool(body.Grounded);
	}

	/// <summary>
	/// Reads a quantized body state.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="precision">The session's precision.</param>
	/// <returns>The state.</returns>
	public static QuantizedBody Read(BitReader reader, in BodyPrecision precision)
	{
		return new QuantizedBody(
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			(byte)reader.ReadBits(2),
			(ushort)reader.ReadBits(precision.RotationBits),
			(ushort)reader.ReadBits(precision.RotationBits),
			(ushort)reader.ReadBits(precision.RotationBits),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadVarInt(),
			reader.ReadBool());
	}

	// Whether two states lie within a step of each other: a body a restore put back can settle a step off at a contact,
	// which is no difference.
	internal static bool WithinAStep(in QuantizedBody first, in QuantizedBody second, in BodyPrecision precision)
	{
		const float RotationTolerance = 2e-5f;

		return Math.Abs(first.X - second.X) <= 1
			&& Math.Abs(first.Y - second.Y) <= 1
			&& Math.Abs(first.Z - second.Z) <= 1
			&& Math.Abs(first.VelocityX - second.VelocityX) <= 1
			&& Math.Abs(first.VelocityY - second.VelocityY) <= 1
			&& Math.Abs(first.VelocityZ - second.VelocityZ) <= 1
			&& first.Grounded == second.Grounded
			&& Math.Abs(
				Quaternion.Dot(Dequantize(first, precision).Rotation, Dequantize(second, precision).Rotation))
				>= 1 - RotationTolerance;
	}

	// A body as it is, for where an entity is made: the client that predicts it runs its setup there, as the server
	// did.
	internal static void WriteExact(BitWriter writer, in BodyState state)
	{
		writer.WriteFloat(state.Position.X);
		writer.WriteFloat(state.Position.Y);
		writer.WriteFloat(state.Position.Z);
		writer.WriteFloat(state.Rotation.X);
		writer.WriteFloat(state.Rotation.Y);
		writer.WriteFloat(state.Rotation.Z);
		writer.WriteFloat(state.Rotation.W);
		writer.WriteFloat(state.Velocity.X);
		writer.WriteFloat(state.Velocity.Y);
		writer.WriteFloat(state.Velocity.Z);
		writer.WriteBool(state.Grounded);
	}

	internal static BodyState ReadExact(BitReader reader)
	{
		return new BodyState(
			new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
			new Quaternion(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
			new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
			reader.ReadBool());
	}

	private static long Steps(float value, double stepsPerUnit)
	{
		return float.IsNaN(value) ? 0 : (long)Math.Round(value * stepsPerUnit);
	}

	private static ushort Level(float component, int levels)
	{
		double normalized = Math.Clamp((component + RotationRange) / (2 * RotationRange), 0, 1);
		return (ushort)Math.Round(normalized * levels);
	}

	private static float FromLevel(ushort level, int levels)
	{
		return (float)(((double)level / levels * 2 * RotationRange) - RotationRange);
	}
}
