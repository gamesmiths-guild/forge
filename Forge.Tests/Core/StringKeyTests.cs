// Copyright © 2024 Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tests.Core;

public class StringKeyTests
{
	[Theory]
	[Trait("Initialization", "Constructor")]

	// Simple strings
	[InlineData("simple")]
	[InlineData("Simple")]

	// Special characters
	[InlineData("user@domain.com")]
	[InlineData("key#123")]
	[InlineData("value!@#$%^&*()_+-=[]{}|;':\",./<>?")]

	// String with diacritics
	[InlineData("Exceção")]

	// Mixed cases
	[InlineData("MixEdCaSe")]

	// Path-like strings
	[InlineData("path/to/resource")]
	[InlineData("a.b.c")]

	// Separator variations
	[InlineData("dashed-string")]
	[InlineData("underscore_string")]
	[InlineData("dot.separated.string")]
	[InlineData("spaced string")]

	// Embedded null characters
	[InlineData("null\0char")]
	[InlineData("another\0null")]

	public void Constructor_initializes_with_correct_value(string keyName)
	{
		var key = new StringKey(keyName);

		key.ToString().Should().Be(keyName.Trim().ToLowerInvariant());
		key.GetHashCode().Should().Be(StringComparer.OrdinalIgnoreCase.GetHashCode(keyName.Trim()));
	}

	[Theory]
	[Trait("Initialization", "Trim")]

	// Leading and trailing spaces
	[InlineData("   spaced out key   ")]
	[InlineData(" leading space")]
	[InlineData("trailing space ")]

	// Special characters
	[InlineData("\tTabbedKey\t")]
	[InlineData("\nNewlineKey\n")]
	[InlineData("\r\nCarriageReturnKey\r\n")]

	// Excessive internal spaces
	[InlineData("key   with   multiple spaces")]

	// No spaces, ensure no trimming errors
	[InlineData("nospaces")]
	public void Trims_leading_and_trailing_whitespaces_on_initialization(string keyName)
	{
		var key = new StringKey(keyName);

		key.ToString().Should().NotStartWith(" ").And.NotEndWith(" ");
	}

	[Fact]
	[Trait("Initialization", "Edge cases")]
	public void Handles_very_long_strings_correctly()
	{
		// 10,000 'a's
		var longString = new string('a', 10000);
		var key = new StringKey(longString);
		key.ToString().Should().Be(longString.ToLowerInvariant());
	}

	[Theory]
	[Trait("Initialization", "Normalization")]
	[InlineData("ConsistentString", "consistentstring")]
	[InlineData("AnotherTest ", "anothertest")]

	// Should normalize Unicode characters to lowercase
	[InlineData("Café", "café")]
	[InlineData("Straße", "straße")] // German sharp S
	[InlineData("Δοκιμή", "δοκιμή")] // Greek word for "test"

	[InlineData("control\u0001char", "control\u0001char")]
	[InlineData("newline\nchar", "newline\nchar")]
	[InlineData("user@domain.com", "user@domain.com")]
	[InlineData("key#123", "key#123")]
	[InlineData("value!@#$%^&*()", "value!@#$%^&*()")]
	public void Normalization_handles_special_characters_as_expected(string keyName, string expected)
	{
		var key = new StringKey(keyName);

		key.ToString().Should().Be(expected);
	}

	[Theory]
	[Trait("Conversion", "From string")]

	// Simple strings
	[InlineData("simple")]
	[InlineData("Simple")]

	// Special characters
	[InlineData("user@domain.com")]
	[InlineData("key#123")]
	[InlineData("value!@#$%^&*()_+-=[]{}|;':\",./<>?")]

	// String with diacritics
	[InlineData("Exceção")]

	// Mixed cases
	[InlineData("MixEdCaSe")]

	// Path-like strings
	[InlineData("path/to/resource")]
	[InlineData("a.b.c")]

	// Separator variations
	[InlineData("dashed-string")]
	[InlineData("underscore_string")]
	[InlineData("dot.separated.string")]
	[InlineData("spaced string")]

