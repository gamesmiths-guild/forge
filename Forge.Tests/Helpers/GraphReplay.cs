// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// Runs a graph through a number of steps, then puts it back from the snapshot taken after each step in turn and runs
/// the rest again. The second run has to see exactly what the first saw: the same messages during each step, and the
/// same state after it.
/// </summary>
internal static class GraphReplay
{
	/// <summary>
	/// Asserts that a snapshot taken after any step restores that step's state and runs the steps after it again.
	/// </summary>
	/// <param name="processor">The started graph.</param>
	/// <param name="log">The log the graph's tracking nodes write to. It's cleared between steps.</param>
	/// <param name="steps">How many steps to run.</param>
	/// <param name="step">Runs one step, numbered from 1. Whatever it does to the world depends on its number alone.
	/// </param>
	/// <param name="describe">Describes what a step left behind: running nodes, node contexts, variables.</param>
	/// <param name="rewindWorld">Puts what the steps change outside the graph back as it was after the given step,
	/// before the graph is restored, as restoring the world does in a rollback.</param>
	public static void AssertRepeats(
		GraphProcessor processor,
		List<string> log,
		int steps,
		Action<int> step,
		Func<string> describe,
		Action<int>? rewindWorld = null)
	{
		string[] states = new string[steps + 1];
		string[] messages = new string[steps + 1];
		var snapshots = new GraphSnapshot[steps + 1];

		string Run(int i)
		{
			// What reached the log before the step, such as the world being put back, isn't the step's.
			log.Clear();
			step(i);
			return string.Join(", ", log);
		}

		states[0] = describe();
		snapshots[0] = processor.CaptureSnapshot();

		for (int i = 1; i <= steps; i++)
		{
			messages[i] = Run(i);
			states[i] = describe();
			snapshots[i] = processor.CaptureSnapshot();
		}

		for (int from = 0; from < steps; from++)
		{
			rewindWorld?.Invoke(from);
			processor.RestoreSnapshot(snapshots[from]);

			describe()
				.Should().Be(states[from], $"restoring the snapshot taken after step {from} brings that step back");

			for (int i = from + 1; i <= steps; i++)
			{
				Run(i).Should().Be(messages[i], $"step {i} sends the same messages after a restore to step {from}");
				describe().Should().Be(states[i], $"step {i} leaves the same state after a restore to step {from}");
			}
		}
	}

	/// <summary>
	/// Describes a node's context so two can be compared: its public properties by name, with values written out and
	/// other objects by identity, so a restored context has to point at the very same objects.
	/// </summary>
	/// <param name="processor">The graph's processor.</param>
	/// <param name="node">The node.</param>
	/// <returns>The description.</returns>
	public static string DescribeContext(GraphProcessor processor, Node node)
	{
		StateNodeContext context;

		try
		{
			context = processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID);
		}
		catch (InvalidOperationException)
		{
			return "no context";
		}

		IEnumerable<string> properties = context.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.OrderBy(x => x.Name, StringComparer.Ordinal)
			.Select(x => $"{x.Name}={Describe(x.GetValue(context))}");

		return $"{context.GetType().Name} {{ {string.Join(", ", properties)} }}";
	}

	private static string Describe(object? value)
	{
		return value switch
		{
			null => "null",
			string text => text,

			// A token's id is new each time a listener subscribes, so only that there is one is compared.
			EventSubscriptionToken => "token",
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			IDictionary dictionary => $"{{{string.Join(", ", DescribeEntries(dictionary))}}}",
			IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Describe))}]",
			_ when value.GetType().IsValueType => value.ToString()!,
			_ => $"{value.GetType().Name}#{RuntimeHelpers.GetHashCode(value)}",
		};
	}

	private static IEnumerable<string> DescribeEntries(IDictionary dictionary)
	{
		foreach (DictionaryEntry entry in dictionary)
		{
			yield return $"{Describe(entry.Key)}: {Describe(entry.Value)}";
		}
	}
}
