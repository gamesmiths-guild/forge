// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class VectorFromValuesResolverTests
{
	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_two_components_value_type_is_vector2()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_three_components_value_type_is_vector3()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_four_components_value_type_is_vector4()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)),
			new VariantResolver(new Variant128(4.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector4));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_creates_vector2()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.5f), typeof(float)),
			new VariantResolver(new Variant128(-2.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(new Vector2(1.5f, -2.0f));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_creates_vector3()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(1.0f, 2.0f, 3.0f));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_creates_vector4()
	{
		var resolver = new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)),
			new VariantResolver(new Variant128(4.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector4().Should().Be(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_supports_nested_component_resolvers()
	{
		var resolver = new VectorFromValuesResolver(
			new AddResolver(
				new VariantResolver(new Variant128(1.0f), typeof(float)),
				new VariantResolver(new Variant128(2.0f), typeof(float))),
			new MultiplyResolver(
				new VariantResolver(new Variant128(3.0f), typeof(float)),
				new VariantResolver(new Variant128(4.0f), typeof(float))),
			new NegateResolver(
				new VariantResolver(new Variant128(5.0f), typeof(float))));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(3.0f, 12.0f, -5.0f));
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_throws_for_non_float_x_component()
	{
#pragma warning disable CA1806
		Action act = () => new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_throws_for_non_float_z_component()
	{
#pragma warning disable CA1806
		Action act = () => new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "VectorFromValues")]
	public void VectorFromValues_resolver_throws_for_non_float_w_component()
	{
#pragma warning disable CA1806
		Action act = () => new VectorFromValuesResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)),
			new VariantResolver(new Variant128(4.0), typeof(double)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
