// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Internal view over a resolved source array from either the value lane (<see cref="IArrayPropertyResolver"/>) or the
/// object lane (<see cref="IObjectArrayResolver"/>). Used by resolvers whose source may come from either lane (e.g.
/// reductions and projections) to iterate elements uniformly as <see cref="ElementFrame"/> instances.
/// </summary>
internal readonly struct ElementSequence
{
	private readonly Variant128[]? _values;

	private readonly object?[]? _objects;

	internal Type ElementType { get; }

	internal int Length => _values?.Length ?? _objects!.Length;

	private ElementSequence(Variant128[]? values, object?[]? objects, Type elementType)
	{
		_values = values;
		_objects = objects;
		ElementType = elementType;
	}

	internal static ElementSequence Resolve(
		GraphContext graphContext,
		IArrayPropertyResolver? valueSource,
		IObjectArrayResolver? objectSource)
	{
		if (valueSource is not null)
		{
			return new ElementSequence(valueSource.ResolveArray(graphContext), null, valueSource.ElementType);
		}

		return new ElementSequence(null, objectSource!.ResolveArray(graphContext), objectSource.ElementType);
	}

	internal static Type GetElementType(IArrayPropertyResolver? valueSource, IObjectArrayResolver? objectSource)
	{
		return valueSource?.ElementType ?? objectSource!.ElementType;
	}

	internal ElementFrame GetFrame(int index)
	{
		return _values is not null
			? new ElementFrame(_values[index], ElementType, index)
			: new ElementFrame(_objects![index], ElementType, index);
	}
}
