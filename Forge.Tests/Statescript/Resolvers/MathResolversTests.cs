// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MathResolversTests
{
	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Add_then_multiply_follows_explicit_grouping()
	{
		// (10 + 5) * 3 = 45
		var resolver = new MultiplyResolver(
			new AddResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				new VariantResolver(new Variant128(5), typeof(int))),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(45);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Multiply_then_add_follows_explicit_grouping()
	{
		// 10 + (5 * 3) = 25
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new MultiplyResolver(
				new VariantResolver(new Variant128(5), typeof(int)),
				new VariantResolver(new Variant128(3), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(25);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Four_operations_chained()
	{
		// ((10 + 2) * 3 - 6) / 3 = (12 * 3 - 6) / 3 = (36 - 6) / 3 = 30 / 3 = 10
		var addStep = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		var multiplyStep = new MultiplyResolver(
			addStep,
			new VariantResolver(new Variant128(3), typeof(int)));

		var subtractStep = new SubtractResolver(
			multiplyStep,
			new VariantResolver(new Variant128(6), typeof(int)));

		var divideStep = new DivideResolver(
			subtractStep,
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		divideStep.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Damage_formula_base_plus_bonus_times_multiplier()
	{
		// finalDamage = (baseDamage + bonusDamage) * critMultiplier
		// (25 + 10) * 2 = 70
		var resolver = new MultiplyResolver(
			new AddResolver(
				new VariantResolver(new Variant128(25), typeof(int)),
				new VariantResolver(new Variant128(10), typeof(int))),
			new VariantResolver(new Variant128(2), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(70);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Effective_damage_after_armor_reduction()
	{
		// effectiveDamage = rawDamage - (rawDamage * armorReduction)
		// 100 - (100 * 0.25) = 100 - 25 = 75.0
		var rawDamage = new VariantResolver(new Variant128(100.0), typeof(double));
		var armorReduction = new VariantResolver(new Variant128(0.25), typeof(double));

		var resolver = new SubtractResolver(
			rawDamage,
			new MultiplyResolver(rawDamage, armorReduction));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(75.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Health_percentage_formula()
	{
		// healthPercent = (currentHealth / maxHealth) * 100
		// (75.0 / 200.0) * 100.0 = 37.5
		var resolver = new MultiplyResolver(
			new DivideResolver(
				new VariantResolver(new Variant128(75.0), typeof(double)),
				new VariantResolver(new Variant128(200.0), typeof(double))),
			new VariantResolver(new Variant128(100.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(37.5);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Cooldown_reduction_formula()
	{
		// effectiveCooldown = baseCooldown * (1.0 - cooldownReduction)
		// 10.0 * (1.0 - 0.3) = 10.0 * 0.7 = 7.0
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new SubtractResolver(
				new VariantResolver(new Variant128(1.0), typeof(double)),
				new VariantResolver(new Variant128(0.3), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(7.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Linear_interpolation_formula()
	{
		// lerp(a, b, t) = a + (b - a) * t
		// lerp(10, 30, 0.5) = 10 + (30 - 10) * 0.5 = 10 + 10 = 20
		var a = new VariantResolver(new Variant128(10.0), typeof(double));
		var b = new VariantResolver(new Variant128(30.0), typeof(double));
		var t = new VariantResolver(new Variant128(0.5), typeof(double));

		var resolver = new AddResolver(
			a,
			new MultiplyResolver(
				new SubtractResolver(b, a),
				t));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(20.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Linear_interpolation_at_boundaries()
	{
		// lerp(10, 30, 0.0) = 10, lerp(10, 30, 1.0) = 30
		var a = new VariantResolver(new Variant128(10.0), typeof(double));
		var b = new VariantResolver(new Variant128(30.0), typeof(double));

		var t0 = new VariantResolver(new Variant128(0.0), typeof(double));
		var t1 = new VariantResolver(new Variant128(1.0), typeof(double));

		var lerpAtZero = new AddResolver(
			a,
			new MultiplyResolver(new SubtractResolver(b, a), t0));

		var lerpAtOne = new AddResolver(
			a,
			new MultiplyResolver(new SubtractResolver(b, a), t1));

		var context = new GraphContext();

		lerpAtZero.Resolve(context).AsDouble().Should().Be(10.0);
		lerpAtOne.Resolve(context).AsDouble().Should().Be(30.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Absolute_distance_between_two_values()
	{
		// distance = Abs(a - b)
		// Abs(10 - 25) = Abs(-15) = 15
		var resolver = new AbsResolver(
			new SubtractResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				new VariantResolver(new Variant128(25), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(15);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Negate_used_to_subtract_via_addition()
	{
		// a + (-b) equivalent to a - b
		// 20 + (-8) = 12
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(20), typeof(int)),
			new NegateResolver(
				new VariantResolver(new Variant128(8), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(12);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Cycle_index_with_offset()
	{
		// cycleIndex = (frameCount + offset) % cycleLength
		// (17 + 3) % 6 = 20 % 6 = 2
		var resolver = new ModuloResolver(
			new AddResolver(
				new VariantResolver(new Variant128(17), typeof(int)),
				new VariantResolver(new Variant128(3), typeof(int))),
			new VariantResolver(new Variant128(6), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Even_odd_check_with_modulo_and_comparison()
	{
		// isEven = (value % 2) == 0
		// 42 % 2 = 0, so isEven = true
		var modulo = new ModuloResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		var resolver = new ComparisonResolver(
			modulo,
			ComparisonOperation.Equal,
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Mixed_int_and_float_arithmetic()
	{
		// (int 10 + float 2.5) * float 3.0 = float 12.5 * 3.0 = 37.5
		var resolver = new MultiplyResolver(
			new AddResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				new VariantResolver(new Variant128(2.5f), typeof(float))),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(37.5f);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Vector_midpoint_formula()
	{
		// midpoint = (a + b) / (2, 2, 2)
		// ((1,2,3) + (5,8,9)) / (2,2,2) = (6,10,12) / (2,2,2) = (3,5,6)
		var resolver = new DivideResolver(
			new AddResolver(
				new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
				new VariantResolver(new Variant128(new Vector3(5, 8, 9)), typeof(Vector3))),
			new VariantResolver(new Variant128(new Vector3(2, 2, 2)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(3, 5, 6));
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Vector_reflect_direction()
	{
		// Simplified reflect: reflected = -velocity + (2,2) * bounce
		// -(3,4) + (2,2) * (1,1) = (-3,-4) + (2,2) = (-1,-2)
		var resolver = new AddResolver(
			new NegateResolver(
				new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2))),
			new MultiplyResolver(
				new VariantResolver(new Variant128(new Vector2(2, 2)), typeof(Vector2)),
				new VariantResolver(new Variant128(new Vector2(1, 1)), typeof(Vector2))));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(-1, -2));
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Compound_formula_with_graph_variables()
	{
		// effectiveValue = (base + bonus) * multiplier - penalty
		// (50 + 20) * 2 - 15 = 140 - 15 = 125
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("base", 50);
		graph.VariableDefinitions.DefineVariable("bonus", 20);
		graph.VariableDefinitions.DefineVariable("multiplier", 2);
		graph.VariableDefinitions.DefineVariable("penalty", 15);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new SubtractResolver(
			new MultiplyResolver(
				new AddResolver(
					new VariableResolver("base", typeof(int)),
					new VariableResolver("bonus", typeof(int))),
				new VariableResolver("multiplier", typeof(int))),
			new VariableResolver("penalty", typeof(int)));

		resolver.Resolve(context).AsInt().Should().Be(125);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Compound_formula_reflects_variable_changes()
	{
		// result = (a + b) * c
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("a", 5);
		graph.VariableDefinitions.DefineVariable("b", 3);
		graph.VariableDefinitions.DefineVariable("c", 2);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new MultiplyResolver(
			new AddResolver(
				new VariableResolver("a", typeof(int)),
				new VariableResolver("b", typeof(int))),
			new VariableResolver("c", typeof(int)));

		// (5 + 3) * 2 = 16
		resolver.Resolve(context).AsInt().Should().Be(16);

		context.GraphVariables.SetVar("a", 10);
		context.GraphVariables.SetVar("c", 3);

		// (10 + 3) * 3 = 39
		resolver.Resolve(context).AsInt().Should().Be(39);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Health_below_threshold_after_damage()
	{
		// isDanger = (maxHealth - damageTaken) < threshold
		// (100 - 80) < 30 → 20 < 30 → true
		var resolver = new ComparisonResolver(
			new SubtractResolver(
				new VariantResolver(new Variant128(100), typeof(int)),
				new VariantResolver(new Variant128(80), typeof(int))),
			ComparisonOperation.LessThan,
			new VariantResolver(new Variant128(30), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Combined_stat_exceeds_requirement()
	{
		// meetsRequirement = (strength + dexterity) >= requiredTotal
		// (15 + 12) >= 25 → 27 >= 25 → true
		var resolver = new ComparisonResolver(
			new AddResolver(
				new VariantResolver(new Variant128(15), typeof(int)),
				new VariantResolver(new Variant128(12), typeof(int))),
			ComparisonOperation.GreaterThanOrEqual,
			new VariantResolver(new Variant128(25), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Quadratic_expression()
	{
		// f(x) = ax² + bx + c  where a=2, b=3, c=5, x=4
		// 2*16 + 3*4 + 5 = 32 + 12 + 5 = 49
		var a = new VariantResolver(new Variant128(2), typeof(int));
		var b = new VariantResolver(new Variant128(3), typeof(int));
		var c = new VariantResolver(new Variant128(5), typeof(int));
		var x = new VariantResolver(new Variant128(4), typeof(int));

		var xSquared = new MultiplyResolver(x, x);
		var ax2 = new MultiplyResolver(a, xSquared);
		var bx = new MultiplyResolver(b, x);

		var resolver = new AddResolver(
			new AddResolver(ax2, bx),
			c);

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(49);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Weighted_average_three_values()
	{
		// weightedAvg = (v1*w1 + v2*w2 + v3*w3) / (w1 + w2 + w3)
		// (80*2.0 + 90*3.0 + 70*1.0) / (2.0 + 3.0 + 1.0) = (160+270+70) / 6 = 500/6 ≈ 83.333
		var v1w1 = new MultiplyResolver(
			new VariantResolver(new Variant128(80.0), typeof(double)),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var v2w2 = new MultiplyResolver(
			new VariantResolver(new Variant128(90.0), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		var v3w3 = new MultiplyResolver(
			new VariantResolver(new Variant128(70.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var numerator = new AddResolver(
			new AddResolver(v1w1, v2w2),
			v3w3);

		var denominator = new AddResolver(
			new AddResolver(
				new VariantResolver(new Variant128(2.0), typeof(double)),
				new VariantResolver(new Variant128(3.0), typeof(double))),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var resolver = new DivideResolver(numerator, denominator);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(83.333, 0.01);
	}
}
