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
	private readonly List<PropertyDefinition> _propertyDefinitions = [];

	private readonly List<ObjectPropertyDefinition> _objectPropertyDefinitions = [];

	private readonly List<ArrayPropertyDefinition> _arrayPropertyDefinitions = [];

	private readonly List<ObjectArrayPropertyDefinition> _objectArrayPropertyDefinitions = [];

	/// <summary>
	/// Gets the list of variable definitions for the graph.
	/// </summary>
	public List<VariableDefinition> VariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the list of object-backed variable definitions for the graph.
	/// </summary>
	public List<ObjectVariableDefinition> ObjectVariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the list of array variable definitions for the graph.
	/// </summary>
	public List<ArrayVariableDefinition> ArrayVariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the list of object-backed array variable definitions for the graph.
	/// </summary>
	public List<ObjectArrayVariableDefinition> ObjectArrayVariableDefinitions { get; } = [];

	/// <summary>
	/// Gets the property definitions for the graph, in definition order. Properties are read-only computed values
	/// resolved on demand from external sources (attributes, tags, comparisons, etc.).
	/// </summary>
	/// <remarks>
	/// Read-only because a property is also indexed by name, and the index is what every lookup consults. Add one with
	/// <see cref="DefineProperty"/>, which writes both.
	/// </remarks>
	public IReadOnlyList<PropertyDefinition> PropertyDefinitions => _propertyDefinitions;

	/// <summary>
	/// Gets the object-backed property definitions for the graph, in definition order.
	/// </summary>
	/// <remarks>
	/// Read-only for the same reason <see cref="PropertyDefinitions"/> is. Add one with
	/// <see cref="DefineObjectProperty"/>.
	/// </remarks>
	public IReadOnlyList<ObjectPropertyDefinition> ObjectPropertyDefinitions => _objectPropertyDefinitions;

	/// <summary>
	/// Gets the array property definitions for the graph, in definition order. Array properties are read-only computed
	/// values that resolve to arrays (e.g., a list of entity IDs within a radius).
	/// </summary>
	/// <remarks>
	/// Read-only for the same reason <see cref="PropertyDefinitions"/> is. Add one with
	/// <see cref="DefineArrayProperty"/>.
	/// </remarks>
	public IReadOnlyList<ArrayPropertyDefinition> ArrayPropertyDefinitions => _arrayPropertyDefinitions;

	/// <summary>
	/// Gets the object-backed array property definitions for the graph, in definition order.
	/// </summary>
	/// <remarks>
	/// Read-only for the same reason <see cref="PropertyDefinitions"/> is. Add one with
	/// <see cref="DefineObjectArrayProperty"/>.
	/// </remarks>
	public IReadOnlyList<ObjectArrayPropertyDefinition> ObjectArrayPropertyDefinitions =>
		_objectArrayPropertyDefinitions;

	internal Dictionary<StringKey, PropertyDefinition> PropertiesByName { get; } = [];

	internal Dictionary<StringKey, ObjectPropertyDefinition> ObjectPropertiesByName { get; } = [];

	internal Dictionary<StringKey, ArrayPropertyDefinition> ArrayPropertiesByName { get; } = [];

	internal Dictionary<StringKey, ObjectArrayPropertyDefinition> ObjectArrayPropertiesByName { get; } = [];

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
	/// Adds a mutable object-backed variable definition with the specified name and initial value.
	/// </summary>
	/// <typeparam name="T">The type of the initial value stored in the object-backed lane.</typeparam>
	/// <param name="name">The name of the variable.</param>
	/// <param name="initialValue">The initial value of the variable.</param>
	public void DefineObjectVariable<T>(StringKey name, T initialValue = default!)
	{
		ObjectVariableDefinitions.Add(new ObjectVariableDefinition(name, initialValue, typeof(T)));
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
		for (int i = 0; i < initialValues.Length; i++)
		{
			variants[i] = Variables.CreateVariant(initialValues[i]);
		}

		ArrayVariableDefinitions.Add(new ArrayVariableDefinition(name, variants, typeof(T)));
	}

	/// <summary>
	/// Adds a mutable object-backed array variable definition with the specified name and initial values.
	/// </summary>
	/// <typeparam name="T">The type of each element stored in the object-backed lane.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="initialValues">The initial values for the array elements.</param>
	public void DefineObjectArrayVariable<T>(StringKey name, params T[] initialValues)
	{
		ObjectArrayVariableDefinitions.Add(
			new ObjectArrayVariableDefinition(name, [.. initialValues.Cast<object?>()], typeof(T)));
	}

	/// <summary>
	/// Adds a read-only property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="resolver">The resolver used to compute the property's value at runtime.</param>
	public void DefineProperty(StringKey name, IPropertyResolver resolver)
	{
		var definition = new PropertyDefinition(name, resolver);
		_propertyDefinitions.Add(definition);
		PropertiesByName.TryAdd(name, definition);
	}

	/// <summary>
	/// Adds a read-only object-backed property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="resolver">The resolver used to compute the property's value at runtime.</param>
	public void DefineObjectProperty(StringKey name, IObjectResolver resolver)
	{
		var definition = new ObjectPropertyDefinition(name, resolver);
		_objectPropertyDefinitions.Add(definition);
		ObjectPropertiesByName.TryAdd(name, definition);
	}

	/// <summary>
	/// Adds a read-only array property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the array property.</param>
	/// <param name="resolver">The resolver used to compute the property's array value at runtime.</param>
	public void DefineArrayProperty(StringKey name, IArrayPropertyResolver resolver)
	{
		var definition = new ArrayPropertyDefinition(name, resolver);
		_arrayPropertyDefinitions.Add(definition);
		ArrayPropertiesByName.TryAdd(name, definition);
	}

	/// <summary>
	/// Adds a read-only object-backed array property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the array property.</param>
	/// <param name="resolver">The resolver used to compute the property's array value at runtime.</param>
	public void DefineObjectArrayProperty(StringKey name, IObjectArrayResolver resolver)
	{
		var definition = new ObjectArrayPropertyDefinition(name, resolver);
		_objectArrayPropertyDefinitions.Add(definition);
		ObjectArrayPropertiesByName.TryAdd(name, definition);
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
		// Through the same indexes the resolve paths use, so validation and resolution can never disagree about which
		// definition a name refers to.
		if (PropertiesByName.TryGetValue(name, out PropertyDefinition property))
		{
			return expectedType.IsAssignableFrom(property.Resolver.ValueType);
		}

		if (ObjectPropertiesByName.TryGetValue(name, out ObjectPropertyDefinition objectProperty))
		{
			return expectedType.IsAssignableFrom(objectProperty.Resolver.ValueType);
		}

		if (ArrayPropertiesByName.TryGetValue(name, out ArrayPropertyDefinition arrayProperty))
		{
			// expectedType should be an array type (e.g., typeof(int[])), and element type must match
			if (expectedType.IsArray && expectedType.GetElementType() is Type elementType)
			{
				return elementType.IsAssignableFrom(arrayProperty.Resolver.ElementType);
			}

			return false;
		}

		if (ObjectArrayPropertiesByName.TryGetValue(name, out ObjectArrayPropertyDefinition objectArrayProperty))
		{
			if (expectedType.IsArray && expectedType.GetElementType() is Type objectElementType)
			{
				return objectElementType.IsAssignableFrom(objectArrayProperty.Resolver.ElementType);
			}

			return false;
		}

		foreach (VariableDefinition definition in VariableDefinitions)
		{
			if (definition.Name == name)
			{
				return expectedType.IsAssignableFrom(definition.ValueType);
			}
		}

		foreach (ObjectVariableDefinition definition in ObjectVariableDefinitions)
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
				if (expectedType.IsArray && expectedType.GetElementType() is Type elementType)
				{
					return elementType.IsAssignableFrom(definition.ElementType);
				}

				return false;
			}
		}

		foreach (ObjectArrayVariableDefinition definition in ObjectArrayVariableDefinitions)
		{
			if (definition.Name == name)
			{
				if (expectedType.IsArray && expectedType.GetElementType() is Type elementType)
				{
					return elementType.IsAssignableFrom(definition.ElementType);
				}

				return false;
			}
		}

		return false;
	}
}

