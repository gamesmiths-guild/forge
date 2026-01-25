# Tags System

The Tags system in Forge provides a hierarchical mechanism for metadata, allowing efficient categorization and queries of game entities and systems. It's a foundational component that many other Forge systems build upon.

For a practical guide on using tags, see the [Quick Start Guide](quick-start.md).

## Core Components

### StringKey

At the lowest level, Forge uses `StringKey` as an optimized string representation:

- Immutable, case-insensitive string wrapper.
- Uses string interning for memory optimization (identical strings share the same reference).
- Automatically converts to lowercase for consistent comparison.
- Perfect for dictionary keys that need case-insensitive comparison.
- Provides implicit conversions to/from strings.

```csharp
// StringKeys are created automatically when strings are used with the Tags system
// The conversion handles case-insensitivity and interning automatically
StringKey tagKey = "Enemy.Undead.Zombie";  // Stored internally as "enemy.undead.zombie"
```

### Tag

A `Tag` is a string-based identifier that typically uses dot notation to define hierarchies (e.g., `enemy.undead.zombie`). In Forge, tags are not created directly but requested from the `TagsManager`.

```csharp
// Proper way to get a tag reference
var zombieTag = Tag.RequestTag(tagsManager, "enemy.undead.zombie");
```

#### Hierarchical Tag Structure

Tags in Forge use dot notation to establish parent-child relationships:

- `enemy` (parent)
  - `enemy.undead` (child of `enemy`, parent of `zombie`)
    - `enemy.undead.zombie` (child of `enemy.undead`)

When checking for a tag, parent tags are considered to be present if any of their children are present.

#### Common Tag Methods

- **RequestTag**: Static method to get a tag from the manager.
  ```csharp
  var tag = Tag.RequestTag(tagsManager, "enemy.undead.zombie");
  ```

- **GetSingleTagContainer**: Get a container with only this tag.
  ```csharp
  TagContainer container = tag.GetSingleTagContainer();
  ```

- **RequestDirectParent**: Get the immediate parent tag.
  ```csharp
  var parent = tag.RequestDirectParent(); // "enemy.undead" from "enemy.undead.zombie"
  ```

- **GetTagParents**: Get a container with this tag and all parents.
  ```csharp
  TagContainer parents = tag.GetTagParents(); // Contains "enemy.undead.zombie", "enemy.undead", "enemy"
  ```

#### Tag Matching Methods

- **MatchesTag**: Check if a tag matches or is a parent of another tag.
  ```csharp
  // "enemy.undead.zombie" matches "enemy" and "enemy.undead" (child matches parent)
  // "enemy" does NOT match "enemy.undead.zombie" (parent doesn't match child)
  bool isMatch = childTag.MatchesTag(parentTag);
  ```

- **MatchesTagExact**: Check for exact tag match (no hierarchy consideration).
  ```csharp
  bool isExactMatch = tag1.MatchesTagExact(tag2);
  ```

- **MatchesAny**: Check if tag matches any tag in a container (considering hierarchy).
  ```csharp
  bool matchesAny = tag.MatchesAny(container);
  ```

- **MatchesAnyExact**: Check for exact match with any tag in a container.
  ```csharp
  bool matchesAnyExact = tag.MatchesAnyExact(container);
  ```

- **MatchesTagDepth**: Calculate similarity between two tags (higher value = more similar).
  ```csharp
  int similarity = tag1.MatchesTagDepth(tag2);
  ```

### TagsManager

The `TagsManager` is the central registry for all tags in your game. It:

- Maintains canonical tag instances.
- Resolves parent-child relationships.
- Provides efficient tag lookup and comparison.

#### Initialization and Immutability

Tags should be registered with the `TagsManager` during initialization, typically from a serialized file:

```csharp
// Load tags from serialized file (recommended approach)
string[] tagDefinitions = LoadTagsFromFile("GameTags.json");
var tagsManager = new TagsManager(tagDefinitions);

// Alternatively, hard-code during initialization
var tagsManager = new TagsManager([
    "enemy.undead.zombie",
    "enemy.undead.skeleton",
    "enemy.beast.wolf",
    "item.consumable.potion.health",
    "cues.damage.fire",
    "cues.status.poison"
]);
```

