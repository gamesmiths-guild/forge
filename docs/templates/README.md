# Documentation Templates

Skeletons for the per-item reference pages in this documentation. Copy the matching template, fill in the placeholders, and drop the result into the folder listed below.

These are authoring tools — they are not part of the reader-facing documentation and nothing links to them from the docs themselves.

| Template | Use for | Output goes to |
|----------|---------|----------------|
| [action-node-template.md](action-node-template.md) | Statescript action nodes | [statescript/nodes/action/](../statescript/nodes/action/) |
| [condition-node-template.md](condition-node-template.md) | Statescript condition nodes | [statescript/nodes/condition/](../statescript/nodes/condition/) |
| [state-node-template.md](state-node-template.md) | Statescript state nodes | [statescript/nodes/state/](../statescript/nodes/state/) |
| [resolver-template.md](resolver-template.md) | Statescript property resolvers | [statescript/resolvers/](../statescript/resolvers/) |
| [effect-component-template.md](effect-component-template.md) | Effect components | [effects/components/](../effects/components/) |

## Conventions

- **File name** is the class name in kebab-case: `TagQueryResolver` → `tag-query-resolver.md`, `BlockAbilityTagsEffectComponent` → `block-ability-tags-effect-component.md`.
- **Placeholders** are written as `{Placeholder}`. Every one must be replaced or the row/section removed.
- **Blockquote notes** in a template are instructions to you, the author. Delete them from the finished page.
- **Relative links** inside a template resolve from the template's own location, but links in the template *body* are written for the finished page's location. Re-check them after copying.
- After adding a page, add its row to the index table in the destination folder's `README.md`.
