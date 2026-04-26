// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ModuloResolverTests
{
	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_int_mod_int_value_type_is_int()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_int_mod_double_promotes_to_double()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_computes_int_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_computes_double_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(10.5), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.5, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_computes_float_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(7.5f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.5f);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_computes_long_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(17L), typeof(long)),
			new VariantResolver(new Variant128(5L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(2L);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_computes_decimal_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(10.5m), typeof(decimal)),
			new VariantResolver(new Variant128(3.0m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(1.5m);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_zero_remainder()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(12), typeof(int)),
			new VariantResolver(new Variant128(4), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_negative_dividend()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128(-10), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		// C# % follows truncation toward zero: -10 % 3 = -1
		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_byte_mod_short_promotes_to_int()
	{
		var resolver = new ModuloResolver(
			new VariantResolver(new Variant128((byte)25), typeof(byte)),
			new VariantResolver(new Variant128((short)7), typeof(short)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(4);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_supports_nesting()
	{
		// (17 % 5) % 2 = 2 % 2 = 0
		var inner = new ModuloResolver(
			new VariantResolver(new Variant128(17), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		var outer = new ModuloResolver(
			inner,
			new VariantResolver(new Variant128(2), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_throws_for_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ModuloResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_throws_for_quaternion_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ModuloResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Modulo")]
	public void Modulo_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ModuloResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