	// Embedded null characters
	[InlineData("null\0char")]
	[InlineData("another\0null")]
	public void Conversion_from_string_is_correct(string keyName)
	{
		StringKey key = keyName;

		key.ToString().Should().Be(keyName.Trim().ToLowerInvariant());
		key.GetHashCode().Should().Be(StringComparer.OrdinalIgnoreCase.GetHashCode(keyName.Trim()));
	}

	[Theory]
	[Trait("Conversion", "To string")]

	// Simple strings
	[InlineData("simple")]
	[InlineData("Simple")]

	// Special characters
	[InlineData("user@domain.com")]
	[InlineData("key#123")]
	[InlineData("value!@#$%^&*()_+-=[]{}|;':\",./<>?")]

	// String with diacritics
	[InlineData("Exceção")]

	// Mixed cases
	[InlineData("MixEdCaSe")]

	// Path-like strings
	[InlineData("path/to/resource")]
	[InlineData("a.b.c")]

	// Separator variations
	[InlineData("dashed-string")]
	[InlineData("underscore_string")]
	[InlineData("dot.separated.string")]
	[InlineData("spaced string")]

	// Embedded null characters
	[InlineData("null\0char")]
	[InlineData("another\0null")]
	public void Conversion_to_string_returns_lowercase(string keyName)
	{
		string convertedKey = new StringKey(keyName);

		convertedKey.Should().Be(keyName.Trim().ToLowerInvariant());
	}

	[Theory]
	[Trait("Conversion", "Back and forth")]
	[InlineData("consistentConversion")]
	[InlineData("ConsistentConversion ")]
	public void Implicit_conversions_maintain_consistency(string keyName)
	{
		var key = new StringKey(keyName);
		string convertedString = key;
		var newKey = (StringKey)convertedString;

		newKey.Should().Be(key);
	}

	[Theory]
	[Trait("Conversion", "Equality")]
	[InlineData("ImplicitConvert")]
	[InlineData("anotherTest ")]
	public void Implicit_conversion_is_consistent_with_equality(string keyName)
	{
		StringKey key1 = keyName;
		var key2 = new StringKey(keyName);

		key1.Should().Be(key2);
		(key1 == key2).Should().BeTrue();
	}

	[Theory]
	[Trait("Equality", "Equals")]

	// Identical strings
	[InlineData("identical", "identical")]

	// Same string, different cases
	[InlineData("sameValue", "SAMEVALUE")]
	[InlineData("REPEATKEY", "repeatKey")]
	[InlineData("CaseSensitive", "casesensitive")]

	// Different strings that are technically equal (e.g., trimmed or normalized)
	[InlineData("   spacedkey   ", "spacedkey")]
	public void StringKeys_are_equal(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		key1.Should().Be(key2);
		(key1 == key2).Should().BeTrue();
		(key1 != key2).Should().BeFalse();
		(key1 >= key2).Should().BeTrue();
		(key1 <= key2).Should().BeTrue();
		(key1 > key2).Should().BeFalse();
		(key1 < key2).Should().BeFalse();
		key1.CompareTo(key2).Should().Be(0);
		key1.CompareTo((object)key2).Should().Be(0);
		key1.Equals(key2).Should().BeTrue();
		key1.Equals((object)key2).Should().BeTrue();
		key1.Should().BeEquivalentTo(key2);

		// Both keys should reference the same interned string
		ReferenceEquals(key1.ToString(), key2.ToString()).Should().BeTrue();
	}

	[Theory]
	[Trait("Equality", "Not equals")]
	[InlineData("key1", "key2")]
	[InlineData("a.b.c", "a.b.d")]
	[InlineData("a.b.c", "a b c")]
	public void StringKeys_are_not_equal(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		key1.Should().NotBe(key2);
		(key1 == key2).Should().BeFalse();
		(key1 != key2).Should().BeTrue();
		key1.CompareTo(key2).Should().NotBe(0);
		key1.CompareTo((object)key2).Should().NotBe(0);
		key1.Equals(key2).Should().BeFalse();
		key1.Equals((object)key2).Should().BeFalse();
		key1.Should().NotBeSameAs(key2);

		// Both keys should reference the same interned string
		ReferenceEquals(key1.ToString(), key2.ToString()).Should().BeFalse();
	}