**Important:** The `TagsManager`'s tag registry should be immutable during gameplay. All tags should be registered during initialization to maintain efficiency and prevent runtime errors.

#### Common TagsManager Methods

- **RequestTagContainer**: Create a container from a string array.
  ```csharp
  TagContainer container = tagsManager.RequestTagContainer([
      "enemy.undead.zombie",
      "enemy.beast.wolf"
  ]);
  ```

- **RequestAllTags**: Get a container with all registered tags.
  ```csharp
  // Get all explicit tags (leaf nodes only)
  TagContainer allTags = tagsManager.RequestAllTags(true);

  // Get all tags including parent/intermediate tags
  TagContainer absolutelyAllTags = tagsManager.RequestAllTags(false);
  ```

- **RequestTagParents**: Get a container with a tag and its parents.
  ```csharp
  TagContainer parents = tagsManager.RequestTagParents(tag);
  ```

- **RequestTagChildren**: Get a container with child tags.
  ```csharp
  TagContainer children = tagsManager.RequestTagChildren(tag);
  ```

- **SplitTagKey**: Split a tag into its component parts.
  ```csharp
  string[] parts = tagsManager.SplitTagKey(tag); // ["enemy", "undead", "zombie"]
  ```

- **GetNumberOfTagNodes**: Count the depth of a tag in the hierarchy.
  ```csharp
  int depth = tagsManager.GetNumberOfTagNodes(tag); // 3 for "enemy.undead.zombie"
  ```

#### Single Instance vs. Multiple Managers

Typically, you should use **one TagsManager** for your entire game. This enables:

- Consistent tag usage across systems.
- Efficient memory usage.
- Simplified tag queries.

While not strictly prohibited, using multiple `TagsManager` instances means:

- Tags from different managers cannot be mixed.
- Effects using tags from one manager can't be applied to entities using another.
- Each manager maintains its own hierarchy.

### TagContainer

`TagContainer` stores and manages a collection of tags on entities or components. It handles both explicitly added tags and their implicit parent tags:

```csharp
// Create a tag container
var container = new TagContainer(tagsManager);

// Add tags to the container
container.AddTag(Tag.RequestTag(tagsManager, "enemy.undead.zombie"));

// Check if a tag exists in the container
bool isZombie = container.HasTag(Tag.RequestTag(tagsManager, "enemy.undead.zombie"));
```

#### Common TagContainer Methods

- **HasTag**: Check if the container has a tag (or a parent of that tag).
  ```csharp
  // container with "enemy.undead.zombie" will return true for "enemy"
  bool hasTag = container.HasTag(enemyTag);
  ```

- **HasTagExact**: Check for an exact tag match only (no hierarchy).
  ```csharp
  // container with "enemy.undead.zombie" will return false for "enemy"
  bool hasExact = container.HasTagExact(enemyTag);
  ```

- **HasAny**: Check if the container has any tags from another container.
  ```csharp
  bool hasAny = container1.HasAny(container2);
  ```

- **HasAnyExact**: Check if the container has any exact tags from another container.
  ```csharp
  bool hasAnyExact = container1.HasAnyExact(container2);
  ```

- **HasAll**: Check if the container has all tags from another container.
  ```csharp
  bool hasAll = container1.HasAll(container2);
  ```

- **HasAllExact**: Check if the container has all exact tags from another container.
  ```csharp
  bool hasAllExact = container1.HasAllExact(container2);
  ```

- **Filter**: Return a subset of the container matching another container.
  ```csharp
  TagContainer filtered = container1.Filter(container2);
  ```

- **FilterExact**: Return a subset with exact matches to another container.
  ```csharp
  TagContainer filteredExact = container1.FilterExact(container2);
  ```

- **MatchesQuery**: Check if the container satisfies a `TagQuery`.
  ```csharp
  bool matches = container.MatchesQuery(query);
  ```

### EntityTags

