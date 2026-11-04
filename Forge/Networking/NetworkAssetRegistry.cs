// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// The assets a session refers to on the wire, each by a small index instead of its name: every effect, ability,
/// graph and networked scene the game can send. Both ends build the same registry and compare its hash in the
/// handshake, so an index means the same asset everywhere.
/// </summary>
/// <remarks>
/// <para>An asset is registered under a key the host chooses: a string that identifies it and nothing else. The
/// registry sorts the keys of each kind and numbers them, so the indices don't depend on the order of registration,
/// and hashes the kinds and keys, so two registries with the same keys hash the same and two that differ by one asset
/// don't. The host decides what a key is: an engine integration uses the engine's own stable asset ids, such as
/// Godot's resource UIDs or Unity's asset GUIDs, so renaming or moving a file changes nothing; a bare host uses
/// names.</para>
/// <para>Registration ends with <see cref="Seal"/>, before the first handshake; a sealed registry never changes, since
/// an index that moved would mean another asset to a peer that joined earlier.</para>
/// </remarks>
public sealed class NetworkAssetRegistry
{
	// The kinds, counted by hand: Enum.GetValues isn't AOT-safe, and a new kind is a protocol change anyway.
	private const int KindCount = 4;

	private readonly SortedDictionary<string, object>[] _pending = new SortedDictionary<string, object>[KindCount];
	private readonly Dictionary<string, int>[] _indices = new Dictionary<string, int>[KindCount];
	private readonly string[][] _keys = new string[KindCount][];
	private readonly object[][] _assets = new object[KindCount][];

	/// <summary>
	/// Gets a value indicating whether registration has ended and the indices and hash are final.
	/// </summary>
	public bool IsSealed { get; private set; }

	/// <summary>
	/// Gets the hash of every kind and key, the same on every machine with the same assets. Zero until sealed.
	/// </summary>
	public ulong Hash { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NetworkAssetRegistry"/> class, empty and open for registration.
	/// </summary>
	public NetworkAssetRegistry()
	{
		for (int kind = 0; kind < KindCount; kind++)
		{
			_pending[kind] = new SortedDictionary<string, object>(StringComparer.Ordinal);
			_indices[kind] = [];
			_keys[kind] = [];
			_assets[kind] = [];
		}
	}

	/// <summary>
	/// Registers an asset under a key.
	/// </summary>
	/// <param name="kind">What kind of asset it is.</param>
	/// <param name="key">The string that identifies it: an engine's stable asset id, or a name.</param>
	/// <param name="asset">The asset.</param>
	/// <exception cref="InvalidOperationException">Thrown once the registry is sealed.</exception>
	/// <exception cref="ArgumentException">Thrown when the key is empty or already registered for the kind.</exception>
	public void Register(AssetKind kind, string key, object asset)
	{
		if (IsSealed)
		{
			throw new InvalidOperationException("A sealed registry takes no more assets.");
		}

		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("An asset's key identifies it, so it can't be empty.", nameof(key));
		}

		Validation.Assert(asset is not null, "A registered asset is not null.");

		if (!_pending[(int)kind].TryAdd(key, asset))
		{
			throw new ArgumentException($"The {kind} '{key}' is already registered.", nameof(key));
		}
	}

	/// <summary>
	/// Ends registration: numbers each kind's assets in key order and computes the hash.
	/// </summary>
	public void Seal()
	{
		if (IsSealed)
		{
			return;
		}

		ulong hash = Fnv64.Offset;

		for (int kind = 0; kind < KindCount; kind++)
		{
			SortedDictionary<string, object> pending = _pending[kind];
			_keys[kind] = new string[pending.Count];
			_assets[kind] = new object[pending.Count];
			hash = Fnv64.Add(hash, (ulong)kind);
			hash = Fnv64.Add(hash, (ulong)pending.Count);

			int index = 0;

			foreach (KeyValuePair<string, object> entry in pending)
			{
				_keys[kind][index] = entry.Key;
				_assets[kind][index] = entry.Value;
				_indices[kind][entry.Key] = index;
				hash = Fnv64.Add(hash, entry.Key);
				index++;
			}

			pending.Clear();
		}

		Hash = hash;
		IsSealed = true;
	}

	/// <summary>
	/// Gets how many assets of a kind are registered.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <returns>The count.</returns>
	public int Count(AssetKind kind)
	{
		ThrowIfNotSealed();
		return _keys[(int)kind].Length;
	}

	/// <summary>
	/// Gets how many bits an index of a kind takes on the wire: enough for every index, at least one.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <returns>The bit count.</returns>
	public int BitsFor(AssetKind kind)
	{
		return BitWriter.BitsFor((ulong)Math.Max(Count(kind) - 1, 0));
	}

	/// <summary>
	/// Gets the index an asset travels as.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <param name="key">The key it was registered under.</param>
	/// <returns>The index, or -1 when nothing is registered under the key.</returns>
	public int IndexOf(AssetKind kind, string key)
	{
		ThrowIfNotSealed();
		return _indices[(int)kind].TryGetValue(key, out int index) ? index : -1;
	}

	/// <summary>
	/// Gets the key an index stands for.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <param name="index">The index.</param>
	/// <returns>The key.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when nothing has that index.</exception>
	public string KeyAt(AssetKind kind, int index)
	{
		ThrowIfNotSealed();
		string[] keys = _keys[(int)kind];

		if (index < 0 || index >= keys.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, $"No {kind} has index {index}.");
		}

		return keys[index];
	}

	/// <summary>
	/// Gets the asset an index stands for.
	/// </summary>
	/// <typeparam name="T">The asset's type.</typeparam>
	/// <param name="kind">The kind.</param>
	/// <param name="index">The index.</param>
	/// <param name="asset">The asset.</param>
	/// <returns><see langword="true"/> if something of that type has that index.</returns>
	public bool TryGet<T>(AssetKind kind, int index, out T asset)
	{
		ThrowIfNotSealed();
		object[] assets = _assets[(int)kind];

		if (index >= 0 && index < assets.Length && assets[index] is T found)
		{
			asset = found;
			return true;
		}

		asset = default!;
		return false;
	}

	/// <summary>
	/// Gets the asset an index stands for, for an index that came from a trusted place.
	/// </summary>
	/// <typeparam name="T">The asset's type.</typeparam>
	/// <param name="kind">The kind.</param>
	/// <param name="index">The index.</param>
	/// <returns>The asset.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when nothing of that type has that index.</exception>
	public T Get<T>(AssetKind kind, int index)
	{
		if (TryGet(kind, index, out T asset))
		{
			return asset;
		}

		throw new ArgumentOutOfRangeException(
			nameof(index),
			index,
			$"No {kind} of type {typeof(T).Name} has index {index}.");
	}

	private void ThrowIfNotSealed()
	{
		if (!IsSealed)
		{
			throw new InvalidOperationException("Seal the registry before looking assets up in it.");
		}
	}
}