	[Fact]
	[Trait("Equality", "Null")]
	public void Equals_returns_false_when_compared_with_null()
	{
		var key = new StringKey("testKey");
		var equalsResult = key.Equals((StringKey?)null);
		equalsResult.Should().BeFalse();
	}

	[Fact]
	[Trait("Equality", "Non StringKey object")]
	public void Equals_returns_false_for_different_object_type()
	{
		var key = new StringKey("testKey");
		const int differentTypeObject = 123;
		var equalsResult = key.Equals(differentTypeObject);
		equalsResult.Should().BeFalse();
	}

	[Theory]
	[Trait("Comparability", "Less than")]
	[InlineData("a.b.c", "x.y.z")]
	[InlineData("a.b.b", "a.b.c")]
	[InlineData("A.B.B", "a.b.c")]
	[InlineData("a.b.b", "A.B.C")]
	[InlineData("a.b", "a.b.c")]
	[InlineData("a.b.c", "a.c.b")]
	public void StringKey1_is_less_than_StringKey2(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		(key1 < key2).Should().BeTrue();
		key1.CompareTo(key2).Should().BeNegative();
	}

	[Theory]
	[Trait("Comparability", "Less than")]
	[InlineData("a.b.c", "x.y.z")]
	[InlineData("a.b.b", "a.b.c")]
	[InlineData("A.B.B", "a.b.c")]
	[InlineData("a.b.b", "A.B.C")]
	[InlineData("a.b", "a.b.c")]
	[InlineData("a.b.c", "a.c.b")]
	[InlineData("Equal", "Equal")]
	public void StringKey1_is_less_than_or_equal_to_StringKey2(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		(key1 <= key2).Should().BeTrue();
		key1.CompareTo(key2).Should().BeLessThanOrEqualTo(0);
	}

	[Theory]
	[Trait("Comparability", "Greater than")]
	[InlineData("x.y.z", "a.b.c")]
	[InlineData("a.b.c", "a.b.b")]
	[InlineData("a.b.c", "A.B.B")]
	[InlineData("A.B.C", "a.b.b")]
	[InlineData("a.b.c", "a.b")]
	[InlineData("a.c.b", "a.b.c")]
	public void StringKey1_is_greater_than_StringKey2(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		(key1 > key2).Should().BeTrue();
		key1.CompareTo(key2).Should().BePositive();
	}

	[Theory]
	[Trait("Comparability", "Greater than")]
	[InlineData("x.y.z", "a.b.c")]
	[InlineData("a.b.c", "a.b.b")]
	[InlineData("a.b.c", "A.B.B")]
	[InlineData("A.B.C", "a.b.b")]
	[InlineData("a.b.c", "a.b")]
	[InlineData("a.c.b", "a.b.c")]
	[InlineData("Equal", "Equal")]
	public void StringKey1_is_greater_than_or_equal_to_StringKey2(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		(key1 >= key2).Should().BeTrue();
		key1.CompareTo(key2).Should().BeGreaterThanOrEqualTo(0);
	}

