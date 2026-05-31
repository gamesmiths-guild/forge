// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// A mutable bag of named value variables and object-backed variables. This includes <see cref="Variant128"/> values,
/// <see cref="Variant128"/> arrays, typed object-backed values, and typed object-backed arrays. Used for both per-graph
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

	private readonly Dictionary<StringKey, ObjectVariableStorage> _objectVariables = [];

	private readonly Dictionary<StringKey, ObjectArrayStorage> _objectArrays = [];

	/// <summary>
	/// Initializes the runtime variables from a <see cref="GraphVariableDefinitions"/> instance. For each variable
	/// definition, a fresh copy is created so that each graph execution has independent mutable state.
	/// </summary>
	/// <param name="definitions">The graph variable definitions to initialize from.</param>
	public void InitializeFrom(GraphVariableDefinitions definitions)
	{
		_variables.Clear();
		_arrays.Clear();
		_objectVariables.Clear();
		_objectArrays.Clear();

		foreach (VariableDefinition definition in definitions.VariableDefinitions)
		{
			_variables[definition.Name] = definition.InitialValue;
		}

		foreach (ArrayVariableDefinition definition in definitions.ArrayVariableDefinitions)
		{
			_arrays[definition.Name] = [.. definition.InitialValues];
		}

		foreach (ObjectVariableDefinition definition in definitions.ObjectVariableDefinitions)
		{
			_objectVariables[definition.Name] =
				new ObjectVariableStorage(definition.ValueType, definition.InitialValue);
		}

		foreach (ObjectArrayVariableDefinition definition in definitions.ObjectArrayVariableDefinitions)
		{
			_objectArrays[definition.Name] =
				new ObjectArrayStorage(definition.ElementType, [.. definition.InitialValues]);
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
	/// Tries to get the declared type of an object-backed variable with the given name.
	/// </summary>
	/// <param name="name">The name of the variable to inspect.</param>
	/// <param name="valueType">The declared type if the object-backed variable exists.</param>
	/// <returns><see langword="true"/> if the object-backed variable exists; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGetObjectVariableType(StringKey name, [NotNullWhen(true)] out Type? valueType)
	{
		if (_objectVariables.TryGetValue(name, out ObjectVariableStorage stored))
		{
			valueType = stored.ValueType;
			return true;
		}

		valueType = null;
		return false;
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
	/// Defines a new mutable variable directly in this <see cref="Variables"/> bag using a raw <see cref="Variant128"/>
	/// value. If a variable with the given name already exists, its value is updated instead.
	/// </summary>
	/// <remarks>
	/// This overload is useful when the value has already been converted to a <see cref="Variant128"/> (e.g., from a
	/// serialized source). For typed values, prefer <see cref="DefineVariable{T}(StringKey, T)"/> instead.
	/// </remarks>
	/// <param name="name">The name of the variable.</param>
	/// <param name="value">The raw variant value.</param>
	public void DefineVariable(StringKey name, Variant128 value)
	{
		_variables[name] = value;
	}

	/// <summary>
	/// Tries to get the typed object-backed value of a variable with the given name.
	/// </summary>
	/// <typeparam name="T">The type to interpret the value as.</typeparam>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="value">The value of the variable if it was found and the declared type is compatible.</param>
	/// <returns><see langword="true"/> if the variable was found and the requested type is compatible;
	/// <see langword="false"/> otherwise.</returns>
	public bool TryGetObject<T>(StringKey name, [MaybeNullWhen(false)] out T value)
	{
		value = default!;

		if (!_objectVariables.TryGetValue(name, out ObjectVariableStorage stored))
		{
			return false;
		}

		if (!typeof(T).IsAssignableFrom(stored.ValueType))
		{
			return false;
		}

		value = (T)stored.Value!;
		return true;
	}

	/// <summary>
	/// Tries to get the object-backed value of a variable with the given name using a runtime type.
	/// </summary>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="expectedType">The expected object-backed type.</param>
	/// <param name="value">The stored value if the variable exists and its declared type is compatible.</param>
	/// <returns><see langword="true"/> if the variable was found and the requested type is compatible;
	/// <see langword="false"/> otherwise.</returns>
	public bool TryGetObject(StringKey name, Type expectedType, out object? value)
	{
		value = null;

		if (!_objectVariables.TryGetValue(name, out ObjectVariableStorage stored))
		{
			return false;
		}

		if (!expectedType.IsAssignableFrom(stored.ValueType))
		{
			return false;
		}

		value = stored.Value;
		return true;
	}

	/// <summary>
	/// Sets the value of an object-backed variable with the given name.
	/// </summary>
	/// <typeparam name="T">The type of the value to set.</typeparam>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if no variable with this name exists or if the declared type
	/// of the variable is not compatible with the value type.</exception>
	public void SetObject<T>(StringKey name, T value)
	{
		if (!_objectVariables.TryGetValue(name, out ObjectVariableStorage stored))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no object variable with this name exists.");
		}

		if (value is not null && !stored.ValueType.IsInstanceOfType(value))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}' with value type {value.GetType()}: variable expects values assignable to " +
				$"{stored.ValueType}.");
		}

		_objectVariables[name] = new ObjectVariableStorage(stored.ValueType, value);
	}

	/// <summary>
	/// Sets the value of an object-backed variable with the given name using a runtime value.
	/// </summary>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if no variable with this name exists or if the declared type
	/// of the variable is not compatible with the value type.</exception>
	public void SetObject(StringKey name, object? value)
	{
		if (!_objectVariables.TryGetValue(name, out ObjectVariableStorage stored))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}': no object variable with this name exists.");
		}

		if (value is not null && !stored.ValueType.IsInstanceOfType(value))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}' with value type {value.GetType()}: variable expects values assignable to " +
				$"{stored.ValueType}.");
		}

		_objectVariables[name] = new ObjectVariableStorage(stored.ValueType, value);
	}

	/// <summary>
	/// Defines a new mutable object-backed variable directly in this <see cref="Variables"/> bag.
	/// </summary>
	/// <typeparam name="T">The type of the variable.</typeparam>
	/// <param name="name">The name of the variable.</param>
	/// <param name="value">The initial value.</param>
	public void DefineObjectVariable<T>(StringKey name, T value = default!)
	{
		_objectVariables[name] = new ObjectVariableStorage(typeof(T), value);
	}

	/// <summary>
	/// Defines a new mutable array variable directly in this <see cref="Variables"/> bag. If an array variable with
	/// the given name already exists, its values are replaced.
	/// </summary>
	/// <remarks>
	/// This is intended for entity-level shared variables that are not defined through a graph's
	/// <see cref="GraphVariableDefinitions"/>. For graph instance variables, use
	/// <see cref="InitializeFrom(GraphVariableDefinitions)"/> instead.
	/// </remarks>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="values">The initial values of the array variable.</param>
	public void DefineArrayVariable(StringKey name, IEnumerable<Variant128> values)
	{
		_arrays[name] = [.. values];
	}

	/// <summary>
	/// Defines a new mutable object-backed array variable directly in this <see cref="Variables"/> bag.
	/// </summary>
	/// <typeparam name="T">The type of each element.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="values">The initial values for the array variable.</param>
	public void DefineObjectArrayVariable<T>(StringKey name, IEnumerable<T> values)
	{
		_objectArrays[name] = new ObjectArrayStorage(typeof(T), [.. values.Cast<object?>()]);
	}

	/// <summary>
	/// Defines a new mutable object-backed array variable directly in this <see cref="Variables"/> bag using runtime
	/// type information.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="elementType">The declared element type for the array.</param>
	/// <param name="values">The initial values for the array variable.</param>
	/// <exception cref="InvalidOperationException">Thrown if any element is not assignable to
	/// <paramref name="elementType"/>.</exception>
	public void DefineObjectArrayVariable(StringKey name, Type elementType, IEnumerable<object?> values)
	{
		List<object?> materializedValues = [.. values];

		for (int i = 0; i < materializedValues.Count; i++)
		{
			object? value = materializedValues[i];
			if (value is not null && !elementType.IsInstanceOfType(value))
			{
				throw new InvalidOperationException(
					$"Cannot define '{name}' with element type {value.GetType()}: array expects values assignable to " +
					$"{elementType}.");
			}
		}

		_objectArrays[name] = new ObjectArrayStorage(elementType, materializedValues);
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
	/// Gets the element at the specified index from an object-backed array variable.
	/// </summary>
	/// <typeparam name="T">The type to interpret the element as.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <param name="value">The value of the element if found and type-compatible.</param>
	/// <returns><see langword="true"/> if the array variable was found, the index was valid, and the declared element
	/// type is compatible; otherwise, <see langword="false"/>.</returns>
	public bool TryGetObjectArrayElement<T>(StringKey name, int index, [MaybeNullWhen(false)] out T value)
	{
		value = default!;

		if (!_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			return false;
		}

		if (index < 0 || index >= stored.Values.Count || !typeof(T).IsAssignableFrom(stored.ElementType))
		{
			return false;
		}

		value = (T)stored.Values[index]!;
		return true;
	}

	/// <summary>
	/// Tries to get the full typed contents of an object-backed array variable.
	/// </summary>
	/// <typeparam name="T">The type to interpret the elements as.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="values">The resolved array if found and type-compatible.</param>
	/// <returns><see langword="true"/> if the array variable was found and the declared element type is compatible;
	/// <see langword="false"/> otherwise.</returns>
	public bool TryGetObjectArray<T>(StringKey name, [NotNullWhen(true)] out T[]? values)
	{
		values = null;

		if (!_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			return false;
		}

		if (!typeof(T).IsAssignableFrom(stored.ElementType))
		{
			return false;
		}

		values = new T[stored.Values.Count];
		for (int i = 0; i < stored.Values.Count; i++)
		{
			values[i] = (T)stored.Values[i]!;
		}

		return true;
	}

	/// <summary>
	/// Tries to get the full contents of an object-backed array variable using a runtime element type.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="expectedElementType">The expected element type.</param>
	/// <param name="values">The resolved array if found and type-compatible.</param>
	/// <returns><see langword="true"/> if the array variable was found and the declared element type is compatible;
	/// <see langword="false"/> otherwise.</returns>
	public bool TryGetObjectArray(
		StringKey name,
		Type expectedElementType,
		[NotNullWhen(true)] out object?[]? values)
	{
		values = null;

		if (!_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			return false;
		}

		if (!expectedElementType.IsAssignableFrom(stored.ElementType))
		{
			return false;
		}

		values = [.. stored.Values];
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
	/// Sets the element at the specified index in an object-backed array variable.
	/// </summary>
	/// <typeparam name="T">The type of the value to set.</typeparam>
	/// <param name="name">The name of the array variable.</param>
	/// <param name="index">The zero-based index of the element to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="InvalidOperationException">Thrown if no variable with this name exists or if the declared type
	/// of the variable is not compatible with the value type.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
	public void SetObjectArrayElement<T>(StringKey name, int index, T value)
	{
		if (!_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			throw new InvalidOperationException(
				$"Cannot set object array element for '{name}': no array variable with this name exists.");
		}

		if ((uint)index >= (uint)stored.Values.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(index),
				index,
				$"Index {index} is out of range for array '{name}' with length {stored.Values.Count}.");
		}

		if (value is not null && !stored.ElementType.IsInstanceOfType(value))
		{
			throw new InvalidOperationException(
				$"Cannot set '{name}' with element type {value.GetType()}: array expects values assignable to " +
				$"{stored.ElementType}.");
		}

		stored.Values[index] = value;
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
	/// Gets the length of an object-backed array variable.
	/// </summary>
	/// <param name="name">The name of the array variable.</param>
	/// <returns>The number of elements in the array, or -1 if the variable does not exist or is not an object-backed
	/// array.
	/// </returns>
	public int GetObjectArrayLength(StringKey name)
	{
		if (!_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			return -1;
		}

		return stored.Values.Count;
	}

	/// <summary>
	/// Tries to get the declared element type of an object-backed array variable with the given name.
	/// </summary>
	/// <param name="name">The name of the array variable to inspect.</param>
	/// <param name="elementType">The declared element type if the array exists.</param>
	/// <returns><see langword="true"/> if the object-backed array exists; otherwise, <see langword="false"/>.</returns>
	public bool TryGetObjectArrayElementType(StringKey name, [NotNullWhen(true)] out Type? elementType)
	{
		if (_objectArrays.TryGetValue(name, out ObjectArrayStorage? stored))
		{
			elementType = stored.ElementType;
			return true;
		}

		elementType = null;
		return false;
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

	private readonly record struct ObjectVariableStorage(Type ValueType, object? Value);

	private sealed class ObjectArrayStorage(Type elementType, List<object?> values)
	{
		public Type ElementType { get; } = elementType;

		public List<object?> Values { get; } = values;
	}
}
