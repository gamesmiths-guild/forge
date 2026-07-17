// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MathFillResolverTests
{
	[Theory]
	[Trait("Resolver", "InverseLerp")]
	[InlineData(0f, 10f, 2.5f, 0.25f)]
	[InlineData(0f, 10f, 15f, 1f)]
	[InlineData(0f, 10f, -5f, 0f)]
	[InlineData(5f, 5f, 7f, 0f)]
	public void Inverse_lerp_resolver_normalizes_values(float a, float b, float value, float expected)
	{
		var resolver = new InverseLerpResolver(Constant(a), Constant(b), Constant(value));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(expected, 0.0001f);
	}

	[Theory]
	[Trait("Resolver", "Remap")]
	[InlineData(5f, 0f, 10f, 0f, 100f, false, 50f)]
	[InlineData(15f, 0f, 10f, 0f, 100f, false, 150f)]
	[InlineData(15f, 0f, 10f, 0f, 100f, true, 100f)]
	[InlineData(5f, 5f, 5f, 0f, 100f, false, 0f)]
	public void Remap_resolver_remaps_ranges(
		float value,
		float inMin,
		float inMax,
		float outMin,
		float outMax,
		bool clamp,
		float expected)
	{
		var resolver = new RemapResolver(
			Constant(value),
			Constant(inMin),
			Constant(inMax),
			Constant(outMin),
			Constant(outMax),
			clamp);

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(expected, 0.0001f);
	}

	[Fact]
	[Trait("Resolver", "Remap")]
	public void Remap_resolver_promotes_to_double_when_any_operand_is_double()
	{
		var resolver = new RemapResolver(
			ConstantDouble(5),
			Constant(0f),
			Constant(10f),
			Constant(0f),
			Constant(100f));

		resolver.ValueType.Should().Be(typeof(double));
		resolver.Resolve(new GraphContext()).Get<double>().Should().BeApproximately(50, 0.0001);
	}

	[Theory]
	[Trait("Resolver", "SmoothStep")]
	[InlineData(0f, 1f, 0.5f, 0.5f)]
	[InlineData(0f, 10f, 2.5f, 0.15625f)]
	[InlineData(0f, 1f, -1f, 0f)]
	[InlineData(0f, 1f, 2f, 1f)]
	public void Smooth_step_resolver_interpolates(float edge0, float edge1, float value, float expected)
	{
		var resolver = new SmoothStepResolver(Constant(edge0), Constant(edge1), Constant(value));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(expected, 0.0001f);
	}

	[Theory]
	[Trait("Resolver", "Wrap")]
	[InlineData(370f, 0f, 360f, 10f)]
	[InlineData(-30f, 0f, 360f, 330f)]
	[InlineData(90f, 0f, 360f, 90f)]
	public void Wrap_resolver_wraps_into_range(float value, float min, float max, float expected)
	{
		var resolver = new WrapResolver(Constant(value), Constant(min), Constant(max));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(expected, 0.0001f);
	}

	[Theory]
	[Trait("Resolver", "PingPong")]
	[InlineData(7f, 5f, 3f)]
	[InlineData(12f, 5f, 2f)]
	[InlineData(3f, 5f, 3f)]
	public void Ping_pong_resolver_bounces_values(float value, float length, float expected)
	{
		var resolver = new PingPongResolver(Constant(value), Constant(length));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(expected, 0.0001f);
	}

	[Fact]
	[Trait("Resolver", "DeltaAngle")]
	public void Delta_angle_resolver_returns_the_shortest_signed_difference()
	{
		var resolver = new DeltaAngleResolver(Constant(0f), Constant(3f * MathF.PI / 2f));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(-MathF.PI / 2f, 0.0001f);
	}

	[Theory]
	[Trait("Resolver", "Approximately")]
	[InlineData(1.0000001f, 1f, true)]
	[InlineData(1.1f, 1f, false)]
	public void Approximately_resolver_compares_with_tolerance(float a, float b, bool expected)
	{
		var resolver = new ApproximatelyResolver(Constant(a), Constant(b));

		resolver.Resolve(new GraphContext()).AsBool().Should().Be(expected);
	}

	[Theory]
	[Trait("Resolver", "Conditional")]
	[InlineData(true, 5)]
	[InlineData(false, 7)]
	public void Conditional_resolver_selects_a_branch(bool condition, int expected)
	{
		var resolver = new ConditionalResolver(
			new VariantResolver(new Variant128(condition), typeof(bool)),
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(7), typeof(int)));

		resolver.Resolve(new GraphContext()).Get<int>().Should().Be(expected);
	}

	[Fact]
	[Trait("Resolver", "Conditional")]
	public void Conditional_resolver_rejects_mismatched_branch_types()
	{
		FluentActions.Invoking(() => new ConditionalResolver(
				new VariantResolver(new Variant128(true), typeof(bool)),
				new VariantResolver(new Variant128(5), typeof(int)),
				Constant(7f)))
			.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "CurveSample")]
	public void Curve_sample_resolver_evaluates_the_curve()
	{
		var resolver = new CurveSampleResolver(new DoublingCurve(), Constant(3f));

		resolver.Resolve(new GraphContext()).Get<float>().Should().BeApproximately(6f, 0.0001f);
	}

	private static VariantResolver Constant(float value)
	{
		return new VariantResolver(new Variant128(value), typeof(float));
	}

	private static VariantResolver ConstantDouble(double value)
	{
		return new VariantResolver(new Variant128(value), typeof(double));
	}

	private sealed class DoublingCurve : ICurve
	{
		public float Evaluate(float value)
		{
			return value * 2f;
		}
	}
}
