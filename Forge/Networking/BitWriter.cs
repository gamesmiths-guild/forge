// Copyright © Gamesmiths Guild.

using System.Text;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Writes values into a packet bit by bit, so a flag costs one bit and a small number only the bits it needs.
/// </summary>
/// <remarks>
/// <para>Values are written least significant bit first into a growing byte buffer. Every write has a matching read
/// on <see cref="BitReader"/>, and reading must follow the same sequence of calls as writing, since nothing in the
/// stream says what type comes next.</para>
/// <para>Numbers come in three shapes. A fixed width (<see cref="WriteBits"/>) costs exactly the bits asked for and
/// suits values with a known range. A variable width (<see cref="WriteVarUInt"/>, <see cref="WriteVarInt"/>) costs 8
/// bits per 7 bits of magnitude, so a small value costs one byte and a large one grows, which suits counts, indices
/// and deltas. A quantized float (<see cref="WriteQuantized(float, float)"/>) is rounded to a step and sent as a
/// variable-width integer, so a position in thousandths costs about as much as its magnitude in thousandths needs.
/// </para>
/// <para>A writer is reused across packets with <see cref="Clear"/>, so a steady stream of packets allocates nothing
/// once the buffer has grown to the largest packet.</para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="BitWriter"/> class.
/// </remarks>
/// <param name="capacity">How many bytes to reserve. The buffer grows past it when needed.</param>
public sealed class BitWriter(int capacity = BitWriter.DefaultCapacity)
{
	private const int DefaultCapacity = 256;

	// The largest double below 2^63, so a clamped step count always fits a long.
	private const double MaxSteps = long.MaxValue - 1023;

	private byte[] _buffer = new byte[Math.Max(capacity, 1)];

	/// <summary>
	/// Gets how many bits have been written.
	/// </summary>
	public int BitLength { get; private set; }

	/// <summary>
	/// Gets how many bytes the written bits take, counting a partly filled last byte.
	/// </summary>
	public int ByteLength => (BitLength + 7) >> 3;

	/// <summary>
	/// Gets the bytes written so far. The last byte's unwritten bits are zero.
	/// </summary>
	public ReadOnlySpan<byte> WrittenBytes => _buffer.AsSpan(0, ByteLength);

	/// <summary>
	/// Rounds a value to a step, the way <see cref="WriteQuantized(float, float)"/> sends it, so a simulation can use
	/// exactly what the other side receives.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="step">The precision.</param>
	/// <returns>The rounded value.</returns>
	public static float Quantize(float value, float step)
	{
		return (float)(Steps(value, step) * (double)step);
	}

	/// <summary>
	/// How many bits <see cref="WriteBits"/> needs for every value from 0 to <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="maxValue">The highest value.</param>
	/// <returns>The bit count, at least 1.</returns>
	public static int BitsFor(ulong maxValue)
	{
		int bits = 1;

		while (bits < 64 && (maxValue >> bits) != 0)
		{
			bits++;
		}

		return bits;
	}

	/// <summary>
	/// Starts over at the first bit, keeping the buffer.
	/// </summary>
	public void Clear()
	{
		Array.Clear(_buffer, 0, ByteLength);
		BitLength = 0;
	}

	/// <summary>
	/// Copies the written bytes into a new array.
	/// </summary>
	/// <returns>The written bytes.</returns>
	public byte[] ToArray()
	{
		return WrittenBytes.ToArray();
	}

	/// <summary>
	/// Writes the low <paramref name="bits"/> bits of a value.
	/// </summary>
	/// <param name="value">The value. Bits above <paramref name="bits"/> are ignored.</param>
	/// <param name="bits">How many bits to write, from 0 to 64.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bits"/> is outside 0 to 64.
	/// </exception>
	public void WriteBits(ulong value, int bits)
	{
		if (bits is < 0 or > 64)
		{
			throw new ArgumentOutOfRangeException(nameof(bits), bits, "A value is written in 0 to 64 bits.");
		}

		EnsureBits(bits);

		int written = 0;

		while (written < bits)
		{
			int byteIndex = BitLength >> 3;
			int bitInByte = BitLength & 7;
			int take = Math.Min(8 - bitInByte, bits - written);
			ulong chunk = (value >> written) & ((1UL << take) - 1);

			_buffer[byteIndex] |= (byte)(chunk << bitInByte);
			BitLength += take;
			written += take;
		}
	}

	/// <summary>
	/// Writes a flag as one bit.
	/// </summary>
	/// <param name="value">The flag.</param>
	public void WriteBool(bool value)
	{
		WriteBits(value ? 1UL : 0UL, 1);
	}

	/// <summary>
	/// Writes a byte.
	/// </summary>
	/// <param name="value">The byte.</param>
	public void WriteByte(byte value)
	{
		WriteBits(value, 8);
	}

	/// <summary>
	/// Writes a 16-bit unsigned integer.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteUInt16(ushort value)
	{
		WriteBits(value, 16);
	}

