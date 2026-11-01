// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// A tick that <see cref="WorldHistory.Replay"/> ran differently from the way it was recorded.
/// </summary>
/// <remarks>
/// It keeps the tick's recorded and replayed snapshots, so <see cref="Describe"/> can tell apart every value that
/// differs whenever it's asked, long after the history has moved on.
/// </remarks>
public sealed class WorldDesync
{
	private readonly EntitySnapshot[] _recorded;

	private readonly EntitySnapshot[] _replayed;

	/// <summary>
	/// Gets the tick that ran differently.
	/// </summary>
	public long Tick { get; }

	internal WorldDesync(long tick, EntitySnapshot[] recorded, EntitySnapshot[] replayed)
	{
		Tick = tick;
		_recorded = recorded;
		_replayed = replayed;
	}

	/// <summary>
	/// Describes how the tick ran differently: for each entity whose state differs, every value that differs, as
	/// recorded and as replayed, and the entities only one of the two had.
	/// </summary>
	/// <remarks>
	/// Entities are compared by their place in the world, in the order they joined it, as the
	/// <see cref="WorldHistory.ChecksumAt">checksum</see> compares them. An entity only the replay had was released
	/// when the replay put the world back, so <paramref name="name"/> may be asked about an entity whose host object is
	/// gone.
	/// </remarks>
	/// <param name="name">Names an entity in the description. Without it, entities are named by their place in the
	/// world.</param>
	/// <returns>The description, one line per entity and one per value that differs.</returns>
	public string Describe(Func<IForgeEntity, string>? name = null)
	{
		var lines = new List<string>();
		IForgeEntity[] recordedWorld = [.. _recorded.Select(x => x.Entity)];
		IForgeEntity[] replayedWorld = [.. _replayed.Select(x => x.Entity)];

		string Name(int index, IForgeEntity entity)
		{
			return name is null ? $"Entity {index}" : $"{name(entity)} (entity {index})";
		}

		for (int i = 0; i < Math.Max(_recorded.Length, _replayed.Length); i++)
		{
			if (i >= _replayed.Length)
			{
				lines.Add($"{Name(i, _recorded[i].Entity)}: only in the recording");
				continue;
			}

			if (i >= _recorded.Length)
			{
				lines.Add($"{Name(i, _replayed[i].Entity)}: only in the replay");
				continue;
			}

			Dictionary<string, string> recorded = Values(_recorded[i], recordedWorld);
			Dictionary<string, string> replayed = Values(_replayed[i], replayedWorld);
			IEnumerable<string> differences = recorded.Keys
				.Concat(replayed.Keys.Where(x => !recorded.ContainsKey(x)))
				.Where(x => !recorded.TryGetValue(x, out string? before)
					|| !replayed.TryGetValue(x, out string? after)
					|| before != after)
				.Select(x => $"  {x}: {Value(recorded, x)} recorded, {Value(replayed, x)} replayed");

			int count = lines.Count;
			lines.AddRange(differences);

			if (lines.Count > count)
			{
				lines.Insert(count, $"{Name(i, _recorded[i].Entity)}:");
			}
		}

		return string.Join(Environment.NewLine, lines);
	}

	private static Dictionary<string, string> Values(EntitySnapshot snapshot, IForgeEntity[] world)
	{
		var description = new StateDescription(world);
		snapshot.Write(description);
		return description.Values;
	}

	private static string Value(Dictionary<string, string> values, string path)
	{
		return values.TryGetValue(path, out string? value) ? value : "missing";
	}
}
