// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

/// <summary>
/// End-to-end coverage for the motivating array pipeline: collect entities, sort them by a per-element key, and keep
/// the closest three. The per-element key is authored purely from composable resolvers
/// (<see cref="ElementEntityResolver"/> feeding an <see cref="AttributeResolver"/>).
/// </summary>
/// <param name="tagsAndCuesFixture">The fixture providing tags and cues managers.</param>
public class ArrayPipelineIntegrationTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ArrayPipeline")]
	public void Order_by_take_pipeline_selects_the_three_closest_entities()
	{
		// "Distance to the owner" is modeled as an attribute so the whole pipeline runs on core resolvers.
		VitalTestEntity[] entities =
		[
			CreateEntityWithDistance(50),
			CreateEntityWithDistance(10),
			CreateEntityWithDistance(40),
			CreateEntityWithDistance(20),
			CreateEntityWithDistance(30),
		];

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>(
			"nearbyEntities",
			[entities[0], entities[1], entities[2], entities[3], entities[4]]);

		var threeClosest = new ObjectTakeResolver<IForgeEntity>(
			new ObjectOrderByResolver<IForgeEntity>(
				new EntityArrayVariableResolver("nearbyEntities"),
				new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver())),
			new VariantResolver(new Variant128(3), typeof(int)));

		IForgeEntity[] result = threeClosest.ResolveArray(context);

		result.Should().Equal(entities[1], entities[3], entities[4]);
	}

	[Fact]
	[Trait("Resolver", "ArrayPipeline")]
	public void Where_order_by_take_pipeline_composes_filtering_and_sorting()
	{
		VitalTestEntity[] entities =
		[
			CreateEntityWithDistance(50),
			CreateEntityWithDistance(10),
			CreateEntityWithDistance(40),
			CreateEntityWithDistance(20),
		];

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>(
			"nearbyEntities",
			[entities[0], entities[1], entities[2], entities[3]]);

		var distanceKey = new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver());

		// Keep entities farther than 15, then take the two closest of those.
		var pipeline = new ObjectTakeResolver<IForgeEntity>(
			new ObjectOrderByResolver<IForgeEntity>(
				new ObjectWhereResolver<IForgeEntity>(
					new EntityArrayVariableResolver("nearbyEntities"),
					new ComparisonResolver(
						distanceKey,
						ComparisonOperation.GreaterThan,
						new VariantResolver(new Variant128(15), typeof(int)))),
				distanceKey),
			new VariantResolver(new Variant128(2), typeof(int)));

		IForgeEntity[] result = pipeline.ResolveArray(context);

		result.Should().Equal(entities[3], entities[2]);
	}

	private VitalTestEntity CreateEntityWithDistance(int distance)
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);
		entity.VitalAttributeSet.UpdateBaseHealth(distance);
		return entity;
	}
}
