// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

// Hashes snapshots into 64 bits that come out the same on every machine and in every run: numbers bit for bit, strings
// by their characters rather than the runtime's randomized string hash, entities by their network id or else their
// place in the world, and the values of a set in any order.
internal sealed class StateChecksum(IReadOnlyList<IForgeEntity> world) : IStateWriter
{
	private const ulong Offset = 0xCBF29CE484222325;

	private const ulong Prime = 0x100000001B3;

	private const ulong NetworkedEntity = 0x4E45545F454E5449;

	private readonly List<Frame> _frames = [new Frame(Offset, IsSet: false, 0, 0)];

	public ulong Value => Avalanche(_frames[0].Hash);

	public void Open(string name)
	{
		_frames.Add(new Frame(HashOf(name), IsSet: false, 0, 0));
	}

	public void Open(string name, int index)
	{
		_frames.Add(new Frame(Mix(HashOf(name), (ulong)index), IsSet: false, 0, 0));
	}

	public void OpenSet(string name)
	{
		_frames.Add(new Frame(HashOf(name), IsSet: true, 0, 0));
	}

	public void Close()
	{
		Frame frame = _frames[^1];
		_frames.RemoveAt(_frames.Count - 1);

		// The scope's name is already in its hash, so it's the key it goes into a set by.
		ulong hash = frame.IsSet ? Mix(Mix(frame.Hash, frame.Sum), (ulong)frame.Count) : frame.Hash;
		AddHash(hash, hash);
	}

	public void Write(string name, long value)
	{
		Add(name, unchecked((ulong)value));
	}

	public void Write(string name, double value)
	{
		Add(name, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
	}

	public void Write(string name, string? value)
	{
		Add(name, value is null ? 0 : HashOf(value));
	}

	public void Write(string name, in Variant128 value)
	{
		Add(name, Mix(Mix(Offset, value.AsULong()), value.AsUpperULong()));
	}

	public void WriteEntity(string name, IForgeEntity? entity)
	{
		if (entity is null)
		{
			Add(name, 0);
			return;
		}

		// A networked entity counts by its id, the same on every machine; one without counts by its place in the world,
		// which every machine that built the same world agrees on, or as outside it.
		if (entity.NetworkId.IsValid)
		{
			Add(name, Mix(NetworkedEntity, entity.NetworkId.Value));
			return;
		}

		for (int i = 0; i < world.Count; i++)
		{
			if (world[i] == entity)
			{
				Add(name, (ulong)i + 2);
				return;
			}
		}

		Add(name, 1);
	}

	private static ulong Mix(ulong hash, ulong value)
	{
		return unchecked((hash ^ value) * Prime);
	}

	private static ulong HashOf(string text)
	{
		ulong hash = Offset;

		foreach (char character in text)
		{
			hash = Mix(hash, character);
		}

		return Mix(hash, (ulong)text.Length);
	}

	// Spreads every input bit over the whole hash, so sums of element hashes don't cancel out.
	private static ulong Avalanche(ulong hash)
	{
		unchecked
		{
			hash = (hash ^ (hash >> 30)) * 0xBF58476D1CE4E5B9UL;
			hash = (hash ^ (hash >> 27)) * 0x94D049BB133111EBUL;
			return hash ^ (hash >> 31);
		}
	}

	// A value in an ordered scope mixes into the scope's hash; in a set, it adds its own hash, named, to the set's sum.
	private void Add(string name, ulong value)
	{
		AddHash(Mix(HashOf(name), value), value);
	}

	private void AddHash(ulong keyed, ulong value)
	{
		Frame frame = _frames[^1];

		if (frame.IsSet)
		{
			frame = frame with { Sum = unchecked(frame.Sum + Avalanche(keyed)), Count = frame.Count + 1 };
		}
		else
		{
			frame = frame with { Hash = Mix(frame.Hash, Avalanche(value)) };
		}

		_frames[^1] = frame;
	}

	private readonly record struct Frame(ulong Hash, bool IsSet, ulong Sum, int Count);
}
