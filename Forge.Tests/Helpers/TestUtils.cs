// Copyright © 2024 Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.Helpers;

public static class TestUtils
{
	public static void TestStackData(
		IEnumerable<GameplayEffectStackInstanceData> stackData,
		int expectedStackDataCount,
		object[] expectedStackData,
		TestEntity entity1,
		TestEntity entity2)
	{
		stackData.Should().HaveCount(expectedStackDataCount);

		for (var i = 0; i < expectedStackDataCount; i++)
		{
			var expectedData = (int[])expectedStackData[i];

			stackData.ElementAt(i).StackCount.Should().Be(expectedData[0]);
			stackData.ElementAt(i).EffectLevel.Should().Be(expectedData[1]);
#pragma warning disable FAA0001 // Simplify Assertion
			stackData.ElementAt(i).Owner.Should().Be(expectedData[2] == 0 ? entity1 : entity2);
#pragma warning restore FAA0001 // Simplify Assertion
		}
	}

	public static void TestAttribute(TestEntity target, string targetAttribute, int[] expectedResults)
	{
		target.Attributes[targetAttribute].CurrentValue.Should().Be(expectedResults[0]);
		target.Attributes[targetAttribute].BaseValue.Should().Be(expectedResults[1]);
		target.Attributes[targetAttribute].Modifier.Should().Be(expectedResults[2]);
		target.Attributes[targetAttribute].Overflow.Should().Be(expectedResults[3]);
	}

	public static HashSet<GameplayTag> StringToGameplayTag(GameplayTagsManager gameplayTagsManager, string[]? keys)
	{
		if (keys is null)
		{
			return [];
		}

		var tags = new HashSet<GameplayTag>();

		foreach (var key in keys)
		{
			tags.Add(GameplayTag.RequestTag(gameplayTagsManager, key));
		}

		return tags;
	}
}
