// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Default <see cref="IRandom"/> implementation backed by <see cref="Random"/>.
/// </summary>
/// <remarks>
/// Convenient for single-player and tooling scenarios. For deterministic behavior (replays, networking), inject a
/// seeded instance or a custom <see cref="IRandom"/> instead.
/// </remarks>
public sealed class SystemRandom : IRandom
{
	private readonly Random _random;

	/// <summary>
	/// Initializes a new instance of the <see cref="SystemRandom"/> class with a time-based seed.
	/// </summary>
	public SystemRandom()
	{
		_random = new Random();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SystemRandom"/> class with the given seed.
	/// </summary>
	/// <param name="seed">The seed for the random sequence.</param>
	public SystemRandom(int seed)
	{
		_random = new Random(seed);
	}

	/// <inheritdoc/>
	public void NextBytes(byte[] buffer)
	{
		_random.NextBytes(buffer);
	}

	/// <inheritdoc/>
	public void NextBytes(Span<byte> buffer)
	{
		_random.NextBytes(buffer);
	}

	/// <inheritdoc/>
	public double NextDouble()
	{
		return _random.NextDouble();
	}

	/// <inheritdoc/>
	public double NextDoubleInclusive()
	{
		Span<byte> buffer = stackalloc byte[8];
		_random.NextBytes(buffer);
		ulong rand = BitConverter.ToUInt64(buffer);
		ulong value = (rand >> 10) % ((1UL << 53) + 1UL);
		return (double)value / (1UL << 53);
	}

	/// <inheritdoc/>
	public int NextInt()
	{
		return _random.Next();
	}

	/// <inheritdoc/>
	public int NextInt(int maxValue)
	{
		return maxValue <= 0 ? 0 : _random.Next(maxValue);
	}

	/// <inheritdoc/>
	public int NextInt(int minValue, int maxValue)
	{
		return minValue >= maxValue ? minValue : _random.Next(minValue, maxValue);
	}

	/// <inheritdoc/>
	public int NextIntInclusive(int minValue, int maxValue)
	{
		if (minValue > maxValue)
		{
			return minValue;
		}

		if (maxValue == int.MaxValue)
		{
			return (int)NextInt64Inclusive(minValue, maxValue);
		}

		return _random.Next(minValue, maxValue + 1);
	}

	/// <inheritdoc/>
	public long NextInt64()
	{
		Span<byte> buffer = stackalloc byte[8];
		_random.NextBytes(buffer);
		return (long)(BitConverter.ToUInt64(buffer) & long.MaxValue);
	}

	/// <inheritdoc/>
	public long NextInt64(long maxValue)
	{
		return maxValue <= 0 ? 0 : NextInt64(0, maxValue);
	}

	/// <inheritdoc/>
	public long NextInt64(long minValue, long maxValue)
	{
		if (minValue >= maxValue)
		{
			return minValue;
		}

		ulong range = (ulong)(maxValue - minValue);
		ulong rand = (ulong)NextInt64();

		return (long)(rand % range) + minValue;
	}

	/// <inheritdoc/>
	public long NextInt64Inclusive(long minValue, long maxValue)
	{
		if (minValue > maxValue)
		{
			return minValue;
		}

		if (minValue == maxValue)
		{
			return minValue;
		}

		if (maxValue == long.MaxValue)
		{
			ulong inclusiveRange = (ulong)(maxValue - minValue) + 1UL;
			ulong rand = (ulong)NextInt64();
			return (long)(rand % inclusiveRange) + minValue;
		}

		return NextInt64(minValue, maxValue + 1);
	}

	/// <inheritdoc/>
	public float NextSingle()
	{
		float value;
		do
		{
			value = (float)_random.NextDouble();
		}
		while (value >= 1.0f);

		return value;
	}

	/// <inheritdoc/>
	public float NextSingleInclusive()
	{
		return (float)NextDoubleInclusive();
	}
}
