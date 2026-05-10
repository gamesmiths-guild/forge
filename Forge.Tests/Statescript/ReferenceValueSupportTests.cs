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

public class ReferenceValueSupportTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Statescript", "ReferenceValues")]
	public void Variables_initialize_reference_variables_and_arrays_from_definitions()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var definitions = new GraphVariableDefinitions();

		definitions.DefineReferenceVariable<IForgeEntity>("selected", entity1);
		definitions.DefineReferenceArrayVariable<IForgeEntity>("entities", entity1, entity2);

		var variables = new Variables();
		variables.InitializeFrom(definitions);

		variables.TryGetReference("selected", out IForgeEntity? selected).Should().BeTrue();
		selected.Should().BeSameAs(entity1);

		variables.TryGetReferenceArray("entities", out IForgeEntity?[]? entities).Should().BeTrue();
		entities.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Statescript", "ReferenceValues")]
	public void Graph_context_resolves_reference_properties_and_arrays()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var graph = new Graph();
		var readSelectedEntity = new ReadReferencePropertyNode<IForgeEntity>();
		var readEntityGroup = new ReadReferenceArrayPropertyNode<IForgeEntity>();

		graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("selected");
		graph.VariableDefinitions.DefineReferenceArrayVariable<IForgeEntity>("entities", entity1, entity2);
		graph.VariableDefinitions.DefineReferenceProperty("selectedEntity", new EntityVariableResolver("selected"));
		graph.VariableDefinitions.DefineReferenceArrayProperty(
			"entityGroup",
			new ReferenceArrayVariableResolver<IForgeEntity>("entities"));
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
		processor.StartGraph(variables => variables.SetReference("selected", entity2));

		readSelectedEntity.LastReadValue.Should().BeSameAs(entity2);
		readEntityGroup.LastReadArray.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Statescript", "ReferenceValues")]
	public void Graph_variable_definitions_validate_reference_types()
	{
		var graph = new Graph();

		graph.VariableDefinitions.DefineReferenceProperty("owner", new OwnerEntityResolver());
		graph.VariableDefinitions.DefineReferenceArrayVariable<IForgeEntity>("entities");

		graph.VariableDefinitions.ValidatePropertyType("owner", typeof(IForgeEntity)).Should().BeTrue();
		graph.VariableDefinitions.ValidatePropertyType("entities", typeof(IForgeEntity[])).Should().BeTrue();
		graph.VariableDefinitions.ValidatePropertyType("owner", typeof(string)).Should().BeFalse();
	}
}
