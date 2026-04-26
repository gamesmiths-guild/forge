// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

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
		// (80*2.0 + 90*3.0 + 70*1.0) / (2.0 + 3.0 + 1.0) = (160+270+70) / 6 = 500/6 ≈ 83.33333
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

		resolver.Resolve(context).AsDouble().Should().BeApproximately(83.33333, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Clamped_health_after_damage()
	{
		// clampedHealth = Clamp(currentHealth - damage, 0, maxHealth)
		// Clamp(100 - 150, 0, 100) = Clamp(-50, 0, 100) = 0
		var resolver = new ClampResolver(
			new SubtractResolver(
				new VariantResolver(new Variant128(100), typeof(int)),
				new VariantResolver(new Variant128(150), typeof(int))),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(100), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Clamped_health_after_heal()
	{
		// clampedHealth = Clamp(currentHealth + heal, 0, maxHealth)
		// Clamp(80 + 50, 0, 100) = Clamp(130, 0, 100) = 100
		var resolver = new ClampResolver(
			new AddResolver(
				new VariantResolver(new Variant128(80), typeof(int)),
				new VariantResolver(new Variant128(50), typeof(int))),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(100), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(100);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Min_max_to_clamp_stat()
	{
		// Clamp implemented as Max(min, Min(value, max))
		// Max(0, Min(150, 100)) = Max(0, 100) = 100
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(0), typeof(int)),
			new MinResolver(
				new VariantResolver(new Variant128(150), typeof(int)),
				new VariantResolver(new Variant128(100), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(100);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Lerp_for_smooth_transition()
	{
		// Smoothly transition between walk and run speed
		// Lerp(walkSpeed, runSpeed, t) = Lerp(3.0, 8.0, 0.7) = 3 + (8 - 3) * 0.7 = 3 + 3.5 = 6.5
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(3.0f), typeof(float)),
			new VariantResolver(new Variant128(8.0f), typeof(float)),
			new VariantResolver(new Variant128(0.7f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(6.5f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Clamped_lerp_for_safe_interpolation()
	{
		// Safe lerp: Lerp(a, b, Clamp(t, 0, 1))
		// Lerp(0, 100, Clamp(1.5, 0, 1)) = Lerp(0, 100, 1.0) = 100
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(100.0f), typeof(float)),
			new ClampResolver(
				new VariantResolver(new Variant128(1.5f), typeof(float)),
				new VariantResolver(new Variant128(0.0f), typeof(float)),
				new VariantResolver(new Variant128(1.0f), typeof(float))));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(100.0f);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Distance_formula_with_sqrt()
	{
		// distance = Sqrt((x2-x1)^2 + (y2-y1)^2)
		// Sqrt((4-1)^2 + (5-1)^2) = Sqrt(9 + 16) = Sqrt(25) = 5
		var dx = new SubtractResolver(
			new VariantResolver(new Variant128(4.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var dy = new SubtractResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var resolver = new SqrtResolver(
			new AddResolver(
				new PowResolver(dx, new VariantResolver(new Variant128(2.0), typeof(double))),
				new PowResolver(dy, new VariantResolver(new Variant128(2.0), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Exponential_decay_formula()
	{
		// decay = initialValue * Pow(decayRate, time)
		// 100 * Pow(0.5, 3) = 100 * 0.125 = 12.5
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(100.0), typeof(double)),
			new PowResolver(
				new VariantResolver(new Variant128(0.5), typeof(double)),
				new VariantResolver(new Variant128(3.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(12.5);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Floor_for_grid_snapping()
	{
		// gridX = Floor(worldX / cellSize) * cellSize
		// Floor(7.8 / 2.0) * 2.0 = Floor(3.9) * 2.0 = 3.0 * 2.0 = 6.0
		var resolver = new MultiplyResolver(
			new FloorResolver(
				new DivideResolver(
					new VariantResolver(new Variant128(7.8), typeof(double)),
					new VariantResolver(new Variant128(2.0), typeof(double)))),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(6.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Ceil_for_resource_cost_rounding_up()
	{
		// requiredItems = Ceil(totalCost / costPerItem)
		// Ceil(10.0 / 3.0) = Ceil(3.333...) = 4.0
		var resolver = new CeilResolver(
			new DivideResolver(
				new VariantResolver(new Variant128(10.0), typeof(double)),
				new VariantResolver(new Variant128(3.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(4.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Truncate_for_integer_conversion()
	{
		// Convert float position to grid coordinate (always toward zero)
		// Truncate(-7.8 / 2.0) = Truncate(-3.9) = -3.0
		var resolver = new TruncateResolver(
			new DivideResolver(
				new VariantResolver(new Variant128(-7.8), typeof(double)),
				new VariantResolver(new Variant128(2.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-3.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Round_for_display_value()
	{
		// displayValue = Round(rawValue * 100) / 100 — but using Round directly
		// Round(3.14159 * 1.0) = Round(3.14159) = 3.0
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.14159), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Effective_stat_with_min_cap()
	{
		// effectiveStat = Max(baseStat + modifier, minimumStat)
		// Max(10 + (-15), 1) = Max(-5, 1) = 1
		var resolver = new MaxResolver(
			new AddResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				new VariantResolver(new Variant128(-15), typeof(int))),
			new VariantResolver(new Variant128(1), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Damage_with_crit_floor()
	{
		// critDamage = Floor(baseDamage * critMultiplier)
		// Floor(47.0 * 1.5) = Floor(70.5) = 70.0
		var resolver = new FloorResolver(
			new MultiplyResolver(
				new VariantResolver(new Variant128(47.0), typeof(double)),
				new VariantResolver(new Variant128(1.5), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(70.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Level_scaling_with_sqrt()
	{
		// scaledStat = baseStat + Floor(Sqrt(level) * growthRate)
		// 10 + Floor(Sqrt(25.0) * 3.0) = 10 + Floor(5.0 * 3.0) = 10 + Floor(15.0) = 10 + 15.0 = 25.0
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new FloorResolver(
				new MultiplyResolver(
					new SqrtResolver(
						new VariantResolver(new Variant128(25.0), typeof(double))),
					new VariantResolver(new Variant128(3.0), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(25.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Experience_curve_with_pow()
	{
		// xpRequired = Floor(baseXP * Pow(level, exponent))
		// Floor(100.0 * Pow(5.0, 1.5)) = Floor(100.0 * 11.1803...) = Floor(1118.03...) = 1118.0
		var resolver = new FloorResolver(
			new MultiplyResolver(
				new VariantResolver(new Variant128(100.0), typeof(double)),
				new PowResolver(
					new VariantResolver(new Variant128(5.0), typeof(double)),
					new VariantResolver(new Variant128(1.5), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(1118.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Vector3_lerp_for_position_interpolation()
	{
		// Smoothly move between two positions at t=0.25
		// Lerp((0,0,0), (8,4,12), 0.25) = (2, 1, 3)
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector3(0, 0, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(8, 4, 12)), typeof(Vector3)),
			new VariantResolver(new Variant128(0.25f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(2, 1, 3));
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Vector2_lerp_for_ui_animation()
	{
		// Animate a UI element from one position to another
		// Lerp((10, 200), (300, 50), 0.5) = (155, 125)
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector2(10, 200)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(300, 50)), typeof(Vector2)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(155, 125));
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Sine_wave_oscillation()
	{
		// amplitude * Sin(frequency * time)
		// 10.0 * Sin(2.0 * π/2) = 10.0 * Sin(π) ≈ 10.0 * 0.0 = 0.0
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new SinResolver(
				new MultiplyResolver(
					new VariantResolver(new Variant128(2.0), typeof(double)),
					new VariantResolver(new Variant128(Math.PI / 2.0), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Angle_between_two_points_with_atan2()
	{
		// angle = ATan2(dy, dx)
		// ATan2(3 - 0, 4 - 0) = ATan2(3, 4) ≈ 0.6435 radians
		var resolver = new ATan2Resolver(
			new SubtractResolver(
				new VariantResolver(new Variant128(3.0), typeof(double)),
				new VariantResolver(new Variant128(0.0), typeof(double))),
			new SubtractResolver(
				new VariantResolver(new Variant128(4.0), typeof(double)),
				new VariantResolver(new Variant128(0.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.6435, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Exponential_growth_with_exp()
	{
		// population = initial * Exp(rate * time)
		// 100 * Exp(0.1 * 10) = 100 * Exp(1) = 100 * e ≈ 271.83
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(100.0), typeof(double)),
			new ExpResolver(
				new MultiplyResolver(
					new VariantResolver(new Variant128(0.1), typeof(double)),
					new VariantResolver(new Variant128(10.0), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(100.0 * Math.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Logarithmic_scaling_for_difficulty()
	{
		// difficulty = baseValue + scaleFactor * Log(level)
		// 10.0 + 5.0 * Log(e^2) = 10.0 + 5.0 * 2.0 = 20.0
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new MultiplyResolver(
				new VariantResolver(new Variant128(5.0), typeof(double)),
				new LogResolver(
					new VariantResolver(new Variant128(Math.E * Math.E), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(20.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Bit_depth_calculation_with_log2()
	{
		// bitsNeeded = Ceil(Log2(numValues))
		// Ceil(Log2(1000)) = Ceil(9.9658) = 10.0
		var resolver = new CeilResolver(
			new Log2Resolver(
				new VariantResolver(new Variant128(1000.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(10.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Decibel_calculation_with_log10()
	{
		// decibels = 20 * Log10(amplitude / reference)
		// 20 * Log10(100 / 1) = 20 * Log10(100) = 20 * 2 = 40
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(20.0), typeof(double)),
			new Log10Resolver(
				new DivideResolver(
					new VariantResolver(new Variant128(100.0), typeof(double)),
					new VariantResolver(new Variant128(1.0), typeof(double)))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(40.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Smooth_step_approximation_with_tanh()
	{
		// smoothValue = (TanH(x) + 1) / 2 — maps (-∞,+∞) to (0, 1)
		// (TanH(0) + 1) / 2 = (0 + 1) / 2 = 0.5
		var resolver = new DivideResolver(
			new AddResolver(
				new TanHResolver(
					new VariantResolver(new Variant128(0.0), typeof(double))),
				new VariantResolver(new Variant128(1.0), typeof(double))),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.5, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Direction_sign_with_copysign()
	{
		// Apply direction sign to speed: CopySign(speed, direction)
		// CopySign(Abs(-7.5), -1.0) = CopySign(7.5, -1.0) = -7.5
		var resolver = new CopySignResolver(
			new AbsResolver(
				new VariantResolver(new Variant128(-7.5), typeof(double))),
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-7.5);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Cube_root_for_volume_to_side_length()
	{
		// sideLength = Cbrt(volume)
		// Cbrt(27) = 3
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(27.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(3.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Sign_for_movement_direction()
	{
		// direction = Sign(targetX - currentX)
		// Sign(10 - 25) = Sign(-15) = -1
		var resolver = new SignResolver(
			new SubtractResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				new VariantResolver(new Variant128(25), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Pythagorean_identity_sin_squared_plus_cos_squared()
	{
		// Sin²(x) + Cos²(x) = 1 for any x
		var angle = new VariantResolver(new Variant128(0.7), typeof(double));

		var resolver = new AddResolver(
			new PowResolver(
				new SinResolver(angle),
				new VariantResolver(new Variant128(2.0), typeof(double))),
			new PowResolver(
				new CosResolver(angle),
				new VariantResolver(new Variant128(2.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Exp_and_log_are_inverses()
	{
		// Log(Exp(x)) = x
		// Log(Exp(3.5)) = 3.5
		var resolver = new LogResolver(
			new ExpResolver(
				new VariantResolver(new Variant128(3.5), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(3.5, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Trig_with_degrees_using_DegToRad()
	{
		// Sin(DegToRad(30)) = Sin(π/6) = 0.5
		var resolver = new SinResolver(
			new DegToRadResolver(
				new VariantResolver(new Variant128(30.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.5, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void RadToDeg_after_ATan2_for_aiming_angle()
	{
		// angle = RadToDeg(ATan2(1, 1)) = RadToDeg(π/4) = 45°
		var resolver = new RadToDegResolver(
			new ATan2Resolver(
				new VariantResolver(new Variant128(1.0), typeof(double)),
				new VariantResolver(new Variant128(1.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(45.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Pi_resolver_in_circle_area_formula()
	{
		// area = Pi * r^2
		// Pi * 5^2 = Pi * 25 ≈ 78.5398
		var resolver = new MultiplyResolver(
			new PiResolver(),
			new PowResolver(
				new VariantResolver(new Variant128(5.0), typeof(double)),
				new VariantResolver(new Variant128(2.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI * 25.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void E_resolver_in_natural_exponential()
	{
		// Pow(e, 2) = e^2 ≈ 7.389
		var resolver = new PowResolver(
			new EResolver(),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.E * Math.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Random_damage_range()
	{
		// damage = baseDamage + Random(0, bonusRange)
		// With fixed random (nextSingle=0.5): 50 + Random(0, 20) = 50 + 10 = 60
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(50.0f), typeof(float)),
			new RandomResolver(
				new FixedRandom(nextSingle: 0.5f),
				new VariantResolver(new Variant128(0.0f), typeof(float)),
				new VariantResolver(new Variant128(20.0f), typeof(float))));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(60.0f);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Round_with_digits_for_display_value()
	{
		// Display damage per second with 1 decimal place
		// Round(totalDamage / time, 1) = Round(100.0 / 3.0, 1) = Round(33.333, 1) = 33.3
		var resolver = new RoundResolver(
			new DivideResolver(
				new VariantResolver(new Variant128(100.0), typeof(double)),
				new VariantResolver(new Variant128(3.0), typeof(double))),
			digits: 1);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(33.3);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void Round_away_from_zero_for_score_display()
	{
		// Round score to nearest 0.5 increment using AwayFromZero
		// Round(7.5, AwayFromZero) = 8 (not 8 with ToEven, but here the midpoint rounds up)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(7.5), typeof(double)),
			mode: MidpointRounding.AwayFromZero);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(8.0);
	}

	[Fact]
	[Trait("Resolver", "MathComposition")]
	public void DegToRad_and_RadToDeg_roundtrip()
	{
		// RadToDeg(DegToRad(135)) = 135
		var resolver = new RadToDegResolver(
			new DegToRadResolver(
				new VariantResolver(new Variant128(135.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(135.0, TestUtils.Tolerance);
	}
}
