// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// A mutable bag of named <see cref="Variant128"/> values and <see cref="Variant128"/> arrays. Used for both per-graph
/// instance variables (<see cref="GraphContext.GraphVariables"/>) and entity-level shared variables
/// (<see cref="GraphContext.SharedVariables"/>).
/// </summary>
/// <remarks>
/// Variables hold raw values only, they are not resolvers and do not depend on external state. For read-only computed
/// properties (attributes, tags, comparisons) use <see cref="Properties.IPropertyResolver"/> on the graph's
/// <see cref="GraphVariableDefinitions"/> instead.
/// </remarks>
public class Variables
{
	private readonly Dictionary<StringKey, Variant128> _variables = [];

	private readonly Dictionary<StringKey, List<Variant128>> _arrays = [];

	/// <summary>
	/// Initializes the runtime variables from a <see cref="GraphVariableDefinitions"/> instance. For each variable
	/// definition, a fresh copy is created so that each graph execution has independent mutable state.
	/// </summary>
	/// <param name="definitions">The graph variable definitions to initialize from.</param>
	public void InitializeFrom(GraphVariableDefinitions definitions)
	{
		_variables.Clear();
		_arrays.Clear();

		foreach (VariableDefinition definition in definitions.VariableDefinitions)
		{
			_variables[definition.Name] = definition.InitialValue;
		}

		foreach (ArrayVariableDefinition definition in definitions.ArrayVariableDefinitions)
		{
			_arrays[definition.Name] = [.. definition.InitialValues];
		}
	}

	/// <summary>
	/// Tries to get the value of a variable with the given name.
	/// </summary>
	/// <typeparam name="T">The type to interpret the value as. Must be supported by <see cref="Variant128"/>.
	/// </typeparam>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="value">The value of the variable if it was found.</param>
	/// <returns><see langword="true"/> if the variable was found, <see langword="false"/> otherwise.</returns>
	public bool TryGetVar<T>(StringKey name, out T value)
		where T : unmanaged
	{
		value = default;

		if (!_variables.TryGetValue(name, out Variant128 stored))
		{
			return false;
		}

		value = stored.Get<T>();
		return true;
	}

	/// <summary>
	/// Tries to get the raw <see cref="Variant128"/> value of a variable with the given name.
	/// </summary>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="value">The raw variant value if the variable was found.</param>
	/// <returns><see langword="true"/> if the variable was found, <see langword="false"/> otherwise.</returns>
	public bool TryGetVariant(StringKey name, out Variant128 value)
	{
		return _variables.TryGetValue(name, out value);
	}

	/// <summary>
	/// Sets the value of a variable with the given name.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if no variable with this name exists.</exception>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void SetVar<T>(StringKey name, T value)
	{
		if (!_variables.ContainsKey(name))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no variable with this name exists.");
		}

		_variables[name] = CreateVariant(value);
	}

	/// <summary>
	/// Sets the raw <see cref="Variant128"/> value of a variable with the given name.
	/// </summary>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The raw variant value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if no variable with this name exists.</exception>
	public void SetVariant(StringKey name, Variant128 value)
	{
		if (!_variables.ContainsKey(name))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no variable with this name exists.");
		}

		_variables[name] = value;
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
		_variables[name] = CreateVariant(value);
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

		if (!_arrays.TryGetValue(name, out List<Variant128>? array))
		{
			return false;
		}

		if (index < 0 || index >= array.Count)
		{
			return false;
		}

		value = array[index].Get<T>();
		return true;
	}

	/// <summary>
	/// Gets the raw <see cref="Variant128"/> element at the specified index from an array variable.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <param name="value">The raw variant value of the element if found.</param>
	/// <returns><see langword="true"/> if the array variable was found and the index was valid, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGetArrayVariant(StringKey name, int index, out Variant128 value)
	{
		value = default;

		if (!_arrays.TryGetValue(name, out List<Variant128>? array))
		{
			return false;
		}

		if (index < 0 || index >= array.Count)
		{
			return false;
		}

		value = array[index];
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
		if (!_arrays.TryGetValue(name, out List<Variant128>? array))
		{
			throw new InvalidOperationException(
				$"Cannot set array element for '{name}': no array variable with this name exists.");
		}

		array[index] = CreateVariant(value);
	}

	/// <summary>
	/// Gets the length of an array variable.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <returns>The number of elements in the array, or -1 if the variable does not exist or is not an array.
	/// </returns>
	public int GetArrayLength(StringKey name)
	{
		if (!_arrays.TryGetValue(name, out List<Variant128>? array))
		{
			return -1;
		}

		return array.Count;
	}

	/// <summary>
	/// Creates a <see cref="Variant128"/> from a typed value.
	/// </summary>
	/// <typeparam name="T">The type of the value. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="Variant128"/> containing the value.</returns>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	internal static Variant128 CreateVariant<T>(T value)
	{
		return value switch
		{
			bool @bool => new Variant128(@bool),
			byte @byte => new Variant128(@byte),
			sbyte @sbyte => new Variant128(@sbyte),
			char @char => new Variant128(@char),
			decimal @decimal => new Variant128(@decimal),
			double @double => new Variant128(@double),
			float @float => new Variant128(@float),
			int @int => new Variant128(@int),
			uint @uint => new Variant128(@uint),
			long @long => new Variant128(@long),
			ulong @ulong => new Variant128(@ulong),
			short @short => new Variant128(@short),
			ushort @ushort => new Variant128(@ushort),
			Vector2 vector2 => new Variant128(vector2),
			Vector3 vector3 => new Variant128(vector3),
			Vector4 vector4 => new Variant128(vector4),
			Plane plane => new Variant128(plane),
			Quaternion quaternion => new Variant128(quaternion),
			_ => throw new ArgumentException($"{typeof(T)} is not supported by Variant128"),
		};
	}
}
