// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents a collection of variables used within a Statescript graph.
/// </summary>
public class Variables : ICloneable
{
	private Dictionary<StringKey, Variant128>? _savedVariables;

	private Dictionary<StringKey, Variant128> _variables;

	/// <summary>
	/// Gets or sets the variable with the specified key.
	/// </summary>
	/// <param name="key">The key of the variable.</param>
	/// <returns>The variable associated with the specified key.</returns>
	public Variant128 this[StringKey key]
	{
		get => _variables[key];
		set => _variables[key] = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Variables"/> class.
	/// </summary>
	public Variables()
	{
		_variables = [];
	}

	/// <summary>
	/// Saves the current variable values.
	/// </summary>
	public void SaveVariableValues()
	{
		_savedVariables = _variables;
	}

	/// <summary>
	/// Loads the saved variable values.
	/// </summary>
	public void LoadVariableValues()
	{
		if (_savedVariables is null)
		{
			return;
		}

		_variables = _savedVariables;
	}

	/// <summary>
	/// Sets the variable with the given name to the given value.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by Variant128.</typeparam>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The value to set the variable to.</param>
	/// <returns><see langword="true"/> if the variable was set successfully, <see langword="false"/> otherwise.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by Variant128.</exception>
	public bool SetVar<T>(StringKey name, T value)
	{
		_variables[name] = value switch
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
		return true;
	}

	/// <summary>
	/// Tries to get the variable with the given name.
	/// </summary>
	/// <typeparam name="T">The type of the variable to get. Must be supported by Variant128.</typeparam>
	/// <param name="name">The name of the variable to get.</param>
	/// <param name="value">The value of the variable if it was found.</param>
	/// <returns><see langword="true"/> if the variable was found and retrieved successfully, <see langword="false"/>
	/// otherwise.</returns>
	public bool TryGetVar<T>(StringKey name, out T value)
		where T : unmanaged
	{
		value = default;

		if (!_variables.TryGetValue(name, out Variant128 variant))
		{
			return false;
		}

		value = variant.Get<T>();

		return true;
	}

	/// <summary>
	/// Loads variable definitions and values from another <see cref="Variables"/> instance, replacing the current
	/// variable set. This is typically used to initialize runtime variables from a graph's default variable
	/// definitions.
	/// </summary>
	/// <param name="source">The source variables to copy from.</param>
	public void LoadFrom(Variables source)
	{
		_variables = new Dictionary<StringKey, Variant128>(source._variables);
		_savedVariables = null;
	}

	/// <inheritdoc/>
	public object Clone()
	{
		var copy = new Variables();

		if (_savedVariables is not null)
		{
			copy._savedVariables = new Dictionary<StringKey, Variant128>(_savedVariables);
		}
		else
		{
			copy._savedVariables = null;
		}

		copy._variables = new Dictionary<StringKey, Variant128>(_variables);

		return copy;
	}
}
