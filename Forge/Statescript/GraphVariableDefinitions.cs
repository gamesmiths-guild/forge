// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Defines the schema of variables and properties for a graph. This is immutable definition data that belongs to the
/// <see cref="Graph"/>, it contains no runtime state. When a graph execution starts, a runtime <see cref="Variables"/>
/// instance is created from these definitions and placed into the <see cref="IGraphContext"/>.
/// </summary>
/// <remarks>
/// <para>Variables and properties are both stored as <see cref="PropertyDefinition"/>s. A variable is simply a property
/// backed by a <see cref="VariantResolver"/> (mutable <see cref="Variant128"/>), while read-only properties use other
/// <see cref="IPropertyResolver"/> implementations.</para>
/// </remarks>
public class GraphVariableDefinitions
{
	/// <summary>
	/// Gets the list of property definitions for the graph.
	/// </summary>
	public List<PropertyDefinition> Definitions { get; } = [];

	/// <summary>
	/// Adds a mutable variable definition with the specified name and initial value. At runtime, a fresh
	/// <see cref="VariantResolver"/> is created for each graph execution with the given initial value.
	/// </summary>
	/// <typeparam name="T">The type of the initial value. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="name">The name of the variable.</param>
	/// <param name="initialValue">The initial value of the variable.</param>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void DefineVariable<T>(StringKey name, T initialValue)
	{
		Variant128 variant = initialValue switch
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

		Definitions.Add(new PropertyDefinition(name, new VariantResolver(variant, typeof(T))));
	}

	/// <summary>
	/// Adds a mutable array variable definition with the specified name and initial values. At runtime, a fresh
	/// <see cref="ArrayVariableResolver"/> is created for each graph execution with copies of the initial values.
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
			variants[i] = initialValues[i] switch
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

		Definitions.Add(new PropertyDefinition(name, new ArrayVariableResolver(variants, typeof(T))));
	}

	/// <summary>
	/// Adds a read-only property definition with the specified name and resolver.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="resolver">The resolver used to compute the property's value at runtime.</param>
	public void DefineProperty(StringKey name, IPropertyResolver resolver)
	{
		Definitions.Add(new PropertyDefinition(name, resolver));
	}

	/// <summary>
	/// Validates that the property or variable with the specified name produces a value assignable to the expected
	/// type. This should be called at graph construction time to catch type mismatches early (e.g., binding a
	/// <see cref="TagResolver"/> to a timer duration that expects a numeric type).
	/// </summary>
	/// <param name="name">The name of the property or variable to validate.</param>
	/// <param name="expectedType">The expected value type.</param>
	/// <returns><see langword="true"/> if the property exists and its value type is assignable to the expected type;
	/// <see langword="false"/> otherwise.</returns>
	public bool ValidatePropertyType(StringKey name, Type expectedType)
	{
		foreach (PropertyDefinition definition in Definitions)
		{
			if (definition.Name == name)
			{
				return expectedType.IsAssignableFrom(definition.Resolver.ValueType);
			}
		}

		return false;
	}
}
