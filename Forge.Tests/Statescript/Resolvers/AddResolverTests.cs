// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AddResolverTests
{
	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_int_plus_int_value_type_is_int()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_float_plus_float_value_type_is_float()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_double_plus_double_value_type_is_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_vector2_plus_vector2_value_type_is_vector2()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_vector3_plus_vector3_value_type_is_vector3()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_vector4_plus_vector4_value_type_is_vector4()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)));

		resolver.ValueType.Should().Be(typeof(Vector4));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_quaternion_plus_quaternion_value_type_is_quaternion()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_int_plus_float_promotes_to_float()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_int_plus_double_promotes_to_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_float_plus_double_promotes_to_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_byte_plus_short_promotes_to_int()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((byte)1), typeof(byte)),
			new VariantResolver(new Variant128((short)2), typeof(short)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_int_plus_uint_promotes_to_long()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2u), typeof(uint)));

		resolver.ValueType.Should().Be(typeof(long));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_long_plus_long_value_type_is_long()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1L), typeof(long)),
			new VariantResolver(new Variant128(2L), typeof(long)));

		resolver.ValueType.Should().Be(typeof(long));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_ulong_plus_int_promotes_to_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1UL), typeof(ulong)),
			new VariantResolver(new Variant128(2), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_decimal_plus_int_promotes_to_decimal()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)),
			new VariantResolver(new Variant128(2), typeof(int)));

		resolver.ValueType.Should().Be(typeof(decimal));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_ints()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(20), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_floats()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.5f), typeof(float)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_doubles()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.5), typeof(double)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(4.0);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_longs()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(100L), typeof(long)),
			new VariantResolver(new Variant128(200L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(300L);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_decimals()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.1m), typeof(decimal)),
			new VariantResolver(new Variant128(2.2m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(3.3m);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_bytes()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((byte)10), typeof(byte)),
			new VariantResolver(new Variant128((byte)20), typeof(byte)));

		var context = new GraphContext();

		// byte + byte promotes to int
		resolver.Resolve(context).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_shorts()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((short)100), typeof(short)),
			new VariantResolver(new Variant128((short)200), typeof(short)));

		var context = new GraphContext();

		// short + short promotes to int
		resolver.Resolve(context).AsInt().Should().Be(300);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_sbytes()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((sbyte)10), typeof(sbyte)),
			new VariantResolver(new Variant128((sbyte)20), typeof(sbyte)));

		var context = new GraphContext();

		// sbyte + sbyte promotes to int
		resolver.Resolve(context).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_ushorts()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((ushort)100), typeof(ushort)),
			new VariantResolver(new Variant128((ushort)200), typeof(ushort)));

		var context = new GraphContext();

		// ushort + ushort promotes to int
		resolver.Resolve(context).AsInt().Should().Be(300);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_uints()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(100u), typeof(uint)),
			new VariantResolver(new Variant128(200u), typeof(uint)));

		var context = new GraphContext();

		// uint + uint → long (promotion to avoid overflow)
		resolver.Resolve(context).AsLong().Should().Be(300L);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_ulongs()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(100UL), typeof(ulong)),
			new VariantResolver(new Variant128(200UL), typeof(ulong)));

		var context = new GraphContext();

		// ulong + ulong → double (promotion since there's no wider integer type)
		resolver.Resolve(context).AsDouble().Should().Be(300.0);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_int_and_float()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(12.5f);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_float_and_int()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(2.5f), typeof(float)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(12.5f);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_int_and_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(12.5);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_float_and_double()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(1.5f), typeof(float)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_byte_and_int()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((byte)5), typeof(byte)),
			new VariantResolver(new Variant128(100), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(105);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_short_and_long()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128((short)5), typeof(short)),
			new VariantResolver(new Variant128(1000L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(1005L);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_int_and_decimal()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(12.5m);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_vector2()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(4, 6));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_vector3()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4, 5, 6)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(5, 7, 9));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_vector4()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(5, 6, 7, 8)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(6, 8, 10, 12));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adds_two_quaternions()
	{
		var left = new Quaternion(1, 2, 3, 4);
		var right = new Quaternion(5, 6, 7, 8);

		var resolver = new AddResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();

		Quaternion result = resolver.Resolve(context).AsQuaternion();
		Quaternion expected = left + right;

		result.Should().Be(expected);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_handles_negative_ints()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(-3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_handles_negative_result()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(-10.0), typeof(double)),
			new VariantResolver(new Variant128(-5.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-15.0);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adding_zero_returns_same_value()
	{
		var resolver = new AddResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_adding_zero_vector_returns_same_vector()
	{
		var original = new Vector3(1, 2, 3);

		var resolver = new AddResolver(
			new VariantResolver(new Variant128(original), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(original);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_supports_nesting()
	{
		// (10 + 20) + 30 = 60
		var innerAdd = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(20), typeof(int)));

		var outerAdd = new AddResolver(
			innerAdd,
			new VariantResolver(new Variant128(30), typeof(int)));

		var context = new GraphContext();

		outerAdd.Resolve(context).AsInt().Should().Be(60);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_nests_with_type_promotion()
	{
		// (int 10 + float 2.5f) + double 1.0 = double 13.5
		var innerAdd = new AddResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		innerAdd.ValueType.Should().Be(typeof(float));

		var outerAdd = new AddResolver(
			innerAdd,
			new VariantResolver(new Variant128(1.0), typeof(double)));

		outerAdd.ValueType.Should().Be(typeof(double));

		var context = new GraphContext();

		outerAdd.Resolve(context).AsDouble().Should().BeApproximately(13.5, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_nests_vector_additions()
	{
		// (1,0) + (0,1) + (2,3) = (3,4)
		var innerAdd = new AddResolver(
			new VariantResolver(new Variant128(new Vector2(1, 0)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(0, 1)), typeof(Vector2)));

		var outerAdd = new AddResolver(
			innerAdd,
			new VariantResolver(new Variant128(new Vector2(2, 3)), typeof(Vector2)));

		var context = new GraphContext();

		outerAdd.Resolve(context).AsVector2().Should().Be(new Vector2(3, 4));
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_works_with_variable_resolver()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("speed", 5.0);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new AddResolver(
			new VariableResolver("speed", typeof(double)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		resolver.Resolve(context).AsDouble().Should().Be(7.5);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_reflects_runtime_variable_changes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("base", 10.0);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new AddResolver(
			new VariableResolver("base", typeof(double)),
			new VariantResolver(new Variant128(5.0), typeof(double)));

		resolver.Resolve(context).AsDouble().Should().Be(15.0);

		context.GraphVariables.SetVar("base", 20.0);

		resolver.Resolve(context).AsDouble().Should().Be(25.0);
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_composes_with_comparison_resolver()
	{
		// (3 + 4) > 5 → true
		var addResolver = new AddResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(4), typeof(int)));

		var comparisonResolver = new ComparisonResolver(
			addResolver,
			ComparisonOperation.GreaterThan,
			new VariantResolver(new Variant128(5), typeof(int)));

		var context = new GraphContext();

		comparisonResolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AddResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>()
			.WithMessage("*bool*");
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_throws_for_mismatched_vector_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AddResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>()
			.WithMessage("*vector*");
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_throws_for_vector_and_numeric_mix()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AddResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>()
			.WithMessage("*vector*");
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_throws_for_char_operands()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AddResolver(
			new VariantResolver(new Variant128('A'), typeof(char)),
			new VariantResolver(new Variant128('B'), typeof(char)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>()
			.WithMessage("*char*");
	}

	[Fact]
	[Trait("Resolver", "Add")]
	public void Add_resolver_throws_for_quaternion_and_vector_mix()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AddResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>()
			.WithMessage("*vector*");
	}
}
