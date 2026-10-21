// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// A seedable <see cref="IRandom"/> whose numbers depend only on its seed and on the tick it was last started for, so
/// two machines simulating the same entity on the same tick draw the same numbers in the same order.
/// </summary>
/// <remarks>
/// <para>The generator is xoshiro256**, seeded through SplitMix64. <see cref="BeginTick"/> restarts the sequence from a
/// state derived from <see cref="Seed"/> and the tick, so the draws of one tick never depend on how many numbers
/// earlier ticks used. That is what lets a simulation replay a tick and get the same rolls, and keeps a roll that one
/// machine made and another did not from shifting every roll after it.</para>
/// <para>All arithmetic is integer: the results are identical on every platform.</para>
/// </remarks>
public sealed class DeterministicRandom : IRandom
{
	private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

	private ulong _s0;
	private ulong _s1;
	private ulong _s2;
	private ulong _s3;

	/// <summary>
	/// Gets the seed every tick's sequence is derived from.
	/// </summary>
	public ulong Seed { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DeterministicRandom"/> class.
	/// </summary>
	/// <param name="seed">The seed every tick's sequence is derived from.</param>
	public DeterministicRandom(ulong seed)
	{
		Seed = seed;
		SetState(seed);
	}

	/// <summary>
	/// Restarts the sequence for the given tick. Every draw until the next call comes from a sequence that depends only
	/// on <see cref="Seed"/> and <paramref name="tick"/>.
	/// </summary>
	/// <param name="tick">The tick about to be simulated.</param>
	public void BeginTick(long tick)
	{
		SetState(unchecked(Seed + ((ulong)tick * GoldenGamma)));
	}

	/// <inheritdoc/>
	public int NextInt()
	{
		return (int)(NextUInt64() >> 33);
	}

	/// <inheritdoc/>
	public int NextInt(int maxValue)
	{
		return maxValue <= 0 ? 0 : (int)NextBounded((ulong)maxValue);
	}

	/// <inheritdoc/>
	public int NextInt(int minValue, int maxValue)
	{
		if (minValue >= maxValue)
		{
			return minValue;
		}

		return (int)(minValue + (long)NextBounded((ulong)((long)maxValue - minValue)));
	}

	/// <inheritdoc/>
	public int NextIntInclusive(int minValue, int maxValue)
	{
		if (minValue >= maxValue)
		{
			return minValue;
		}

		return (int)(minValue + (long)NextBounded((ulong)((long)maxValue - minValue) + 1UL));
	}

	/// <inheritdoc/>
	public float NextSingle()
	{
		return (NextUInt64() >> 40) * (1.0f / (1 << 24));
	}

	/// <inheritdoc/>
	public float NextSingleInclusive()
	{
		return NextBounded((1UL << 24) + 1UL) * (1.0f / (1 << 24));
	}

	/// <inheritdoc/>
	public double NextDouble()
	{
		return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
	}

	/// <inheritdoc/>
	public double NextDoubleInclusive()
	{
		return NextBounded((1UL << 53) + 1UL) * (1.0 / (1UL << 53));
	}

	/// <inheritdoc/>
	public long NextInt64()
	{
		return (long)(NextUInt64() >> 1);
	}

	/// <inheritdoc/>
	public long NextInt64(long maxValue)
	{
		return maxValue <= 0 ? 0 : (long)NextBounded((ulong)maxValue);
	}

	/// <inheritdoc/>
	public long NextInt64(long minValue, long maxValue)
	{
		if (minValue >= maxValue)
		{
			return minValue;
		}

		return unchecked(minValue + (long)NextBounded((ulong)maxValue - (ulong)minValue));
	}

	/// <inheritdoc/>
	public long NextInt64Inclusive(long minValue, long maxValue)
	{
		if (minValue >= maxValue)
		{
			return minValue;
		}

		ulong range = unchecked((ulong)maxValue - (ulong)minValue + 1UL);

		// A range spanning every long wraps to zero: any 64-bit value is then in range.
		return range == 0 ? (long)NextUInt64() : unchecked(minValue + (long)NextBounded(range));
	}

	/// <inheritdoc/>
	public void NextBytes(byte[] buffer)
	{
		NextBytes(buffer.AsSpan());
	}

	/// <inheritdoc/>
	public void NextBytes(Span<byte> buffer)
	{
		int i = 0;

		while (i < buffer.Length)
		{
			ulong value = NextUInt64();

			// Written byte by byte, low byte first, so the result does not depend on the machine's endianness.
			for (int b = 0; b < 8 && i < buffer.Length; b++, i++)
			{
				buffer[i] = (byte)(value >> (8 * b));
			}
		}
	}

	private static ulong SplitMix64(ref ulong state)
	{
		ulong z = state = unchecked(state + GoldenGamma);
		z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
		z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
		return z ^ (z >> 31);
	}

	private static ulong RotateLeft(ulong value, int offset)
	{
		return (value << offset) | (value >> (64 - offset));
	}

	private void SetState(ulong seed)
	{
		ulong state = seed;
		_s0 = SplitMix64(ref state);
		_s1 = SplitMix64(ref state);
		_s2 = SplitMix64(ref state);
		_s3 = SplitMix64(ref state);
	}

	private ulong NextUInt64()
	{
		ulong result = unchecked(RotateLeft(_s1 * 5, 7) * 9);
		ulong shifted = _s1 << 17;

		_s2 ^= _s0;
		_s3 ^= _s1;
		_s1 ^= _s2;
		_s0 ^= _s3;
		_s2 ^= shifted;
		_s3 = RotateLeft(_s3, 45);

		return result;
	}

	// Uniform in [0, range) without modulo bias: values below the threshold would make the low results more likely, so
	// they are drawn again. At most half of all values are ever rejected.
	private ulong NextBounded(ulong range)
	{
		ulong threshold = unchecked(0UL - range) % range;

		while (true)
		{
			ulong value = NextUInt64();

			if (value >= threshold)
			{
				return value % range;
			}
		}
	}
}
