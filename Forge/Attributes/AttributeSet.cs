// Copyright © 2024 Gamesmiths Guild.

using System.Reflection;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// <para>A collection of attributes grouped together for cohesive management. Use this to organize attributes that naturally
/// belong together, such as character stats, item bonuses, environment properties, skill modifiers, weapon stats, or
/// vehicle parameters.</para>
/// <para>To use this class you should declare <see langword="public"/> (and ideally <see langword="readonly"/>) fields
/// of type <see cref="Attribute"/> for each attribute you want to create and then initialize them withing the
/// <see cref="InitializeAttributes"/> method using <see cref="InitializeAttribute"/> calls for each attribute.</para>
/// </summary>
/// <remarks>
/// Particularly useful for grouping primary and secondary attributes, such as pairing MaxHealth and Health, allowing
/// logic like clamping Health to MaxHealth to be centralized within the set.
/// </remarks>
public abstract class AttributeSet
{
	/// <summary>
	/// Use this method to initialize all attributes with <see cref="InitializeAttribute"/> passing the appropriate
	/// min, max and initial values.
	/// </summary>
	protected abstract void InitializeAttributes();

	/// <summary>
	/// Gets retrieves a dictionary mapping each attribute to its unique <see cref="StringKey"/>.
	/// </summary>
	/// <remarks>
	/// Keys follow the format '{AttributeSetClassName}.{AttributeFieldName}', where <c>AttributeSetClassName</c> is the
	/// name of the class extending <see cref="AttributeSet"/> and <c>AttributeFieldName</c> is the name of the
	/// <see cref="Attribute"/> field defined within that class.
	/// </remarks>
	public Dictionary<StringKey, Attribute> AttributesMap { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AttributeSet"/> class.
	/// </summary>
	protected AttributeSet()
	{
		// Fetch fields of type Attribute
		IEnumerable<FieldInfo> fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.FieldType == typeof(Attribute));

		AttributesMap = new Dictionary<StringKey, Attribute>(fields.Count());

		foreach (FieldInfo? field in fields)
		{
			var attributeName = $"{GetType().Name}.{field.Name}";

			// Create an instance of the Attribute class
			if (Activator.CreateInstance(field.FieldType, true) is Attribute attributeInstance)
			{
				// Set the value using the field
				field.SetValue(this, attributeInstance);

				AttributesMap.Add(attributeName, attributeInstance);
				attributeInstance.OnValueChanged += AttributeOnValueChanged;
			}
		}

#pragma warning disable S1699 // Constructors should only call non-overridable methods
		InitializeAttributes();
#pragma warning restore S1699 // Constructors should only call non-overridable methods
	}

	/// <summary>
	/// Initializes attributes with the given values.
	/// </summary>
	/// <param name="attribute">Which <see cref="Attribute" /> to initialize.</param>
	/// <param name="defaultValue">The attribute's default initial value.</param>
	/// <param name="minValue">The attribute's min value.</param>
	/// <param name="maxValue">The attribute's max value.</param>
	/// <param name="channels">How many channels are supported for this attribute.</param>
	protected static void InitializeAttribute(
		Attribute attribute,
		int defaultValue,
		int minValue = int.MinValue,
		int maxValue = int.MaxValue,
		int channels = 1)
	{
		attribute.Initialize(defaultValue, minValue, maxValue, channels);
	}

	/// <summary>
	/// Adds a given value to the attribute's <see cref="Attribute.BaseValue"/> value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="value">The value to be added.</param>
	protected static void AddToAttributeBaseValue(Attribute attribute, int value)
	{
		attribute.ExecuteFlatModifier(value);
	}

	/// <summary>
	/// Sets the attribute's <see cref="Attribute.BaseValue"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="newValue">The value to be set.</param>
	protected static void SetAttributeBaseValue(Attribute attribute, int newValue)
	{
		attribute.ExecuteOverride(newValue);
	}

	/// <summary>
	/// Sets the attribute's <see cref="Attribute.Min"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="minValue">The value to be set.</param>
	protected static void SetAttributeMinValue(Attribute attribute, int minValue)
	{
		attribute.SetMinValue(minValue);
	}

	/// <summary>
	/// Sets the attribute's <see cref="Attribute.Max"/> to the given value.
	/// </summary>
	/// <param name="attribute">The target attribute.</param>
	/// <param name="maxValue">The value to be set.</param>
	protected static void SetAttributeMaxValue(Attribute attribute, int maxValue)
	{
		attribute.SetMaxValue(maxValue);
	}

	/// <summary>
	/// Called whenever there's a change in the value of the attribute's <see cref="Attribute.CurrentValue"/>.
	/// </summary>
	/// <param name="attribute">The attribute receiving changes.</param>
	/// <param name="change">The change magnitude.</param>
	protected virtual void AttributeOnValueChanged(Attribute attribute, int change)
	{
	}

	/// <summary>
	/// Executes just before a Gameplay Effect execution.
	/// </summary>
	protected virtual void PreGameplayEffectExecute()
	{
	}

	/// <summary>
	/// Executes just after a Gameplay Effect execution.
	/// </summary>
	protected virtual void PostGameplayEffectExecute()
	{
	}
}