`EntityTags` manages the tags for an entity, separating permanent tags from temporary ones added by effects:

```csharp
// Create base tags for entity
var originalTags = new TagContainer(
    tagsManager,
    [
        Tag.RequestTag(tagsManager, "enemy.undead.zombie"),
        Tag.RequestTag(tagsManager, "color.green")
    ]);

// Create EntityTags component
var entityTags = new EntityTags(originalTags);
```

#### EntityTags Properties

- **BaseTags**: Permanent tags assigned to the entity (typically during creation).
- **ModifierTags**: Temporary tags applied through effects with reference counting.
- **CombinedTags**: Unified view of both `BaseTags` and `ModifierTags` for queries.

Typically, game code should use `CombinedTags` for tag checks:

```csharp
// Check if entity has a tag (correctly checks both base and modifier tags)
bool isStunned = entity.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
```

**Note:** While the `EntityTags` class provides methods for adding and removing tags, these are internal. Tags should be modified through proper channels: base tags during entity initialization and modifier tags through the [Effects system](effects/README.md).

## Tag Queries

TagQueries provide powerful logical operations for matching tag containers against complex conditions. They allow you to create sophisticated, reusable rules using nested logical expressions.

### Creating a Basic Query

```csharp
// Create a query that matches if any of these tags exist
var anyMatchQuery = TagQuery.MakeQueryMatchAnyTags(tagContainer);

// Create a query that matches if all of these tags exist
var allMatchQuery = TagQuery.MakeQueryMatchAllTags(tagContainer);

// Create a query that matches if none of these tags exist
var noMatchQuery = TagQuery.MakeQueryMatchNoTags(tagContainer);

// Exact matching variants (don't consider parent tags)
var exactMatchQuery = TagQuery.MakeQueryMatchAnyTagsExact(tagContainer);
```

### Complex Query Expressions

For more complex logic, use `TagQueryExpression` to build nested conditions:

```csharp
// Build a query: (isRed OR isBlue) AND NOT (isGreen)
var complexQuery = new TagQuery();
complexQuery.Build(
    new TagQueryExpression(tagsManager)
        .AllExpressionsMatch()                      // AND
            .AddExpression(
                new TagQueryExpression(tagsManager)
                    .AnyTagsMatch()                 // OR
                        .AddTag("color.red")
                        .AddTag("color.blue"))
            .AddExpression(
                new TagQueryExpression(tagsManager)
                    .NoTagsMatch()                  // NOT
                        .AddTag("color.green")));

// Test against a container
bool matches = complexQuery.Matches(entityTags.CombinedTags);
```

### Query Expression Types

TagQueries support these expression types:

- **Tag Matching**
  - `AnyTagsMatch` - At least one tag must match (hierarchical).
  - `AllTagsMatch` - All tags must match (hierarchical).
  - `NoTagsMatch` - No tags may match (hierarchical).
  - `AnyTagsMatchExact` - At least one tag must match exactly.
  - `AllTagsMatchExact` - All tags must match exactly.
  - `NoTagsMatchExact` - No tags may match exactly.

- **Expression Matching**
  - `AnyExpressionsMatch` - At least one nested expression must match.
  - `AllExpressionsMatch` - All nested expressions must match.
  - `NoExpressionsMatch` - No nested expressions may match.

### Use Cases

- **Ability Requirements**: Check if an entity meets specific tag requirements.
- **Target Filtering**: Filter potential targets based on complex criteria.
- **State-Dependent Behavior**: Execute different behavior based on entity state.
- **AI Decision Making**: Make AI decisions based on target properties.

```csharp
// AI targeting query: target enemy undead OR beasts, but NOT if they're immune to fire
var targetingQuery = new TagQuery();
targetingQuery.Build(
    new TagQueryExpression(tagsManager)
        .AllExpressionsMatch()
            .AddExpression(
                new TagQueryExpression(tagsManager)
                    .AnyTagsMatch()
                        .AddTag("enemy.undead")
                        .AddTag("enemy.beast"))
            .AddExpression(
                new TagQueryExpression(tagsManager)
                    .NoTagsMatch()
                        .AddTag("status.immune.fire")));

// Check potential targets
foreach (var target in potentialTargets)
{
    if (targetingQuery.Matches(target.Tags.CombinedTags))
    {
        validTargets.Add(target);
    }
}
```

