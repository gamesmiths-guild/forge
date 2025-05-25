// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// <para>A collection of attributes grouped together for cohesive management. Use this to organize attributes that
/// naturally belong together, such as character stats, item bonuses, environment properties, skill modifiers, weapon
/// stats, or vehicle parameters.</para>
/// <para>To use this class you should declare <see langword="public"/> (and ideally <see langword="readonly"/>) fields
/// or properties of type <see cref="EntityAttribute"/> for each attribute you want to create and then initialize them withing
/// the constructor using <see cref="InitializeAttribute"/> calls for each attribute passing <see langword="nameof"/>
/// for the attributeName paremeter like so:</para>
/// <code>
/// Health = InitializeAttribute(nameof(Health), 100, 0, 100);
/// </code>
/// </summary>
/// <remarks>
/// Attribute sets are particularly useful for grouping primary and secondary attributes, such as pairing MaxHealth and
/// Health, allowing logic like clamping Health to MaxHealth to be centralized within the set.
/// </remarks>
public abstract class AttributeSet
{
	/// <summary>
	/// Gets retrieves a dictionary mapping each attribute to its unique <see cref="StringKey"/>.
	/// </summary>
	/// <remarks>
	/// Keys follow the format '{AttributeSetClassName}.{AttributeFieldName}', where <c>AttributeSetClassName</c> is the
	/// name of the class extending <see cref="AttributeSet"/> and <c>AttributeFieldName</c> is the name of the
	/// <see cref="EntityAttribute"/> field defined within that class.
	/// </remarks>
	public Dictionary<StringKey, EntityAttribute> AttributesMap { get; } = [];

	/// <summary>
	/// Adds a given value to the attribute's <see cref="EntityAttribute.BaseValue"/> value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="value">The value to be added.</param>
	protected static void AddToAttributeBaseValue(EntityAttribute attribute, int value)
	{
		attribute.ExecuteFlatModifier(value);
	}

	/// <summary>
	/// Sets the attribute's <see cref="EntityAttribute.BaseValue"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="newValue">The value to be set.</param>
	protected static void SetAttributeBaseValue(EntityAttribute attribute, int newValue)
	{
		attribute.ExecuteOverride(newValue);
	}

	/// <summary>
	/// Sets the attribute's <see cref="EntityAttribute.Min"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="minValue">The value to be set.</param>
	protected static void SetAttributeMinValue(EntityAttribute attribute, int minValue)
	{
		attribute.SetMinValue(minValue);
	}

	/// <summary>
	/// Sets the attribute's <see cref="EntityAttribute.Max"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="maxValue">The value to be set.</param>
	protected static void SetAttributeMaxValue(EntityAttribute attribute, int maxValue)
	{
		attribute.SetMaxValue(maxValue);
	}

	/// <summary>
	/// Initializes an attribute with the given parameters while taking care of properly registering it into the
	/// <see cref="AttributesMap"/> and all the necessary events for the framework to function properly.
	/// </summary>
	/// <param name="attributeName">The name of the attribute, should typically be the name of the property or field
	/// registered in the attribute set passed with <see langword="nameof"/>.</param>
	/// <param name="defaultValue">The default initial value for the initialized attribute.</param>
	/// <param name="minValue">The minimum value for the initialized attribute.</param>
	/// <param name="maxValue">The maximum value for the initialized attribute.</param>
	/// <param name="channels">The number of channels for the initialized attribute.</param>
	/// <returns>A correctly initialized <see cref="EntityAttribute"/>.</returns>
	protected EntityAttribute InitializeAttribute(
		string attributeName,
		int defaultValue,
		int minValue = int.MinValue,
		int maxValue = int.MaxValue,
		int channels = 1)
	{
		Debug.Assert(!string.IsNullOrEmpty(attributeName), "attributeName should never be null or empty.");

		var attribute = new EntityAttribute(defaultValue, minValue, maxValue, channels);
		AttributesMap.Add($"{GetType().Name}.{attributeName}", attribute);
		attribute.OnValueChanged += AttributeOnValueChanged;
		return attribute;
	}

	/// <summary>
	/// Called whenever there's a change in the value of the attribute's <see cref="EntityAttribute.CurrentValue"/>.
	/// </summary>
	/// <param name="attribute">The attribute receiving changes.</param>
	/// <param name="change">The change magnitude.</param>
	protected virtual void AttributeOnValueChanged(EntityAttribute attribute, int change)
	{
	}

	/// <summary>
	/// Executes just before a <see cref="Effects.Effect"/> execution.
	/// </summary>
	protected virtual void PreEffectExecute()
	{
	}

	/// <summary>
	/// Executes just after a <see cref="Effects.Effect"/> execution.
	/// </summary>
	protected virtual void PostEffectExecute()
	{
	}
}
