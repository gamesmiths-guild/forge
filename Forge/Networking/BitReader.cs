// Copyright © Gamesmiths Guild.

using System.Text;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Reads values a <see cref="BitWriter"/> wrote, in the same order.
/// </summary>
/// <remarks>
/// Reading past the end of the data throws a <see cref="MalformedPacketException"/>, as does a value that can't be
/// what the writer wrote, such as a variable-width integer that never ends. A packet from the network is untrusted,
/// so a receiver catches that exception and drops the packet rather than trusting any value read before it.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="BitReader"/> class over the given bytes.
/// </remarks>
/// <param name="data">The bytes to read.</param>
public sealed class BitReader(ReadOnlyMemory<byte> data)
{
	private const int MaxVarIntBytes = 10;

	private ReadOnlyMemory<byte> _data = data;

	/// <summary>
	/// Gets how many bits have been read.
	/// </summary>
	public int BitPosition { get; private set; }

	/// <summary>
	/// Gets how many bits are left to read.
	/// </summary>
	public int BitsRemaining => (_data.Length << 3) - BitPosition;

	/// <summary>
	/// Starts over at the first bit of other bytes.
	/// </summary>
	/// <param name="data">The bytes to read.</param>
	public void Reset(ReadOnlyMemory<byte> data)
	{
		_data = data;
		BitPosition = 0;
	}

	/// <summary>
	/// Reads a value of a fixed width.
	/// </summary>
	/// <param name="bits">How many bits to read, from 0 to 64.</param>
	/// <returns>The value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bits"/> is outside 0 to 64.
	/// </exception>
	/// <exception cref="MalformedPacketException">Thrown when the data ends first.</exception>
	public ulong ReadBits(int bits)
	{
		if (bits is < 0 or > 64)
		{
			throw new ArgumentOutOfRangeException(nameof(bits), bits, "A value is read in 0 to 64 bits.");
		}

		if (bits > BitsRemaining)
		{
			throw new MalformedPacketException($"Tried to read {bits} bits with {BitsRemaining} left.");
		}

		ReadOnlySpan<byte> readData = _data.Span;
		ulong value = 0;
		int read = 0;

		while (read < bits)
		{
			int byteIndex = BitPosition >> 3;
			int bitInByte = BitPosition & 7;
			int take = Math.Min(8 - bitInByte, bits - read);
			ulong chunk = ((ulong)readData[byteIndex] >> bitInByte) & ((1UL << take) - 1);

			value |= chunk << read;
			BitPosition += take;
			read += take;
		}

		return value;
	}

	/// <summary>
	/// Reads a flag.
	/// </summary>
	/// <returns>The flag.</returns>
	public bool ReadBool()
	{
		return ReadBits(1) != 0;
	}

	/// <summary>
	/// Reads a byte.
	/// </summary>
	/// <returns>The byte.</returns>
	public byte ReadByte()
	{
		return (byte)ReadBits(8);
	}

	/// <summary>
	/// Reads a 16-bit unsigned integer.
	/// </summary>
	/// <returns>The value.</returns>
	public ushort ReadUInt16()
	{
		return (ushort)ReadBits(16);
	}

	/// <summary>
	/// Reads a 32-bit unsigned integer.
	/// </summary>
	/// <returns>The value.</returns>
	public uint ReadUInt32()
	{
		return (uint)ReadBits(32);
	}

	/// <summary>
	/// Reads a 64-bit unsigned integer.
	/// </summary>
	/// <returns>The value.</returns>
	public ulong ReadUInt64()
	{
		return ReadBits(64);
	}

	/// <summary>
	/// Reads an unsigned integer <see cref="BitWriter.WriteVarUInt"/> wrote.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the data ends first, or the value never ends.</exception>
	public ulong ReadVarUInt()
	{
		ulong value = 0;

		for (int i = 0; i < MaxVarIntBytes; i++)
		{
			ulong chunk = ReadBits(8);
			value |= (chunk & 0x7F) << (7 * i);

			if ((chunk & 0x80) == 0)
			{
				return value;
			}
		}

		throw new MalformedPacketException("A variable-width integer ran past 64 bits.");
	}

	/// <summary>
	/// Reads a signed integer <see cref="BitWriter.WriteVarInt"/> wrote.
	/// </summary>
	/// <returns>The value.</returns>
	public long ReadVarInt()
	{
		return UnZigZag(ReadVarUInt());
	}

	/// <summary>
	/// Reads a float <see cref="BitWriter.WriteFloat"/> wrote.
	/// </summary>
	/// <returns>The value.</returns>
	public float ReadFloat()
	{
		return BitConverter.Int32BitsToSingle(unchecked((int)ReadBits(32)));
	}

	/// <summary>
	/// Reads a float <see cref="BitWriter.WriteQuantized(float, float)"/> wrote with the same step.
	/// </summary>
	/// <param name="step">The precision it was written with.</param>
	/// <returns>The rounded value.</returns>
	public float ReadQuantized(float step)
	{
		return (float)(ReadVarInt() * (double)step);
	}

	/// <summary>
	/// Reads a float <see cref="BitWriter.WriteQuantized(float, float, float, int)"/> wrote with the same range and
	/// bits.
	/// </summary>
	/// <param name="min">The lowest value the range holds.</param>
	/// <param name="max">The highest value the range holds.</param>
	/// <param name="bits">How many bits it was written in, from 1 to 32.</param>
	/// <returns>The value, to the range's precision.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bits"/> is outside 1 to 32, or
	/// <paramref name="max"/> is not above <paramref name="min"/>.</exception>
	public float ReadQuantized(float min, float max, int bits)
	{
		if (bits is < 1 or > 32)
		{
			throw new ArgumentOutOfRangeException(nameof(bits), bits, "A ranged value is read in 1 to 32 bits.");
		}

		if (max <= min)
		{
			throw new ArgumentOutOfRangeException(nameof(max), max, "The range's maximum must be above its minimum.");
		}

		ulong levels = (1UL << bits) - 1;

		return (float)(min + (ReadBits(bits) / (double)levels * (max - min)));
	}

	/// <summary>
	/// Reads a string <see cref="BitWriter.WriteString"/> wrote.
	/// </summary>
	/// <param name="maxBytes">The longest string to accept, in bytes, so a bad length can't ask for a huge buffer.
	/// </param>
	/// <returns>The string, empty for one written as <see langword="null"/>.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the length is above <paramref name="maxBytes"/> or the
	/// data ends first.</exception>
	public string ReadString(int maxBytes = 4096)
	{
		ulong length = ReadVarUInt();

		if (length > (ulong)maxBytes)
		{
			throw new MalformedPacketException($"A string of {length} bytes is longer than the {maxBytes} allowed.");
		}

		if (length == 0)
		{
			return string.Empty;
		}

		byte[] bytes = new byte[length];
		ReadBytes(bytes);

		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// Reads raw bytes into a span, as many as it holds.
	/// </summary>
	/// <param name="destination">Where to put the bytes.</param>
	public void ReadBytes(Span<byte> destination)
	{
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = (byte)ReadBits(8);
		}
	}

	/// <summary>
	/// Moves to the next byte boundary, matching <see cref="BitWriter.Align"/>.
	/// </summary>
	/// <exception cref="MalformedPacketException">Thrown when the data ends first.</exception>
	public void Align()
	{
		int remainder = BitPosition & 7;

		if (remainder != 0)
		{
			ReadBits(8 - remainder);
		}
	}

	internal static long UnZigZag(ulong value)
	{
		return unchecked((long)(value >> 1) ^ -(long)(value & 1));
	}
}
