// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// Covers an entity's input buffer: frames become current as ticks begin, edges are never lost between ticks, and the
/// name readers answer like an engine's input API.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class EntityInputTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Input", "Buffer")]
	public void A_submitted_frame_becomes_current_when_the_tick_begins()
	{
		TestEntity entity = CreateEntity(out EntityInput input);

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		input.IsHeld("jump").Should().BeFalse("the frame waits for its tick");

		EntitySimulation.Tick(entity, 1, Step);

		input.IsHeld("jump").Should().BeTrue();
		input.WasPressed("jump").Should().BeTrue();
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void A_tick_with_nothing_submitted_keeps_held_actions_without_new_edges()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);

		EntitySimulation.Tick(entity, 2, Step);

		input.IsHeld("jump").Should().BeTrue("the button is still down");
		input.WasPressed("jump").Should().BeFalse("it was pressed once, a tick ago");
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void Frames_submitted_twice_before_a_tick_keep_every_edge_and_the_latest_state()
	{
		TestEntity entity = CreateEntity(out EntityInput input);

		input.Submit(new InputFrame { Held = 0b01, Pressed = 0b01 });
		input.Submit(new InputFrame { Held = 0b10, Pressed = 0b10, Released = 0b01 });
		EntitySimulation.Tick(entity, 1, Step);

		input.WasPressed("jump").Should().BeTrue("the tap on jump is kept");
		input.WasReleased("jump").Should().BeTrue();
		input.IsHeld("jump").Should().BeFalse();
		input.WasPressed("dash").Should().BeTrue();
		input.IsHeld("dash").Should().BeTrue();
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void An_action_the_input_does_not_carry_reads_as_never_used()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		input.Submit(new InputFrame { Held = ulong.MaxValue, Pressed = ulong.MaxValue });
		EntitySimulation.Tick(entity, 1, Step);

		input.IsHeld("fly").Should().BeFalse();
		input.WasPressed("fly").Should().BeFalse();
		input.GetStrength("fly").Should().Be(0);
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void Strength_axis_and_vector_come_from_the_vector_that_covers_them()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		input.Submit(default(InputFrame).WithVector(0, new Vector2(-0.5f, 1f)));
		EntitySimulation.Tick(entity, 1, Step);

		const float half = 64 / 127f;
		input.GetStrength("move_left").Should().Be(half);
		input.GetStrength("move_right").Should().Be(0);
		input.GetAxis("move_left", "move_right").Should().Be(-half);
		input.GetAxis("move_forward", "move_back").Should().Be(1);
		input.GetVector("move_left", "move_right", "move_forward", "move_back").Should().Be(new Vector2(-half, 1));
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void Without_a_vector_strength_axis_and_vector_come_from_held_buttons()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		input.Submit(new InputFrame { Held = 0b11 });
		EntitySimulation.Tick(entity, 1, Step);

		input.GetStrength("jump").Should().Be(1);
		input.GetAxis("dash", "jump").Should().Be(0, "both ends are held");

		Vector2 diagonal = input.GetVector("fly", "jump", "fly", "dash");
		diagonal.Length().Should().BeApproximately(1, 0.0001f, "a vector from buttons is no longer than 1");
		diagonal.X.Should().BeApproximately(diagonal.Y, 0.0001f);
	}

	[Fact]
	[Trait("Input", "Buffer")]
	public void An_action_set_refuses_duplicates_and_more_actions_than_it_has_bits()
	{
		FluentActions.Invoking(() => new InputActionSet(["jump", "jump"])).Should().Throw<ArgumentException>();

		FluentActions.Invoking(() => new InputActionSet([.. Enumerable.Range(0, 65).Select(x => $"action_{x}")]))
			.Should().Throw<ArgumentException>();

		new InputActionSet(["a", "b"]).IndexOf("b").Should().Be(1);
		new InputActionSet(["a", "b"]).IndexOf("c").Should().Be(-1);
	}

	private TestEntity CreateEntity(out EntityInput input)
	{
		input = new EntityInput(new InputActionSet(
			["jump", "dash"],
			[new InputVector("move_left", "move_right", "move_forward", "move_back")]));

		return new TestEntity(_tagsManager, _cuesManager) { Input = input };
	}
}
