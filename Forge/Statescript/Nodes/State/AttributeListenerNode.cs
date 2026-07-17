// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that listens for value changes on an entity attribute while active, emitting an event with the new
/// value and the change delta.
/// </summary>
/// <remarks>
/// <para>The entity input selects whose attribute is observed, defaulting to the ability context's owner when
/// unbound. The attribute is identified by its fully qualified key, configured on the node.</para>
/// <para>On each change the node writes the New Value and Delta output variables (<see langword="int"/>), then emits
/// <see cref="OnChangedPort"/>. The handler emits synchronously from the attribute change.</para>
/// <para>The node stays active until deactivated externally, unsubscribing on deactivation.</para>
/// </remarks>
/// <param name="attributeKey">The fully qualified key of the attribute to observe.</param>
public class AttributeListenerNode(StringKey attributeKey) : StateNode<AttributeListenerNodeContext>
{
	/// <summary>
	/// Input property index for the entity whose attribute is observed.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Output variable index for the attribute's new current value.
	/// </summary>
	public const byte NewValueOutput = 0;

	/// <summary>
	/// Output variable index for the change delta.
	/// </summary>
	public const byte DeltaOutput = 1;

	/// <summary>
	/// Output port index for the per-change signal.
	/// </summary>
	public const byte OnChangedPort = 4;

	private readonly StringKey _attributeKey = attributeKey;

	/// <inheritdoc/>
	public override string Description =>
		"Listens for attribute value changes while active and emits OnChanged with the new value and delta.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnChangedPort, "OnChanged"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("New Value", typeof(int)));
		outputVariables.Add(new OutputVariable("Delta", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		AttributeListenerNodeContext nodeContext = graphContext.GetNodeContext<AttributeListenerNodeContext>(NodeID);
		nodeContext.SubscribedAttribute = null;
		nodeContext.Handler = null;

		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity?.Attributes.ContainsAttribute(_attributeKey) != true)
		{
			return;
		}

		EntityAttribute attribute = entity.Attributes[_attributeKey];

		void Handler(EntityAttribute changedAttribute, int change)
		{
			OnAttributeChanged(graphContext, changedAttribute, change);
		}

		nodeContext.SubscribedAttribute = attribute;
		nodeContext.Handler = Handler;

		attribute.OnValueChanged += Handler;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		AttributeListenerNodeContext nodeContext = graphContext.GetNodeContext<AttributeListenerNodeContext>(NodeID);

		if (nodeContext.SubscribedAttribute is not null && nodeContext.Handler is not null)
		{
			nodeContext.SubscribedAttribute.OnValueChanged -= nodeContext.Handler;
		}

		nodeContext.SubscribedAttribute = null;
		nodeContext.Handler = null;
	}

	private static void WriteIntOutput(GraphContext graphContext, OutputVariable output, int value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetVar(output.BoundName, value);
	}

	private void OnAttributeChanged(GraphContext graphContext, EntityAttribute attribute, int change)
	{
		if (!graphContext.HasNodeContext(NodeID)
			|| !graphContext.GetNodeContext<AttributeListenerNodeContext>(NodeID).Active)
		{
			return;
		}

		WriteIntOutput(graphContext, OutputVariables[NewValueOutput], attribute.CurrentValue);
		WriteIntOutput(graphContext, OutputVariables[DeltaOutput], change);

		OutputPorts[OnChangedPort].EmitMessage(graphContext);
	}
}
