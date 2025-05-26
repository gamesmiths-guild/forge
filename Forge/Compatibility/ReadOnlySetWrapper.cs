// Copyright © Gamesmiths Guild.

#if NETSTANDARD2_1
using System.Collections;

namespace Gamesmiths.Forge.Compatibility;

internal class ReadOnlySetWrapper<T>(ISet<T> inner) : IReadOnlySet<T>
{
	private readonly ISet<T> _inner = inner ?? throw new ArgumentNullException(nameof(inner));

	public int Count => _inner.Count;

	public bool Contains(T item)
	{
		return _inner.Contains(item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _inner.GetEnumerator();
	}

	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	public bool IsSubsetOf(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	public bool IsSupersetOf(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	public bool Overlaps(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	public bool SetEquals(IEnumerable<T> other)
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
#endif
