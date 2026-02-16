// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents the runtime state of variables and properties during a graph execution.
/// </summary>
public class Variables
{
	private readonly Dictionary<StringKey, IPropertyResolver> _propertyResolvers = [];

	/// <summary>
	/// Initializes the runtime resolvers from a <see cref="GraphVariableDefinitions"/> instance. For each variable
	/// definition (backed by a <see cref="VariantResolver"/>), a fresh resolver is created with the initial value so
	/// that each graph execution has independent mutable state. Read-only property resolvers are shared directly since
	/// they carry no mutable state.
	/// </summary>
	/// <param name="definitions">The graph variable definitions to initialize from.</param>
	public void InitializeFrom(GraphVariableDefinitions definitions)
	{
		_propertyResolvers.Clear();

		foreach (PropertyDefinition definition in definitions.Definitions)
		{
			switch (definition.Resolver)
			{
				case VariantResolver variantResolver:
					_propertyResolvers[definition.Name] =
						new VariantResolver(variantResolver.Value, variantResolver.ValueType);
					break;

				case ArrayVariableResolver arrayResolver:
					var copy = new Variant128[arrayResolver.Length];
					for (var i = 0; i < arrayResolver.Length; i++)
					{
						copy[i] = arrayResolver.GetElement(i);
					}

					_propertyResolvers[definition.Name] =
						new ArrayVariableResolver(copy, arrayResolver.ValueType);
					break;

				default:
					_propertyResolvers[definition.Name] = definition.Resolver;
					break;
			}
		}
	}

	/// <summary>
	/// Tries to get the resolved value of a variable or property with the given name. Variables return their stored
	/// value; properties are resolved on demand from external sources.
	/// </summary>
	/// <typeparam name="T">The type to interpret the value as. Must be supported by <see cref="Variant128"/>.
	/// </typeparam>
	/// <param name="name">The name of the variable or property to get.</param>
	/// <param name="graphContext">The graph context used by resolvers to access external state.</param>
	/// <param name="value">The resolved value if the entry was found.</param>
	/// <returns><see langword="true"/> if the entry was found and resolved successfully, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGet<T>(StringKey name, GraphContext graphContext, out T value)
		where T : unmanaged
	{
		value = default;

		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			return false;
		}

		Variant128 resolved = resolver.Resolve(graphContext);
		value = resolved.Get<T>();

		return true;
	}

	/// <summary>
	/// Tries to get the resolved value of a variable or property as a raw <see cref="Variant128"/>.
	/// </summary>
	/// <param name="name">The name of the variable or property to get.</param>
	/// <param name="graphContext">The graph context used by resolvers to access external state.</param>
	/// <param name="value">The resolved value if the entry was found.</param>
	/// <returns><see langword="true"/> if the entry was found and resolved successfully, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGetVariant(StringKey name, GraphContext graphContext, out Variant128 value)
	{
		value = default;

		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			return false;
		}

		value = resolver.Resolve(graphContext);
		return true;
	}

	/// <summary>
	/// Tries to get the value of a mutable variable with the given name.
	/// </summary>
	/// <remarks>
	/// This is a convenience overload for variables
	/// (backed by <see cref="VariantResolver"/>) that don't need the graph context for resolution.
	/// </remarks>
	/// <typeparam name="T">The type of the variable to get. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="value">The value of the variable if it was found.</param>
	/// <returns><see langword="true"/> if the variable was found and retrieved successfully, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGetVar<T>(StringKey name, out T value)
		where T : unmanaged
	{
		value = default;

		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			return false;
		}

		Variant128 resolved = resolver.Resolve(null!);
		value = resolved.Get<T>();

		return true;
	}

	/// <summary>
	/// Sets the value of a mutable variable with the given name. Only entries backed by a <see cref="VariantResolver"/>
	/// can be set; attempting to set a read-only property will throw.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The value to set the variable to.</param>
	/// <returns><see langword="true"/> if the variable was set successfully.</returns>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	/// <exception cref="InvalidOperationException">Thrown if the name refers to a read-only property.</exception>
	public bool SetVar<T>(StringKey name, T value)
	{
		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no variable or property with this name exists.");
		}

		if (resolver is not VariantResolver variableResolver)
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': it is a read-only property. Only variables can be set at runtime.");
		}

		variableResolver.Set(value);
		return true;
	}

	/// <summary>
	/// Sets the raw <see cref="Variant128"/> value of a mutable variable with the given name.
	/// </summary>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The raw variant value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if the name does not exist or refers to a read-only property.
	/// </exception>
	public void SetVariant(StringKey name, Variant128 value)
	{
		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no variable or property with this name exists.");
		}

		if (resolver is not VariantResolver variableResolver)
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': it is a read-only property. Only variables can be set at runtime.");
		}

		variableResolver.Value = value;
	}

	/// <summary>
	/// Defines a new mutable variable directly in this <see cref="Variables"/> bag. If a variable with the given name
	/// already exists, its value is updated instead.
	/// </summary>
	/// <remarks>
	/// This is intended for entity-level shared variables that are not defined through a graph's
	/// <see cref="GraphVariableDefinitions"/>. For graph instance variables, use
	/// <see cref="InitializeFrom(GraphVariableDefinitions)"/> instead.
	/// </remarks>
	/// <typeparam name="T">The type of the variable value. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable.</param>
	/// <param name="value">The initial value of the variable.</param>
	public void DefineVariable<T>(StringKey name, T value)
	{
		Variant128 variant = VariantResolver.CreateVariant(value);

		if (_propertyResolvers.TryGetValue(name, out IPropertyResolver? existing)
			&& existing is VariantResolver variantResolver)
		{
			variantResolver.Value = variant;
			return;
		}

		_propertyResolvers[name] = new VariantResolver(variant, typeof(T));
	}

	/// <summary>
	/// Gets the element at the specified index from an array variable.
	/// </summary>
	/// <typeparam name="T">The type to interpret the element as. Must be supported by <see cref="Variant128"/>.
	/// </typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <param name="value">The value of the element if found.</param>
	/// <returns><see langword="true"/> if the array variable was found and the index was valid, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGetArrayElement<T>(StringKey name, int index, out T value)
		where T : unmanaged
	{
		value = default;

		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			return false;
		}

		if (resolver is not ArrayVariableResolver arrayResolver)
		{
			return false;
		}

		if (index < 0 || index >= arrayResolver.Length)
		{
			return false;
		}

		value = arrayResolver.GetElement(index).Get<T>();
		return true;
	}

	/// <summary>
	/// Sets the element at the specified index in an array variable.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="index">The zero-based index of the element to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if the name does not exist or is not an array variable.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
	public void SetArrayElement<T>(StringKey name, int index, T value)
	{
		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			throw new InvalidOperationException(
				$"Cannot set array element for '{name}': no variable with this name exists.");
		}

		if (resolver is not ArrayVariableResolver arrayResolver)
		{
			throw new InvalidOperationException(
				$"Cannot set array element for '{name}': it is not an array variable.");
		}

		Variant128 variant = VariantResolver.CreateVariant(value);
		arrayResolver.SetElement(index, variant);
	}

	/// <summary>
	/// Gets the length of an array variable.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <returns>The number of elements in the array, or -1 if the variable does not exist or is not an array.
	/// </returns>
	public int GetArrayLength(StringKey name)
	{
		if (!_propertyResolvers.TryGetValue(name, out IPropertyResolver? resolver))
		{
			return -1;
		}

		if (resolver is not ArrayVariableResolver arrayResolver)
		{
			return -1;
		}

		return arrayResolver.Length;
	}
}
