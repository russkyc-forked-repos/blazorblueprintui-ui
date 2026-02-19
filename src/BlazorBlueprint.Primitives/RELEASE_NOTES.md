## What's New in v3.0.0

### Breaking Changes
- All Razor components now use the `Bb` prefix (e.g., `Dialog` -> `BbDialog`, `Select` -> `BbSelect`)
- `BlazorBlueprint.Primitives.*` consumer-facing types (`AccordionType`, `SheetSide`, `SortDirection`, `PaginationState`, `PopoverSide`, `PopoverAlign`, `PopoverPlacement`, `PositioningStrategy`) moved to root `BlazorBlueprint.Primitives` namespace
- `PositioningOptions.Strategy` changed from `string` to `PositioningStrategy` enum
- `SelectItem.TextValue` renamed to `SelectItem.Text`
- `IPortalService` redesigned with two-layer portal architecture: `PortalCategory.Container` and `PortalCategory.Overlay`
- `RegisterPortal(id, content)` replaced by `RegisterPortal(id, content, category)`
- `GetPortals()` replaced by `GetPortals(PortalCategory)`
- `OnPortalsChanged` replaced by `OnPortalsCategoryChanged`

### New Components & Sub-Components
- `BbCheckboxIndicator` — auto-renders check/indeterminate SVG icon based on parent state, with configurable `Size` and `StrokeWidth`
- `BbSwitchThumb` — auto-syncs `data-state` attribute from parent `BbSwitch` via cascading parameter
- `BbFloatingPortal` — unified floating content infrastructure with `ForceMount` (default: `true`) and `data-state` attribute for CSS animations
- `BbCategoryPortalHost`, `BbContainerPortalHost`, `BbOverlayPortalHost` — category-scoped portal hosts for efficient rendering
- `BbPortalHost` — convenience wrapper rendering both Container and Overlay portals

### New Features
- `ItemClass` parameter on `BbDropdownMenu`, `BbRadioGroup`, and `BbSelect` — cascades CSS classes to all child items
- `[CascadingTypeParameter]` on `BbSelect<TValue>` — child components infer `TValue` from parent
- `Href` and `Target` parameters on `BbDropdownMenuItem` — renders as `<a>` for link items
- `ISelectDisplayContext` interface for non-generic display text access
- `PortalCategory` enum for scoped portal rendering
- `IPortalService.HasHost` property and `RegisterHost()`/`UnregisterHost()` methods
- `ForceMount` on `BbFloatingPortal` — keeps portal content mounted when closed, eliminating re-mount overhead

### Performance
- Menu keyboard navigation moved from C# to JavaScript via `menu-keyboard.js` — zero interop round-trips for arrow keys, Home, End, Enter, Space
- Dialog, Sheet, and Drawer escape key handling moved to JavaScript via `escape-keydown.js`
- `BbFloatingPortal` defaults to `ForceMount=true` — no re-mount overhead on open/close cycles
- `FocusManager.GetModuleAsync()` now thread-safe with `SemaphoreSlim`

### Bug Fixes
- `autoUpdate` callback in positioning.js now guards against stale DOM elements during Blazor render cycles
- `FocusManager` and `PositioningService` disposal now catches `JSDisconnectedException` for Blazor Server circuit disconnect safety
- Select display text state change optimization to prevent unnecessary re-renders
