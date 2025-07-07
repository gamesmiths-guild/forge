# Tags System

The Tags system in Forge provides a hierarchical mechanism for metadata, allowing efficient categorization and queries of game entities and systems. It's a foundational component that many other Forge systems rely on.

## Core Components

### Tag

A `Tag` is a string-based identifier that typically uses dot notation to define hierarchies (e.g., `enemy.undead.zombie`). In Forge, tags are not created directly but requested from the `TagsManager`.

```csharp
// Proper way to get a tag reference
var zombieTag = Tag.RequestTag(tagsManager, "enemy.undead.zombie");
```

### TagsManager

The `TagsManager` is the central registry for all tags in your game. It:
- Maintains canonical tag instances
- Resolves parent-child relationships
- Provides efficient tag lookup and comparison

#### Initialization and Immutability

Tags should be registered with the TagsManager during initialization, typically from a serialized file:

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

**Important:** The TagsManager's tag registry should be immutable during gameplay. All tags should be registered during initialization to maintain efficiency and prevent runtime errors.

#### Single Instance vs. Multiple Managers

Typically, you should use **one TagsManager** for your entire game. This enables:
- Consistent tag usage across systems
- Efficient memory usage
- Simplified tag queries

While not strictly prohibited, using multiple TagsManagers means:
- Tags from different managers cannot be mixed
- Effects using tags from one manager can't be applied to entities using another
- Each manager maintains its own hierarchy

### TagContainer

`TagContainer` stores and manages a collection of tags on entities or components:

```csharp
// Create a tag container
var container = new TagContainer(tagsManager);

// Add tags to the container
container.AddTag(Tag.RequestTag(tagsManager, "enemy.undead.zombie"));

// Check if a tag exists in the container
bool isZombie = container.HasTag(Tag.RequestTag(tagsManager, "enemy.undead.zombie"));
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

- **BaseTags**: Permanent tags assigned to the entity (typically during creation)
- **ModifierTags**: Temporary tags applied through effects with reference counting
- **CombinedTags**: Unified view of both BaseTags and ModifierTags for queries

Typically, game code should use `CombinedTags` for tag checks:

```csharp
// Check if entity has a tag (correctly checks both base and modifier tags)
bool isStunned = entity.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
```

**Note:** While the EntityTags class provides methods for adding and removing tags, these are internal. Tags should be modified through proper channels: base tags during entity initialization and modifier tags through the Effects system.

## Hierarchical Tag Structure

Tags in Forge use dot notation to establish parent-child relationships:

- `enemy` (parent)
  - `enemy.undead` (child of enemy, parent of zombie)
    - `enemy.undead.zombie` (child of enemy.undead)

When checking for a tag, parent tags are considered to be present if any of their children are present.

## Tag Queries

TagQueries provide powerful logical operations for matching tag containers against complex conditions. They allow you to create sophisticated rules using nested logical expressions.

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
  - `AnyTagsMatch` - At least one tag must match (hierarchical)
  - `AllTagsMatch` - All tags must match (hierarchical)
  - `NoTagsMatch` - No tags may match (hierarchical)
  - `AnyTagsMatchExact` - At least one tag must match exactly
  - `AllTagsMatchExact` - All tags must match exactly
  - `NoTagsMatchExact` - No tags may match exactly

- **Expression Matching**
  - `AnyExpressionsMatch` - At least one nested expression must match
  - `AllExpressionsMatch` - All nested expressions must match
  - `NoExpressionsMatch` - No nested expressions may match

### Use Cases

- **Ability Requirements**: Check if an entity meets specific tag requirements
- **Target Filtering**: Filter potential targets based on complex criteria
- **State-Dependent Behavior**: Execute different behavior based on entity state
- **AI Decision Making**: Make AI decisions based on target properties

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

It's recommended to prefix gameplay cue tags with `cues.` to clearly distinguish them from gameplay tags:

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
    public EntityAttributes Attributes { get; }
    public EntityTags Tags { get; }
    public EffectsManager EffectsManager { get; }

    public GameEntity(TagsManager tagsManager, CuesManager cuesManager)
    {
        // Create attribute set
        var attributeSet = new CombatAttributeSet();

        // Initialize tags
        var originalTags = new TagContainer(
            tagsManager,
            [
                Tag.RequestTag(tagsManager, "character.player"),
                Tag.RequestTag(tagsManager, "class.warrior")
            ]);

        // Setup entity components
        EffectsManager = new(this, cuesManager);
        Attributes = new(attributeSet);
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
   ```

2. **Through Gameplay Effects**
   ```csharp
   // Create effect that applies a tag
   var stunEffect = new GameplayEffect(
       duration: 3.0f,
       tagsToAdd: new[] { Tag.RequestTag(tagsManager, "status.stunned") }
   );

   // Apply the effect to the entity
   entity.EffectsManager.ApplyGameplayEffect(stunEffect);
   ```

During the effect's lifetime, the tag is added to ModifierTags with a reference count. When the effect expires, the reference count decreases, and when it reaches zero, the tag is removed from ModifierTags.

## Best Practices

1. **Serialize Tag Definitions**
   - Store your game's tags in data files (JSON, XML, etc.)
   - Load during initialization to avoid hard-coded tag strings

2. **Single TagsManager**
   - Use one TagsManager for your entire game
   - Initialize it early in your game's startup sequence

3. **Plan Tag Hierarchies**
   - Design meaningful hierarchies (ability.melee, ability.ranged, etc.)
   - Use consistent naming conventions

4. **Use Tag Constants**
   - For commonly used tags, define constants to prevent typos
   ```csharp
   public static class GameTags
   {
       public const string Enemy = "enemy";
       public const string EnemyUndead = "enemy.undead";
       public const string EnemyUndeadZombie = "enemy.undead.zombie";
   }
   ```

5. **Let Effects Manage Tags**
   - Prefer using gameplay effects to add/remove tags during gameplay
   - This ensures proper lifecycle management of temporary tags

6. **Use Consistent Prefixes**
   - `cues.*` for visual/audio feedback tags
   - `status.*` for character statuses
   - `ability.*` for ability-related tags

7. **Use CombinedTags for Checks**
   - When checking if an entity has a tag, use `entity.Tags.CombinedTags` rather than accessing BaseTags or ModifierTags directly