## Integration with Other Systems

Tags are often used with other Forge systems:

```csharp
// Register a gameplay cue with a specific tag
// Note: Use cues.* prefix for gameplay cue tags
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cues.damage.fire"), new FireDamageCueHandler());

// Apply gameplay effects based on tags
if (entity.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.vulnerable.fire")))
{
    // Apply enhanced fire damage
}
```

## Tag Naming Conventions

### Gameplay Cue Tags

It's recommended to prefix gameplay cue tags with `cues.` to clearly distinguish them from gameplay tags. See the [Cues documentation](cues.md) for more details.

```csharp
// Good - clearly identifies a tag used for visual effects
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cues.damage.fire"), new FireCueHandler());
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cues.status.poison"), new PoisonCueHandler());

// Avoid - unclear purpose
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "damage.fire"), new FireCueHandler());
```

## Entity Setup

The proper way to initialize an entity with tags:

```csharp
// Create a test entity with tags
public class GameEntity : IForgeEntity
{
    public EntityTags Tags { get; }
    // Other IForgeEntity properties...

    public GameEntity(TagsManager tagsManager, CuesManager cuesManager)
    {
        // Initialize tags
        var originalTags = new TagContainer(
            tagsManager,
            [
                Tag.RequestTag(tagsManager, "character.player"),
                Tag.RequestTag(tagsManager, "class.warrior")
            ]);

        // Initialize the Tags component with its immutable base tags
        Tags = new(originalTags);
    }
}
```

## Tag Modification Patterns

Tag modification typically happens in specific ways:

1. **During Entity Initialization**
   ```csharp
   // Initial setup of entity tags via TagContainer
   var originalTags = new TagContainer(
       tagsManager,
       [
           Tag.RequestTag(tagsManager, "character.player"),
           Tag.RequestTag(tagsManager, "class.warrior")
       ]);
   var entityTags = new EntityTags(originalTags);
   ```

2. **Through Gameplay Effects**
   ```csharp
   // Create effect that applies a tag
   var stunEffectData = new EffectData(
       "Stun Effect",
       new DurationData(
           DurationType.HasDuration,
           new ModifierMagnitude(
               MagnitudeCalculationType.ScalableFloat,
               new ScalableFloat(3.0f)
           )
       ),
       effectComponents: [
           new ModifierTagsEffectComponent(
               tagsManager.RequestTagContainer(["status.stunned"])
           )
       ]
   );

   // Apply the effect to the entity
   var effect = new Effect(stunEffectData, new EffectOwnership(caster, caster));
   entity.EffectsManager.ApplyEffect(effect);
   ```

During the effect's lifetime, the tag is added to `ModifierTags` with a reference count. When the effect expires, the reference count decreases, and when it reaches zero, the tag is removed from `ModifierTags`.

## Best Practices

1. **Serialize Tag Definitions**:
   - Store your game's tags in data files (JSON, XML, etc.).
   - Load during initialization to avoid hard-coded tag strings.

2. **Single TagsManager**:
   - Use one `TagsManager` for your entire game.
   - Initialize it early in your game's startup sequence.

3. **Plan Tag Hierarchies**:
   - Design meaningful hierarchies (e.g., `ability.melee`, `ability.ranged`).
   - Use consistent naming conventions.

4. **Let Effects Manage Tags**:
   - Prefer using gameplay effects to add/remove tags during gameplay.
   - This ensures proper lifecycle management of temporary tags.

5. **Use Consistent Prefixes**:
   - `cues.*` for visual/audio feedback tags.
   - `status.*` for character statuses.
   - `ability.*` for ability-related tags.

6. **Use `CombinedTags` for Checks**:
   - When checking if an entity has a tag, use `entity.Tags.CombinedTags` rather than accessing `BaseTags` or `ModifierTags` directly.
