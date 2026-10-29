// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// Runs an entity through a number of ticks, recording each in an <see cref="EntityHistory"/>, then rolls it back to
/// every recorded tick in turn and replays the rest. A rollback has to raise nothing and bring the tick's state back,
/// and every replayed tick has to raise the same events and leave the same state as the first time.
/// </summary>
internal static class EntityReplay
{
	/// <summary>
	/// Asserts that the entity rolled back to any tick replays the ticks after it exactly as they first ran.
	/// </summary>
	/// <param name="entity">The entity, set up and watched.</param>
	/// <param name="ticks">How many ticks to run, numbered from 1.</param>
	/// <param name="step">The fixed step.</param>
	/// <param name="submitInput">Submits the input for a tick before it runs the first time. A replay submits what the
	/// tick ran with instead.</param>
	/// <param name="describe">Describes the entity's state.</param>
	/// <param name="log">The log the entity's events are written to. It's cleared before each tick.</param>
	public static void AssertRepeats(
		IForgeEntity entity,
		int ticks,
		double step,
		Action<long> submitInput,
		Func<string> describe,
		List<string> log)
	{
		var history = new EntityHistory(entity, ticks + 1, step);
		string[] states = new string[ticks + 1];
		string[] messages = new string[ticks + 1];

		history.Record(0);
		states[0] = describe();

		for (int tick = 1; tick <= ticks; tick++)
		{
			log.Clear();
			submitInput(tick);
			EntitySimulation.Tick(entity, tick, step);
			history.Record(tick);
			messages[tick] = string.Join(", ", log);
			states[tick] = describe();
		}

		for (int from = 0; from < ticks; from++)
		{
			log.Clear();
			history.Rollback(from);

			log.Should().BeEmpty($"a rollback to tick {from} raises nothing");
			describe().Should().Be(states[from], $"a rollback to tick {from} brings that tick back");

			for (int tick = from + 1; tick <= ticks; tick++)
			{
				log.Clear();
				history.Replay(tick);

				string.Join(", ", log).Should().Be(
					messages[tick],
					$"tick {tick} raises the same events after a rollback to tick {from}");
				describe().Should().Be(
					states[tick],
					$"tick {tick} leaves the same state after a rollback to tick {from}");
			}
		}
	}

	/// <summary>
	/// Writes what the entity raises to the log: attribute and tag changes, and effect and ability events.
	/// </summary>
	/// <param name="entity">The entity to watch.</param>
	/// <param name="log">The log.</param>
	public static void Watch(IForgeEntity entity, List<string> log)
	{
		foreach (EntityAttribute attribute in entity.Attributes)
		{
			attribute.OnValueChanged += (changed, delta) => log.Add($"{changed.Key} {delta:+0;-0}");
		}

		entity.Tags.OnTagsChanged += tags => log.Add($"tags {Tags(tags)}");
		entity.EffectsManager.OnActiveEffectAdded += x => log.Add($"added {Name(x)}");
		entity.EffectsManager.OnActiveEffectChanged += x => log.Add($"changed {Name(x)}");
		entity.EffectsManager.OnActiveEffectRemoved += (x, reason) => log.Add($"removed {Name(x)} {reason}");
		entity.EffectsManager.OnEffectExecuted += x => log.Add($"executed {x.Effect.EffectData.Name}");
		entity.Abilities.OnAbilityGranted += _ => log.Add("granted");
		entity.Abilities.OnAbilityRemoved += _ => log.Add("ability removed");
		entity.Abilities.OnAbilityActivated += _ => log.Add("activated");
		entity.Abilities.OnAbilityEnded += x => log.Add($"ended {x.AbilityData.Name}");
	}

	private static string Name(ActiveEffectHandle handle)
	{
		return handle.Effect?.EffectData.Name ?? "gone";
	}

	private static string Tags(TagContainer tags)
	{
		return $"[{string.Join(", ", tags.Select(x => x.ToString()).OrderBy(x => x, StringComparer.Ordinal))}]";
	}
}
