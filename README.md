# Better Sidebar

This is a mod for Stacklands and it aims to improve the usability of the sidebar.

## Features
### Pin Your Favorite Ideas
- Click on an idea in the sidebar to pin or unpin it. An unpinned idea will wait until you move your mouse away or pin it again before it hides itself.
- Pinned ideas will display at the top of each group in the order they got pinned.
- Click on the "Pinned" button to filter out unpinned ideas. Click on the "Reset Pin" button to reset all ideas to unpinned.

### Advanced Quick Search
- Quick: Click the middle mouse button on a card, and an advanced search for relevant ideas starts. Search results will be marked with a "Q" letter on the right.
- The first search result will display its info in the bottom-left panel. Click the middle mouse button again to display next search result (if any).
- It defaults to search for ideas that produce the clicked card. Hold [Alt] when clicking the card to search for ideas using the clicked card as ingredients. (You can set the default search mode in the mod configuration.)
- Advanced: Instead of doing a text search, (as a result, it works without any input to the search bar), it searches for ideas that has a direct link with the clicked card.
- In other words, the search is based on relationships written in code. An idea will only show up if and only if it does generate or use the clicked card.

### Filter Option
- Right below the search bar input, there is an expandable filter option. It has various options that allow you to display only ideas satisfying the filter.
- "Pinned" filters ideas with pins. "Quick" filter ideas that are results of the quick search. "New" filters new ideas.
"Reset Pin" allows you reset all pins.

## Change Log
- v0.1.2
  - Add filter options to provide a better filtering experience.
  - Bugfix: quick search data initialization no longer depends on original game procedures. Nullreference exeptions should not occur again.
  - Bigfix: non-DLC player can run this mod as well.
- v0.1.1
  - Allow turning off the advanced quick search in the mod configuration.
  - 2023.8.17 Temporary turn off the quick search by default. Waiting for bugfix.
- v0.1.0 Implement advanced quick search, update mod configuration UI.
- v0.0.2 Fix behaviors when closing an expanded group, and when searching under the pinned only mode.
- v0.0.1 Inital release.
