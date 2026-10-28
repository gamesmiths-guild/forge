// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Globalization;
using System.Reflection;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// Describes what an entity snapshot covers, by value, so two runs that hold equivalent objects compare equal: an
/// effect applied again on a replay is a new object with the same name, stacks and timing.
/// </summary>
internal static class EntityState
{
	/// <summary>
	/// Describes the entity: attributes, tags, active effects, granted abilities, input and body, and the running
	/// graphs among the given ones.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <param name="graphs">Graphs the entity's abilities may be running, described in this order when they run.
	/// </param>
	/// <returns>The description.</returns>
	public static string Describe(IForgeEntity entity, IEnumerable<GraphProcessor>? graphs = null)
	{
		var lines = new List<string>();

		foreach (EntityAttribute attribute in entity.Attributes.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
		{
			lines.Add(
				$"{attribute.Key}: {attribute.BaseValue}/{attribute.CurrentValue} [{attribute.Min}..{attribute.Max}] " +
				$"modifier={attribute.Modifier} overflow={attribute.Overflow}");
		}

		lines.Add($"tags: base={Tags(entity.Tags.BaseTags)} modifier={Tags(entity.Tags.ModifierTags)} " +
			$"all={Tags(entity.Tags.AllTags)}");
		lines.Add($"blocked ability tags: {Tags(entity.Abilities.BlockedAbilityTags.AllTags)}");

		foreach (ActiveEffectHandle effect in entity.EffectsManager.GetActiveEffects())
		{
			lines.Add($"effect {Effect(effect)}");
		}

		foreach (AbilityHandle ability in entity.Abilities.GrantedAbilities)
		{
			lines.Add($"ability level={ability.Level} active={ability.IsActive} inhibited={ability.IsInhibited}");
		}

		if (entity.Input is EntityInput input)
		{
			lines.Add($"input {input.Current}");
		}

		if (entity.Mover is IEntityMover mover)
		{
			lines.Add($"body {mover.CaptureState()}");
		}

		foreach (GraphProcessor graph in graphs ?? [])
		{
			if (graph.GraphContext.IsActive)
			{
				lines.Add($"graph {DescribeGraph(graph)}");
			}
		}

		return string.Join(Environment.NewLine, lines);
	}

	/// <summary>
	/// Describes a graph's state nodes by their contexts' public properties, with values written out and the objects
	/// they point at described by value.
	/// </summary>
	/// <param name="processor">The graph's processor.</param>
	/// <returns>The description.</returns>
	public static string DescribeGraph(GraphProcessor processor)
	{
		IEnumerable<string> contexts = processor.Graph.Nodes
			.Where(IsStateNode)
			.Select(x => DescribeContext(processor, x));

		return string.Join(" | ", contexts);
	}

	private static string DescribeContext(GraphProcessor processor, Node node)
	{
		StateNodeContext context;

		try
		{
			context = processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID);
		}
		catch (InvalidOperationException)
		{
			return $"{node.GetType().Name} none";
		}

		IEnumerable<string> properties = context.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.OrderBy(x => x.Name, StringComparer.Ordinal)
			.Select(x => $"{x.Name}={Value(x.GetValue(context))}");

		return $"{node.GetType().Name} {{ {string.Join(", ", properties)} }}";
	}

	private static bool IsStateNode(Node node)
	{
		for (Type? type = node.GetType(); type is not null; type = type.BaseType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StateNode<>))
			{
				return true;
			}
		}

		return false;
	}

	private static string Effect(ActiveEffectHandle effect)
	{
		if (!effect.IsValid)
		{
			return "gone";
		}

		IEnumerable<string> dataTags = effect.Effect!.DataTag
			.Select(x => $"{x.Key}={Number(x.Value)}")
			.OrderBy(x => x, StringComparer.Ordinal);

		return $"{effect.Effect.EffectData.Name} level={effect.Level} stacks={effect.StackCount} " +
			$"remaining={Number(effect.RemainingDuration)}/{Number(effect.TotalDuration)} " +
			$"period={Number(effect.Period)} executions={effect.ExecutionCount} inhibited={effect.IsInhibited} " +
			$"data=[{string.Join(", ", dataTags)}]";
	}

	private static string Tags(TagContainer tags)
	{
		return $"[{string.Join(", ", tags.Select(x => x.ToString()).OrderBy(x => x, StringComparer.Ordinal))}]";
	}

	private static string Number(double value)
	{
		return value.ToString("R", CultureInfo.InvariantCulture);
	}

	private static string Value(object? value)
	{
		return value switch
		{
			null => "null",
			string text => text,

			// A token or a handler is new each time a listener subscribes, so only that there is one is compared.
			EventSubscriptionToken => "token",
			Delegate => "handler",
			ActiveEffectHandle handle => $"effect({Effect(handle)})",
			AbilityHandle handle => handle.IsValid ? $"ability(active={handle.IsActive})" : "ability(gone)",
			AbilityInstanceHandle handle => $"instance(valid={handle.IsValid} active={handle.IsActive})",
			IForgeEntity => "entity",
			Effect effect => $"{effect.EffectData.Name}@{effect.Level}",
			double number => Number(number),
			float number => Number(number),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			IDictionary dictionary => $"{{{string.Join(", ", DescribeEntries(dictionary))}}}",
			IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Value))}]",
			_ when value.GetType().IsValueType => value.ToString()!,
			_ => value.GetType().Name,
		};
	}

	private static IEnumerable<string> DescribeEntries(IDictionary dictionary)
	{
		foreach (DictionaryEntry entry in dictionary)
		{
			yield return $"{Value(entry.Key)}: {Value(entry.Value)}";
		}
	}
}
