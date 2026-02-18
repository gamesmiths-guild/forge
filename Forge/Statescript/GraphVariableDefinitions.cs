// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Defines the schema of variables and properties for a graph. This is immutable definition data that belongs to the
/// <see cref="Graph"/>. When a graph execution starts, the variable definitions are used to initialize a runtime
/// <see cref="Variables"/> instance placed into the <see cref="GraphContext"/>. Properties are resolved on demand
/// through the graph's property definitions.
/// </summary>
public class GraphVariableDefinitions
{
	/// <summary>
	/// Gets the list of variable definitions for the graph.
	/// </summary>
	public List<VariableDefinition> VariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the list of array variable definitions for the graph.
	/// </summary>
	public List<ArrayVariableDefinition> ArrayVariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the list of property definitions for the graph. Properties are read-only computed values resolved on demand
	/// from external sources (attributes, tags, comparisons, etc.).
	/// </summary>
	public List<PropertyDefinition> PropertyDefinitions { get; } = [];

	/// <summary>
	/// Adds a mutable variable definition with the specified name and initial value.
	/// </summary>
	/// <typeparam name="T">The type of the initial value. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable.</param>
	/// <param name="initialValue">The initial value of the variable.</param>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void DefineVariable<T>(StringKey name, T initialValue)
	{
		Variant128 variant = Variables.CreateVariant(initialValue);
		VariableDefinitions.Add(new VariableDefinition(name, variant, typeof(T)));
	}

	/// <summary>
	/// Adds a mutable array variable definition with the specified name and initial values.
	/// </summary>
	/// <typeparam name="T">The type of each element. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="initialValues">The initial values for the array elements.</param>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void DefineArrayVariable<T>(StringKey name, params T[] initialValues)
	{
		var variants = new Variant128[initialValues.Length];
		for (var i = 0; i < initialValues.Length; i++)
		{
			variants[i] = Variables.CreateVariant(initialValues[i]);
		}

		ArrayVariableDefinitions.Add(new ArrayVariableDefinition(name, variants, typeof(T)));
	}

	/// <summary>
	/// Adds a read-only property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="resolver">The resolver used to compute the property's value at runtime.</param>
	public void DefineProperty(StringKey name, IPropertyResolver resolver)
	{
		PropertyDefinitions.Add(new PropertyDefinition(name, resolver));
	}

	/// <summary>
	/// Validates that the variable or property with the specified name produces a value assignable to the expected
	/// type. This should be called at graph construction time to catch type mismatches early.
	/// </summary>
	/// <param name="name">The name of the variable or property to validate.</param>
	/// <param name="expectedType">The expected value type.</param>
	/// <returns><see langword="true"/> if the entry exists and its value type is assignable to the expected type;
	/// <see langword="false"/> otherwise.</returns>
	public bool ValidatePropertyType(StringKey name, Type expectedType)
	{
		foreach (PropertyDefinition definition in PropertyDefinitions)
		{
			if (definition.Name == name)
			{
				return expectedType.IsAssignableFrom(definition.Resolver.ValueType);
			}
		}

		foreach (VariableDefinition definition in VariableDefinitions)
		{
			if (definition.Name == name)
			{
				return expectedType.IsAssignableFrom(definition.ValueType);
			}
		}

		foreach (ArrayVariableDefinition definition in ArrayVariableDefinitions)
		{
			if (definition.Name == name)
			{
				return expectedType.IsAssignableFrom(definition.ElementType);
			}
		}

		return false;
	}
}

/// <summary>
/// Represents a named variable definition with an initial value.
/// </summary>
/// <param name="Name">The name of the variable.</param>
/// <param name="InitialValue">The initial value of the variable.</param>
/// <param name="ValueType">The type of the variable's value.</param>
public readonly record struct VariableDefinition(StringKey Name, Variant128 InitialValue, Type ValueType);

/// <summary>
/// Represents a named array variable definition with initial values.
/// </summary>
/// <param name="Name">The name of the array variable.</param>
/// <param name="InitialValues">The initial values for the array elements.</param>
/// <param name="ElementType">The type of each element in the array.</param>
public readonly record struct ArrayVariableDefinition(StringKey Name, Variant128[] InitialValues, Type ElementType);
