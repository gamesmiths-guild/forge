// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Writes and reads input frames and commands, each against the one before it, so a tick in which nothing changed
/// costs a bit and a tick in which only the aim moved costs the aim's movement.
/// </summary>
/// <remarks>
/// A frame read back is bit for bit the frame written, because what travels is the frame's own quantized steps and
/// the reader rebuilds the frame through <see cref="InputFrame.WithVector"/> and <see cref="InputFrame.WithAim"/>,
/// which quantize the same way. The machine that collected the input and the machine that receives it therefore
/// simulate the same frame.
/// </remarks>
public static class InputCodec
{
	/// <summary>
	/// The most events one command carries. More than this in one tick is a malformed command.
	/// </summary>
	public const int MaxEventsPerCommand = 16;

	private const int VectorSteps = 127;

	private const double AimPointSteps = 1000;

	private const double AimDirectionSteps = 32767;

	/// <summary>
	/// Writes a frame as the difference from the previous one.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	/// <param name="frame">The frame.</param>
	/// <param name="previous">The frame written before it, or <see langword="default"/> for the first.</param>
	/// <param name="actions">The action set, for how many vectors a frame carries.</param>
	public static void WriteFrame(BitWriter writer, in InputFrame frame, in InputFrame previous, InputActionSet actions)
	{
		if (frame == previous)
		{
			writer.WriteBool(true);
			return;
		}

		writer.WriteBool(false);
		writer.WriteVarUInt(frame.Held);

		bool hasEdges = frame.Pressed != 0 || frame.Released != 0;
		writer.WriteBool(hasEdges);

		if (hasEdges)
		{
			writer.WriteVarUInt(frame.Pressed);
			writer.WriteVarUInt(frame.Released);
		}

		for (int i = 0; i < actions.Vectors.Count; i++)
		{
			Vector2 vector = frame.GetVector(i);
			bool changed = vector != previous.GetVector(i);
			writer.WriteBool(changed);

			if (changed)
			{
				writer.WriteBits((byte)VectorStep(vector.X), 8);
				writer.WriteBits((byte)VectorStep(vector.Y), 8);
			}
		}

		writer.WriteBool(frame.HasAim);

		if (!frame.HasAim)
		{
			return;
		}

		bool sameAim = previous.HasAim
			&& frame.AimPoint == previous.AimPoint
			&& frame.AimDirection == previous.AimDirection;
		writer.WriteBool(sameAim);

		if (sameAim)
		{
			return;
		}

		// Against the previous aim when there was one, so a mouse that moved a little costs a little.
		Vector3 basePoint = previous.HasAim ? previous.AimPoint : Vector3.Zero;
		Vector3 baseDirection = previous.HasAim ? previous.AimDirection : Vector3.Zero;

		WriteSteps(writer, frame.AimPoint, basePoint, AimPointSteps);
		WriteSteps(writer, frame.AimDirection, baseDirection, AimDirectionSteps);
	}

	/// <summary>
	/// Reads a frame <see cref="WriteFrame"/> wrote against the same previous frame.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <param name="previous">The frame read before it, or <see langword="default"/> for the first.</param>
	/// <param name="actions">The action set, for how many vectors a frame carries.</param>
	/// <returns>The frame.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the data ends first, or the aim moved further than any
	/// aim can.</exception>
	public static InputFrame ReadFrame(BitReader reader, in InputFrame previous, InputActionSet actions)
	{
		if (reader.ReadBool())
		{
			return previous;
		}

		var frame = new InputFrame { Held = reader.ReadVarUInt() };

		if (reader.ReadBool())
		{
			frame = frame with { Pressed = reader.ReadVarUInt(), Released = reader.ReadVarUInt() };
		}

		for (int i = 0; i < actions.Vectors.Count; i++)
		{
			Vector2 vector = reader.ReadBool()
				? new Vector2(
					(sbyte)reader.ReadBits(8) / (float)VectorSteps,
					(sbyte)reader.ReadBits(8) / (float)VectorSteps)
				: previous.GetVector(i);

			frame = frame.WithVector(i, vector);
		}

		if (!reader.ReadBool())
		{
			return frame;
		}

		if (reader.ReadBool())
		{
			return frame.WithAim(previous.AimPoint, previous.AimDirection);
		}

		Vector3 basePoint = previous.HasAim ? previous.AimPoint : Vector3.Zero;
		Vector3 baseDirection = previous.HasAim ? previous.AimDirection : Vector3.Zero;
		Vector3 point = ReadSteps(reader, basePoint, AimPointSteps);
		Vector3 direction = ReadSteps(reader, baseDirection, AimDirectionSteps);

		return frame.WithAim(point, direction);
	}

