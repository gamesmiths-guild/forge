// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// Covers one tick of input: action bits by slot, and vectors and aim stored already quantized.
/// </summary>
public class InputFrameTests
{
	[Fact]
	[Trait("Input", "Frame")]
	public void Bits_report_each_action_by_its_slot()
	{
		var frame = new InputFrame { Held = 0b0101, Pressed = 0b0001, Released = 0b1000 };

		frame.IsHeld(0).Should().BeTrue();
		frame.IsHeld(1).Should().BeFalse();
		frame.IsHeld(2).Should().BeTrue();
		frame.WasPressed(0).Should().BeTrue();
		frame.WasPressed(2).Should().BeFalse("a held action was not necessarily pressed this tick");
		frame.WasReleased(3).Should().BeTrue();
		frame.IsHeld(63).Should().BeFalse();
	}

	[Fact]
	[Trait("Input", "Frame")]
	public void Vectors_are_stored_in_steps_of_one_over_127_and_clamped()
	{
		InputFrame frame = default(InputFrame).WithVector(0, new Vector2(0.25f, -2f));

		frame.GetVector(0).Should().Be(new Vector2(32 / 127f, -1f));
	}

	[Fact]
	[Trait("Input", "Frame")]
	public void A_vector_component_that_is_not_a_number_is_stored_as_zero()
	{
		InputFrame frame = default(InputFrame).WithVector(0, new Vector2(float.NaN, 1f));

		frame.GetVector(0).Should().Be(new Vector2(0f, 1f));
	}

	[Fact]
	[Trait("Input", "Frame")]
	public void Each_vector_keeps_its_own_slot()
	{
		InputFrame frame = default(InputFrame)
			.WithVector(0, new Vector2(1, 0))
			.WithVector(1, new Vector2(0, -1))
			.WithVector(2, new Vector2(-1, 1))
			.WithVector(3, new Vector2(0, 1))
			.WithVector(1, new Vector2(1, 1));

		frame.GetVector(0).Should().Be(new Vector2(1, 0));
		frame.GetVector(1).Should().Be(new Vector2(1, 1), "writing a slot again replaces it");
		frame.GetVector(2).Should().Be(new Vector2(-1, 1));
		frame.GetVector(3).Should().Be(new Vector2(0, 1));
	}

	[Fact]
	[Trait("Input", "Frame")]
	public void Aim_is_stored_in_thousandths_of_a_unit_and_steps_of_one_over_32767()
	{
		InputFrame frame = default(InputFrame).WithAim(new Vector3(1.23456f, 0, -7.0001f), new Vector3(0.6f, 0, -0.8f));

		frame.HasAim.Should().BeTrue();
		frame.AimPoint.Should().Be(new Vector3(1.235f, 0, -7f));
		frame.AimDirection.Should().Be(new Vector3(
			(float)(Math.Round(0.6f * 32767d) / 32767),
			0,
			(float)(Math.Round(-0.8f * 32767d) / 32767)));
	}

	[Fact]
	[Trait("Input", "Frame")]
	public void Quantizing_again_changes_nothing()
	{
		InputFrame frame = default(InputFrame)
			.WithVector(0, new Vector2(0.3f, -0.71f))
			.WithAim(new Vector3(10.0005f, 2.5f, -3.33333f), Vector3.Normalize(new Vector3(1, 0, 3)));

		InputFrame again = frame
			.WithVector(0, frame.GetVector(0))
			.WithAim(frame.AimPoint, frame.AimDirection);

		again.Should().Be(frame, "a machine that receives a frame and rebuilds it gets exactly the same values");
	}
}