	[Theory]
	[Trait(nameof(StringKey), "Comparability")]
	[InlineData("apple", "banana", -1)]
	[InlineData("Banana", "apple", 1)]
	[InlineData("cherry", "Cherry", 0)]
	public void CompareTo_orders_as_expected(string keyName1, string keyName2, int expected)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		key1.CompareTo(key2).Should().Be(expected);
	}

	[Theory]
	[Trait("Comparability", "Special characters")]
	[InlineData("alpha@", "alpha#")]
	[InlineData("beta!", "beta$")]
	[InlineData("gamma%", "gamma^")]
	public void Comparability_correctly_handles_special_characters(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		// Depending on the expected ordering, adjust the assertions
		var comparison = string.Compare(
			keyName1.Trim().ToLowerInvariant(),
			keyName2.Trim().ToLowerInvariant(),
			StringComparison.OrdinalIgnoreCase);

		key1.CompareTo(key2).Should().Be(comparison);
	}

	[Fact]
	[Trait("Comparability", "Exception")]
	public void CompareTo_throws_exception_for_invalid_object()
	{
		var key = new StringKey("testKey");

		const int nonStringKeyObject = 123;

		Func<int> act = () =>
		{
			return key.CompareTo(nonStringKeyObject);
		};

		act.Should().Throw<ArgumentException>().WithMessage("Object is not a valid*");
	}

	[Theory]
	[Trait("HashCode", "Equivalent")]
	[InlineData("TestString", "teststring")]
	[InlineData("AnotherKey ", "anotherkey")]
	public void Equivalent_keys_have_same_HashCode(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		key1.GetHashCode().Should().Be(key2.GetHashCode());
	}

	[Theory]
	[Trait("HashCode", "Distribution")]
	[InlineData("hash1", "hash2")]
	[InlineData("unique1", "unique2")]
	public void Different_StringKeys_have_different_HashCodes(string keyName1, string keyName2)
	{
		var key1 = new StringKey(keyName1);
		var key2 = new StringKey(keyName2);

		key1.GetHashCode().Should().NotBe(key2.GetHashCode());
	}

	[Fact]
	[Trait("Collections", "Unique")]
	public void HashSet_contains_only_unique_StringKey()
	{
		var set = new HashSet<StringKey>
		{
			new("duplicate"),
			new("DUPLICATE "),
			new("duplicate"),
		};

		set.Should().ContainSingle();
	}

	[Fact]
	[Trait("Collections", "Dictionary")]
	public void Dictionary_uses_StringKeys_correctly_as_keys()
	{
		var dictionary = new Dictionary<StringKey, int>();
		var key1 = new StringKey("key");
		var key2 = new StringKey("KEY ");

		dictionary[key1] = 1;

		// Should overwrite the previous entry.
		dictionary[key2] = 2;

		dictionary.Should().ContainSingle();
		dictionary.Should().Contain(key1, 2);
	}

	[Fact]
	[Trait("Immutability", "Access")]
	public void ToString_returns_the_same_value_across_multiple_calls()
	{
		var key = new StringKey("immutableKey");
		var firstCall = key.ToString();
		var secondCall = key.ToString();
		var thirdCall = key.ToString();

		firstCall.Should().Be(secondCall);
		secondCall.Should().Be(thirdCall);
	}

	[Fact]
	[Trait("Concurrency", "Thread safety")]
	public async Task Concurrent_access_does_not_cause_issues()
	{
		var key = new StringKey("concurrentKey");
		const int numberOfTasks = 100;
		var tasks = new Task[numberOfTasks];
		var results = new bool[numberOfTasks];

		for (var i = 0; i < numberOfTasks; i++)
		{
			var taskIndex = i;
			tasks[taskIndex] = Task.Run(() =>
			{
				results[taskIndex] = key.ToString() == "concurrentkey";
			});
		}

		await Task.WhenAll(tasks);
		results.Should().OnlyContain(x => x);
	}

	[Theory]
	[Trait("Nullable", "Operators")]
	[InlineData("key1", null)]
	[InlineData(null, "null")]
	public void Operator_overloads_handle_nullable_StringKey_comparability(string? key1name, string? key2name)
	{
		StringKey? key1 = key1name is not null ? new StringKey(key1name) : (StringKey?)null;
		StringKey? key2 = key2name is not null ? new StringKey(key2name) : (StringKey?)null;

		// Comparing a non-null key to null
		(key1 > key2).Should().BeFalse();
		(key1 < key2).Should().BeFalse();
		(key1 >= key2).Should().BeFalse();
		(key1 <= key2).Should().BeFalse();
	}

	[Theory]
	[Trait("Nullable", "Operators")]
	[InlineData(null, null)]
	[InlineData("equals", "Equals")]
	public void Operator_overloads_handle_nullable_StringKey_equality(string? key1name, string? key2name)
	{
		StringKey? key1 = key1name is not null ? new StringKey(key1name) : (StringKey?)null;
		StringKey? key2 = key2name is not null ? new StringKey(key2name) : (StringKey?)null;

		// Comparing a non-null key to null
		(key1 == key2).Should().BeTrue();
		(key1 != key2).Should().BeFalse();
	}
}
