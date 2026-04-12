// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SharedVariableResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_reads_value_from_shared_variables()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.SharedVariables.DefineVariable("abilityLock", true);

		var resolver = new SharedVariableResolver("abilityLock", typeof(bool));

		var context = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_returns_default_when_shared_variables_is_null()
	{
		var resolver = new SharedVariableResolver("abilityLock", typeof(double));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_returns_default_for_missing_variable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new SharedVariableResolver("nonexistent", typeof(double));

		var context = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context).AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_value_type_is_double()
	{
		var resolver = new SharedVariableResolver("anything", typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_reflects_changes_across_graph_contexts()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.SharedVariables.DefineVariable("sharedCounter", 0);

		var resolver = new SharedVariableResolver("sharedCounter", typeof(int));

		var context1 = new GraphContext { SharedVariables = entity.SharedVariables };
		var context2 = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context1).AsInt().Should().Be(0);

		entity.SharedVariables.SetVar("sharedCounter", 42);

		resolver.Resolve(context1).AsInt().Should().Be(42);
		resolver.Resolve(context2).AsInt().Should().Be(42);
	}
}
