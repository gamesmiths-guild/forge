// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public class ObjectValueSupportTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Statescript", "ObjectValues")]
	public void Variables_initialize_object_variables_and_arrays_from_definitions()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var definitions = new GraphVariableDefinitions();

		definitions.DefineObjectVariable<IForgeEntity>("selected", entity1);
		definitions.DefineObjectArrayVariable<IForgeEntity>("entities", entity1, entity2);

		var variables = new Variables();
		variables.InitializeFrom(definitions);

		variables.TryGetObject("selected", out IForgeEntity? selected).Should().BeTrue();
		selected.Should().BeSameAs(entity1);

		variables.TryGetObjectArray("entities", out IForgeEntity?[]? entities).Should().BeTrue();
		entities.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Statescript", "ObjectValues")]
	public void Graph_context_resolves_object_properties_and_arrays()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var graph = new Graph();
		var readSelectedEntity = new ReadObjectPropertyNode<IForgeEntity>();
		var readEntityGroup = new ReadObjectArrayPropertyNode<IForgeEntity>();

		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("selected");
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("entities", entity1, entity2);
		graph.VariableDefinitions.DefineObjectProperty("selectedEntity", new EntityVariableResolver("selected"));
		graph.VariableDefinitions.DefineObjectArrayProperty(
			"entityGroup",
			new ObjectArrayVariableResolver<IForgeEntity>("entities"));
		readSelectedEntity.BindInput(0, "selectedEntity");
		readEntityGroup.BindInput(0, "entityGroup");
		graph.AddNode(readSelectedEntity);
		graph.AddNode(readEntityGroup);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			readSelectedEntity.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			readSelectedEntity.OutputPorts[ActionNode.OutputPort],
			readEntityGroup.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph(variables => variables.SetObject("selected", entity2));

		readSelectedEntity.LastReadValue.Should().BeSameAs(entity2);
		readEntityGroup.LastReadArray.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Statescript", "ObjectValues")]
	public void Graph_variable_definitions_validate_object_types()
	{
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty("owner", new AbilityOwnerResolver());
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("entities");

		graph.VariableDefinitions.ValidatePropertyType("owner", typeof(IForgeEntity)).Should().BeTrue();
		graph.VariableDefinitions.ValidatePropertyType("entities", typeof(IForgeEntity[])).Should().BeTrue();
		graph.VariableDefinitions.ValidatePropertyType("owner", typeof(string)).Should().BeFalse();
	}
}
