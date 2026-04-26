// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ProjectResolverTests
{
	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_vector3_projects_onto_axis()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(new Vector3(2, 2, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(2, 0, 0));
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_returns_zero_when_projecting_perpendicular_vector()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_returns_zero_when_projecting_zero_vector()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(Vector2.Zero);
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_returns_zero_when_projecting_onto_zero_vector()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(new Vector2(3.0f, 4.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(Vector2.Zero);
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_supports_vector4_projection()
	{
		var resolver = new ProjectResolver(
			new VariantResolver(new Variant128(new Vector4(2.0f, 4.0f, 6.0f, 8.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(1.0f, 0.0f, 0.0f, 0.0f)), typeof(Vector4)));

		resolver.Resolve(new GraphContext()).AsVector4().Should().Be(new Vector4(2.0f, 0.0f, 0.0f, 0.0f));
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_throws_for_mismatched_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ProjectResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Project")]
	public void Project_resolver_throws_for_non_vector_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ProjectResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