	/// <summary>
	/// Writes a command's events: how many, then each tag index.
	/// </summary>
	/// <param name="writer">Where to write.</param>
	/// <param name="events">The tag indices.</param>
	/// <exception cref="ArgumentException">Thrown when there are more than <see cref="MaxEventsPerCommand"/>.
	/// </exception>
	public static void WriteEvents(BitWriter writer, ushort[] events)
	{
		if (events.Length > MaxEventsPerCommand)
		{
			throw new ArgumentException($"A command carries at most {MaxEventsPerCommand} events.", nameof(events));
		}

		writer.WriteBits((ulong)events.Length, 5);

		foreach (ushort tagIndex in events)
		{
			writer.WriteVarUInt(tagIndex);
		}
	}

	/// <summary>
	/// Reads a command's events.
	/// </summary>
	/// <param name="reader">Where to read.</param>
	/// <returns>The tag indices.</returns>
	/// <exception cref="MalformedPacketException">Thrown when there are more than <see cref="MaxEventsPerCommand"/>,
	/// an index doesn't fit a tag index, or the data ends first.</exception>
	public static ushort[] ReadEvents(BitReader reader)
	{
		int count = (int)reader.ReadBits(5);

		if (count > MaxEventsPerCommand)
		{
			throw new MalformedPacketException($"A command carries at most {MaxEventsPerCommand} events, not {count}.");
		}

		if (count == 0)
		{
			return [];
		}

		ushort[] events = new ushort[count];

		for (int i = 0; i < count; i++)
		{
			ulong index = reader.ReadVarUInt();

			if (index > ushort.MaxValue)
			{
				throw new MalformedPacketException($"{index} is not a tag index.");
			}

			events[i] = (ushort)index;
		}

		return events;
	}

	private static sbyte VectorStep(float component)
	{
		return (sbyte)Math.Round(component * VectorSteps);
	}

	private static void WriteSteps(BitWriter writer, Vector3 value, Vector3 baseline, double steps)
	{
		writer.WriteVarInt(Step(value.X, steps) - Step(baseline.X, steps));
		writer.WriteVarInt(Step(value.Y, steps) - Step(baseline.Y, steps));
		writer.WriteVarInt(Step(value.Z, steps) - Step(baseline.Z, steps));
	}

	private static Vector3 ReadSteps(BitReader reader, Vector3 baseline, double steps)
	{
		return new Vector3(
			(float)((Step(baseline.X, steps) + ReadDelta(reader, steps)) / steps),
			(float)((Step(baseline.Y, steps) + ReadDelta(reader, steps)) / steps),
			(float)((Step(baseline.Z, steps) + ReadDelta(reader, steps)) / steps));
	}

	// Two aim components, each clamped by the frame, are at most twice the clamp apart.
	private static long ReadDelta(BitReader reader, double steps)
	{
		long delta = reader.ReadVarInt();
		double max = 2 * InputFrame.MaxAimComponent * steps;

		if (delta < -max || delta > max)
		{
			throw new MalformedPacketException($"An aim moved by {delta} steps, more than any aim can.");
		}

		return delta;
	}

	// A frame's aim is already on the step grid, so this is exact.
	private static long Step(float value, double steps)
	{
		return (long)Math.Round(value * steps);
	}
}
