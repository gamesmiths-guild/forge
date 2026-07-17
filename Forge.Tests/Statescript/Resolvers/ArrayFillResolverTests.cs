// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ArrayFillResolverTests
{
	[Fact]
	[Trait("Resolver", "Intersect")]
	public void Intersect_resolver_keeps_elements_present_in_both_arrays()
	{
		var source = new TestArrayPropertyResolver(typeof(int), [IntArray(1, 2, 3, 2)]);
		var other = new TestArrayPropertyResolver(typeof(int), [IntArray(2, 4)]);

		Variant128[] result = new IntersectResolver(source, other).ResolveArray(new GraphContext());

		result.Select(x => x.Get<int>()).Should().Equal(2, 2);
	}

	[Fact]
	[Trait("Resolver", "Intersect")]
	public void Object_intersect_resolver_matches_by_reference()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("source", typeof(string), ["a", "b", "c"]);
		context.GraphVariables.DefineObjectArrayVariable("other", typeof(string), ["c", "a"]);

		string[] result = new ObjectIntersectResolver<string>(
				new ObjectArrayVariableResolver<string>("source"),
				new ObjectArrayVariableResolver<string>("other"))
			.ResolveArray(context);

		result.Should().Equal("a", "c");
	}

	[Fact]
	[Trait("Resolver", "RandomElement")]
	public void Random_element_resolver_picks_the_rolled_index()
	{
		var source = new TestArrayPropertyResolver(typeof(int), [IntArray(10, 20, 30)]);
		var resolver = new RandomElementResolver(source, new FixedRandom(nextInt: 1));

		resolver.Resolve(new GraphContext()).Get<int>().Should().Be(20);
	}

	[Fact]
	[Trait("Resolver", "RandomElement")]
	public void Random_element_resolver_returns_default_for_empty_arrays()
	{
		var source = new TestArrayPropertyResolver(typeof(int), [[]]);
		var resolver = new RandomElementResolver(source, new FixedRandom(nextInt: 0));

		resolver.Resolve(new GraphContext()).Get<int>().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "RandomElement")]
	public void Object_random_element_resolver_picks_the_rolled_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("source", typeof(string), ["a", "b", "c"]);

		var resolver = new ObjectRandomElementResolver<string>(
			new ObjectArrayVariableResolver<string>("source"),
			new FixedRandom(nextInt: 2));

		resolver.Resolve(context).Should().Be("c");
	}

	[Fact]
	[Trait("Resolver", "Shuffle")]
	public void Shuffle_resolver_permutes_with_the_random_provider()
	{
		var source = new TestArrayPropertyResolver(typeof(int), [IntArray(1, 2, 3)]);
		var resolver = new ShuffleResolver(source, new TrackingRandom(nextIntsInclusive: [0, 0, 0]));

		Variant128[] result = resolver.ResolveArray(new GraphContext());

		// Inside-out Fisher-Yates with j always 0 rotates the last element to the front.
		result.Select(x => x.Get<int>()).Should().Equal(3, 1, 2);
	}

	[Fact]
	[Trait("Resolver", "Shuffle")]
	public void Object_shuffle_resolver_permutes_with_the_random_provider()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("source", typeof(string), ["a", "b", "c"]);

		var resolver = new ObjectShuffleResolver<string>(
			new ObjectArrayVariableResolver<string>("source"),
			new TrackingRandom(nextIntsInclusive: [0, 0, 0]));

		resolver.ResolveArray(context).Should().Equal("c", "a", "b");
	}

	[Theory]
	[Trait("Resolver", "Conditional")]
	[InlineData(true, "yes")]
	[InlineData(false, "no")]
	public void Conditional_object_resolver_selects_a_branch(bool condition, string expected)
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("whenTrue", "yes");
		context.GraphVariables.DefineObjectVariable("whenFalse", "no");

		var resolver = new ConditionalObjectResolver<string>(
			new VariantResolver(new Variant128(condition), typeof(bool)),
			new ObjectVariableResolver<string>("whenTrue"),
			new ObjectVariableResolver<string>("whenFalse"));

		resolver.Resolve(context).Should().Be(expected);
	}

	private static Variant128[] IntArray(params int[] values)
	{
		var result = new Variant128[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			result[i] = new Variant128(values[i]);
		}

		return result;
	}
}
