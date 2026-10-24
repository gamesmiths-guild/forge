// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

/// <summary>
/// Covers the resolvers that read an entity's input: the frame of its current tick, the ability owner's by default.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class InputResolverTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_action_resolver_reads_whether_the_action_is_held_pressed_or_released()
	{
		TestEntity entity = CreateEntity();
		GraphContext context = CreateContext(entity, out EntityVariableResolver source);

		entity.Input!.Submit(new InputFrame { Held = 0b01, Pressed = 0b01, Released = 0b10 });
		EntitySimulation.Tick(entity, 1, Step);

		Resolve(new InputActionResolver(source, "jump"), context).AsBool()
			.Should().BeTrue();
		Resolve(new InputActionResolver(source, "jump", InputActionState.Pressed), context).AsBool()
			.Should().BeTrue();
		Resolve(new InputActionResolver(source, "jump", InputActionState.Released), context).AsBool()
			.Should().BeFalse();
		Resolve(new InputActionResolver(source, "dash"), context).AsBool()
			.Should().BeFalse();
		Resolve(new InputActionResolver(source, "dash", InputActionState.Released), context).AsBool()
			.Should().BeTrue();
		Resolve(new InputActionResolver(source, "fire"), context).AsBool()
			.Should().BeFalse("the input does not carry it");
	}

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_strength_axis_and_vector_resolvers_read_the_inputs_vector()
	{
		TestEntity entity = CreateEntity();
		GraphContext context = CreateContext(entity, out EntityVariableResolver source);

		entity.Input!.Submit(default(InputFrame).WithVector(0, new Vector2(-1, 0)));
		EntitySimulation.Tick(entity, 1, Step);

		Resolve(new InputStrengthResolver(source, "move_left"), context).AsFloat().Should().Be(1);
		Resolve(new InputStrengthResolver(source, "move_right"), context).AsFloat().Should().Be(0);
		Resolve(new InputAxisResolver(source, "move_left", "move_right"), context).AsFloat().Should().Be(-1);
		Resolve(new InputVectorResolver(source, "move_left", "move_right", "move_forward", "move_back"), context)
			.AsVector2().Should().Be(new Vector2(-1, 0));
	}

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_strength_axis_and_vector_resolvers_read_buttons_as_full_strength()
	{
		TestEntity entity = CreateEntity();
		GraphContext context = CreateContext(entity, out EntityVariableResolver source);

		entity.Input!.Submit(new InputFrame { Held = 0b01 });
		EntitySimulation.Tick(entity, 1, Step);

		Resolve(new InputStrengthResolver(source, "jump"), context).AsFloat().Should().Be(1);
		Resolve(new InputAxisResolver(source, "dash", "jump"), context).AsFloat().Should().Be(1);
		Resolve(new InputVectorResolver(source, "dash", "jump", "fire", "fire"), context)
			.AsVector2().Should().Be(new Vector2(1, 0));
	}

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_aim_resolver_reads_the_aim_point_and_direction_in_3D_and_2D()
	{
		TestEntity entity = CreateEntity();
		GraphContext context = CreateContext(entity, out EntityVariableResolver source);
		var point2D = new InputAimResolver(source, InputAimValue.Point2D);

		EntitySimulation.Tick(entity, 1, Step);

		Resolve(point2D, context).AsVector2().Should().Be(Vector2.Zero, "an input without aim has none to give");

		entity.Input!.Submit(default(InputFrame).WithAim(new Vector3(4, 5, 6), new Vector3(1, 0, 0)));
		EntitySimulation.Tick(entity, 2, Step);

		Resolve(new InputAimResolver(source, InputAimValue.Point3D), context).AsVector3()
			.Should().Be(new Vector3(4, 5, 6));
		Resolve(point2D, context).AsVector2().Should().Be(new Vector2(4, 5));
		Resolve(new InputAimResolver(source, InputAimValue.Direction3D), context).AsVector3()
			.Should().Be(new Vector3(1, 0, 0));
		Resolve(new InputAimResolver(source, InputAimValue.Direction2D), context).AsVector2()
			.Should().Be(new Vector2(1, 0));
		point2D.ValueType.Should().Be<Vector2>();
		new InputAimResolver(source, InputAimValue.Direction3D).ValueType.Should().Be<Vector3>();
	}

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_resolvers_resolve_to_default_when_the_source_has_no_input()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		GraphContext context = CreateContext(entity, out EntityVariableResolver source);

		Resolve(new InputActionResolver(source, "jump"), context).AsBool().Should().BeFalse();
		Resolve(new InputStrengthResolver(source, "jump"), context).AsFloat().Should().Be(0);
		Resolve(new InputAimResolver(source, InputAimValue.Point3D), context).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "Input")]
	public void Input_resolvers_read_the_ability_owners_input_by_default()
	{
		TestEntity owner = CreateEntity();
		GraphContext context = CreateAbilityGraphContext(owner);

		owner.Input!.Submit(new InputFrame { Held = 0b01 }.WithVector(0, new Vector2(0, 1)));
		EntitySimulation.Tick(owner, 1, Step);

		Resolve(new InputActionResolver("jump"), context).AsBool().Should().BeTrue();
		Resolve(new InputStrengthResolver("move_back"), context).AsFloat().Should().Be(1);
		Resolve(new InputAxisResolver("move_forward", "move_back"), context).AsFloat().Should().Be(1);
		Resolve(new InputVectorResolver("move_left", "move_right", "move_forward", "move_back"), context)
			.AsVector2().Should().Be(new Vector2(0, 1));
		Resolve(new InputAimResolver(InputAimValue.Point3D), context).AsVector3().Should().Be(Vector3.Zero);
	}

	private static Variant128 Resolve(IPropertyResolver resolver, GraphContext context)
	{
		return resolver.Resolve(context);
	}

	private static GraphContext CreateContext(TestEntity entity, out EntityVariableResolver source)
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("source", entity);
		source = new EntityVariableResolver("source");
		return context;
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager)
		{
			Input = new EntityInput(new InputActionSet(
				["jump", "dash"],
				[new InputVector("move_left", "move_right", "move_forward", "move_back")])),
		};
	}
}
