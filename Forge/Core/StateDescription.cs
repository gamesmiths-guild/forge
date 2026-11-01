// Copyright © Gamesmiths Guild.

using System.Globalization;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

// Lists a snapshot's values by path, in the order they were written, for two snapshots to be compared value by value.
internal sealed class StateDescription(IReadOnlyList<IForgeEntity> world) : IStateWriter
{
	private readonly List<string> _path = [];

	public Dictionary<string, string> Values { get; } = [];

	public void Open(string name)
	{
		_path.Add(name);
	}

	public void Open(string name, int index)
	{
		_path.Add($"{name}[{index}]");
	}

	public void OpenSet(string name)
	{
		_path.Add(name);
	}

	public void Close()
	{
		_path.RemoveAt(_path.Count - 1);
	}

	public void Write(string name, long value)
	{
		Values[Path(name)] = value.ToString(CultureInfo.InvariantCulture);
	}

	public void Write(string name, double value)
	{
		Values[Path(name)] = value.ToString("R", CultureInfo.InvariantCulture);
	}

	public void Write(string name, string? value)
	{
		Values[Path(name)] = value ?? "null";
	}

	public void Write(string name, in Variant128 value)
	{
		Values[Path(name)] = $"0x{value.AsUpperULong():x16}{value.AsULong():x16}";
	}

	public void WriteEntity(string name, IForgeEntity? entity)
	{
		string value = entity is null ? "none" : "an entity outside the world";

		for (int i = 0; entity is not null && i < world.Count; i++)
		{
			if (world[i] == entity)
			{
				value = $"entity {i}";
				break;
			}
		}

		Values[Path(name)] = value;
	}

	private string Path(string name)
	{
		return _path.Count == 0 ? name : $"{string.Join("/", _path)}/{name}";
	}
}