/// <summary>
/// Represents a named array property definition with a resolver.
/// </summary>
/// <param name="Name">The name of the array property.</param>
/// <param name="Resolver">The resolver used to provide the property's array value at runtime.</param>
public readonly record struct ArrayPropertyDefinition(StringKey Name, IArrayPropertyResolver Resolver);

/// <summary>
/// Represents a named object-backed array property definition with a resolver.
/// </summary>
/// <param name="Name">The name of the array property.</param>
/// <param name="Resolver">The resolver used to provide the property's array value at runtime.</param>
public readonly record struct ObjectArrayPropertyDefinition(StringKey Name, IObjectArrayResolver Resolver);

/// <summary>
/// Represents the definition of a graph property, including its name and the resolver used to compute its value at
/// runtime. This is immutable definition data that belongs to the graph.
/// </summary>
/// <param name="Name">The name of the property, used as the lookup key at runtime.</param>
/// <param name="Resolver">The resolver used to provide the property's value at runtime.</param>
public readonly record struct PropertyDefinition(StringKey Name, IPropertyResolver Resolver);

/// <summary>
/// Represents the definition of a graph object-backed property, including its name and resolver.
/// </summary>
/// <param name="Name">The name of the property, used as the lookup key at runtime.</param>
/// <param name="Resolver">The resolver used to provide the property's value at runtime.</param>
public readonly record struct ObjectPropertyDefinition(StringKey Name, IObjectResolver Resolver);

/// <summary>
/// Represents a named variable definition with an initial value.
/// </summary>
/// <param name="Name">The name of the variable.</param>
/// <param name="InitialValue">The initial value of the variable.</param>
/// <param name="ValueType">The type of the variable's value.</param>
public readonly record struct VariableDefinition(StringKey Name, Variant128 InitialValue, Type ValueType);

/// <summary>
/// Represents a named object-backed variable definition with an initial value.
/// </summary>
/// <param name="Name">The name of the variable.</param>
/// <param name="InitialValue">The initial value of the variable.</param>
/// <param name="ValueType">The type of the variable's value.</param>
public readonly record struct ObjectVariableDefinition(StringKey Name, object? InitialValue, Type ValueType);

/// <summary>
/// Represents a named array variable definition with initial values.
/// </summary>
/// <param name="Name">The name of the array variable.</param>
/// <param name="InitialValues">The initial values for the array elements.</param>
/// <param name="ElementType">The type of each element in the array.</param>
public readonly record struct ArrayVariableDefinition(StringKey Name, Variant128[] InitialValues, Type ElementType);

/// <summary>
/// Represents a named object-backed array variable definition with initial values.
/// </summary>
/// <param name="Name">The name of the array variable.</param>
/// <param name="InitialValues">The initial values for the array elements.</param>
/// <param name="ElementType">The type of each element in the array.</param>
public readonly record struct ObjectArrayVariableDefinition(StringKey Name, object?[] InitialValues, Type ElementType);