	/// <summary>
	/// Writes a 32-bit unsigned integer.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteUInt32(uint value)
	{
		WriteBits(value, 32);
	}

	/// <summary>
	/// Writes a 64-bit unsigned integer.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteUInt64(ulong value)
	{
		WriteBits(value, 64);
	}

	/// <summary>
	/// Writes an unsigned integer in as many bytes as its magnitude needs: 7 bits of value per byte, with the high
	/// bit saying whether another byte follows.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteVarUInt(ulong value)
	{
		while (value >= 0x80)
		{
			WriteBits((value & 0x7F) | 0x80, 8);
			value >>= 7;
		}

		WriteBits(value, 8);
	}

	/// <summary>
	/// Writes a signed integer in as many bytes as its magnitude needs, so small negative numbers cost as little as
	/// small positive ones.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteVarInt(long value)
	{
		WriteVarUInt(ZigZag(value));
	}

	/// <summary>
	/// Writes a float bit for bit, in 32 bits.
	/// </summary>
	/// <param name="value">The value.</param>
	public void WriteFloat(float value)
	{
		WriteBits(unchecked((uint)BitConverter.SingleToInt32Bits(value)), 32);
	}

	/// <summary>
	/// Writes a float rounded to a step, as the signed number of steps in as many bytes as it needs.
	/// </summary>
	/// <remarks>
	/// Quantizing before sending is what lets the sender simulate exactly what the receiver gets: round the value
	/// with <see cref="Quantize(float, float)"/> and use the rounded value on both sides.
	/// </remarks>
	/// <param name="value">The value.</param>
	/// <param name="step">The precision, such as 0.001 for thousandths.</param>
	public void WriteQuantized(float value, float step)
	{
		WriteVarInt(Steps(value, step));
	}

	/// <summary>
	/// Writes a float known to lie in a range, rounded to the nearest of the <c>2^bits</c> values the range is split
	/// into, in exactly <paramref name="bits"/> bits.
	/// </summary>
	/// <param name="value">The value. One outside the range is clamped to it.</param>
	/// <param name="min">The lowest value the range holds.</param>
	/// <param name="max">The highest value the range holds.</param>
	/// <param name="bits">How many bits to spend, from 1 to 32.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bits"/> is outside 1 to 32, or
	/// <paramref name="max"/> is not above <paramref name="min"/>.</exception>
	public void WriteQuantized(float value, float min, float max, int bits)
	{
		if (bits is < 1 or > 32)
		{
			throw new ArgumentOutOfRangeException(nameof(bits), bits, "A ranged value is written in 1 to 32 bits.");
		}

		if (max <= min)
		{
			throw new ArgumentOutOfRangeException(nameof(max), max, "The range's maximum must be above its minimum.");
		}

		if (float.IsNaN(value))
		{
			// NaN rounds to an integer differently on each platform, so it's written as the minimum.
			WriteBits(0, bits);
			return;
		}

		ulong levels = (1UL << bits) - 1;
		double normalized = Math.Clamp(((double)value - min) / ((double)max - min), 0, 1);

		WriteBits((ulong)Math.Round(normalized * levels), bits);
	}

	/// <summary>
	/// Writes a string as UTF-8, its length first.
	/// </summary>
	/// <param name="value">The string. <see langword="null"/> is written as empty.</param>
	public void WriteString(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			WriteVarUInt(0);
			return;
		}

		byte[] bytes = Encoding.UTF8.GetBytes(value);
		WriteVarUInt((ulong)bytes.Length);
		WriteBytes(bytes);
	}

	/// <summary>
	/// Writes raw bytes, without their length.
	/// </summary>
	/// <param name="bytes">The bytes.</param>
	public void WriteBytes(ReadOnlySpan<byte> bytes)
	{
		foreach (byte value in bytes)
		{
			WriteBits(value, 8);
		}
	}

	/// <summary>
	/// Moves to the next byte boundary, so what follows starts on a whole byte. Does nothing when already on one.
	/// </summary>
	public void Align()
	{
		int remainder = BitLength & 7;

		if (remainder != 0)
		{
			WriteBits(0, 8 - remainder);
		}
	}

	internal static ulong ZigZag(long value)
	{
		return unchecked((ulong)((value << 1) ^ (value >> 63)));
	}

	private static long Steps(float value, float step)
	{
		if (step <= 0 || float.IsNaN(step))
		{
			throw new ArgumentOutOfRangeException(nameof(step), step, "A quantization step is positive.");
		}

		// NaN, and a step count outside the long range, convert to an integer differently on each platform, so NaN is
		// sent as zero and the count is clamped first.
		return float.IsNaN(value) ? 0 : (long)Math.Clamp(Math.Round(value / (double)step), -MaxSteps, MaxSteps);
	}

	private void EnsureBits(int bits)
	{
		int needed = (BitLength + bits + 7) >> 3;

		if (needed <= _buffer.Length)
		{
			return;
		}

		int size = _buffer.Length;

		while (size < needed)
		{
			size *= 2;
		}

		Array.Resize(ref _buffer, size);
	}
}
