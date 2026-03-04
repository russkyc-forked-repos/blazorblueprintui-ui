using System.Diagnostics.CodeAnalysis;
using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBlueprint.Components;

/// <summary>
/// A feature-rich, accessible sortable list or grid component that supports
/// drag-and-drop reordering within itself and cross-zone transfers via
/// <see cref="BbDropContainer{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Standalone reorder</strong> — place <c>BbSortable</c> by itself to get an
/// in-list drag-to-reorder experience. Handle <see cref="OnItemDropped"/> to update your list.
/// </para>
/// <para>
/// <strong>Cross-list (kanban)</strong> — wrap two or more <c>BbSortable</c> instances
/// (and optionally <see cref="BbDropZone{T}"/> sinks) inside a shared
/// <see cref="BbDropContainer{T}"/>. Give each a unique <see cref="ZoneIdentifier"/> so
/// items can be dragged between them.
/// </para>
/// <para>
/// <strong>Templates</strong> — supply <see cref="ItemTemplate"/> for normal item appearance
/// and <see cref="DragItemTemplate"/> for an alternate look while the item is being dragged
/// (a "ghost" placeholder shown at the item's original position).
/// </para>
/// <para>
/// <strong>Animations</strong> — items use <c>data-dragging="true"</c> and the zone uses
/// <c>data-drag-over="true"</c> so you can layer CSS transitions on top. Built-in drop
/// indicators use the <c>tw-animate-css</c> <c>animate-in fade-in-0</c> convention.
/// </para>
/// <para>
/// <strong>Keyboard</strong> — every item is focusable. Press <kbd>Space</kbd> to grab,
/// <kbd>↑</kbd>/<kbd>↓</kbd> (list) or <kbd>↑↓←→</kbd> (grid) to move, <kbd>Space</kbd>
/// again to drop, <kbd>Escape</kbd> to cancel.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item in the sortable list. Must be non-null.</typeparam>
public partial class BbSortable<T> : ComponentBase, IDisposable where T : notnull
{
    // ── Standalone drag state (used when no BbDropContainer is present) ──────────
#pragma warning disable CS8601
    [MaybeNull]
    private T _localDraggedItem = default;
#pragma warning restore CS8601

    private string _localSourceZone = string.Empty;
    private int _localSourceIndex;

    // ── Zone visual state ─────────────────────────────────────────────────────────
    private int _dragOverCount;
    private int _mouseDropIndex = -1;    // updated by ondragenter on items  (mouse DnD)
    private int _keyboardTargetIndex = -1; // updated by arrow keys              (keyboard DnD)
    private string _announcement = string.Empty;

    // ── JS interop / animation ────────────────────────────────────────────────────
    private const int FlipAnimationDurationMs = 220;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<BbSortable<T>>? _dotNetRef;
    private ElementReference _containerRef;
    private bool _needsFlip;
    private string? _initializedHandleClass;

    // ── Live reordering ───────────────────────────────────────────────────────────
    /// <summary>Maintains a live preview ordering of items during same-zone drag.</summary>
    private List<T>? _livePreviewList;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    // ─────────────────────────────────────────────────────────────────────────────
    // Cascading parameter
    // ─────────────────────────────────────────────────────────────────────────────

    [CascadingParameter]
    private BbDropContainer<T>? Container { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // Parameters
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the unique identifier for this sortable zone.
    /// Required when inside a <see cref="BbDropContainer{T}"/> to distinguish between zones.
    /// </summary>
    [Parameter]
    public string ZoneIdentifier { get; set; } = "default";

    /// <summary>
    /// Gets or sets the collection of items to display and sort.
    /// The collection is not mutated; use <see cref="OnItemDropped"/> to update it.
    /// </summary>
    [Parameter]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Gets or sets the template used to render each item in its normal state.
    /// The template receives the item instance as its <c>context</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets an optional template rendered in place of <see cref="ItemTemplate"/>
    /// while the item is actively being dragged. Useful for a "ghost" placeholder shown
    /// at the item's original position during the drag.
    /// When omitted the item is dimmed via CSS (<c>data-dragging="true"</c>).
    /// </summary>
    [Parameter]
    public RenderFragment<T>? DragItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets a template rendered when <see cref="Items"/> is empty.
    /// Use this to display a custom empty-state message or call-to-action.
    /// The template is hidden automatically while a drag is in progress so the
    /// zone remains a valid drop target.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// Gets or sets the layout mode.
    /// <see cref="SortableLayout.List"/> stacks items vertically;
    /// <see cref="SortableLayout.Grid"/> arranges them in a CSS grid
    /// (control columns via <see cref="ContainerClass"/>).
    /// </summary>
    [Parameter]
    public SortableLayout Layout { get; set; } = SortableLayout.List;

    /// <summary>
    /// Gets or sets whether items can be reordered within this zone by dragging.
    /// When <c>false</c> the sortable acts only as a cross-zone drag source.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool AllowReorder { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this sortable accepts items dragged from <em>other</em> zones.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool AllowExternalDrop { get; set; } = true;

    /// <summary>
    /// Gets or sets a per-zone predicate that overrides the container-level
    /// <see cref="BbDropContainer{T}.CanDrop"/> check for items targeting this sortable.
    /// Return <c>true</c> to allow the drop, <c>false</c> to reject it.
    /// </summary>
    [Parameter]
    public Func<T, bool>? CanDrop { get; set; }

    /// <summary>
    /// Gets or sets a predicate that marks individual items as non-draggable.
    /// Disabled items cannot be picked up but receive drops above them normally.
    /// </summary>
    [Parameter]
    public Func<T, bool>? ItemDisabled { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after an item is dropped.
    /// The <see cref="DropItemInfo{T}"/> provides item, source zone, target zone, and
    /// the <see cref="DropItemInfo{T}.TargetIndex"/> where the item should be inserted.
    /// </summary>
    [Parameter]
    public EventCallback<DropItemInfo<T>> OnItemDropped { get; set; }

    /// <summary>
    /// Gets or sets any additional content appended after the rendered items.
    /// Useful for empty-state messages or "add item" buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the accessible label for the sortable region
    /// (exposed as <c>aria-label</c> on the list/grid element).
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the outer zone wrapper element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the inner item-container element
    /// that carries <c>role="list"</c> or <c>role="grid"</c>.
    /// Use this to set grid column count, gap, padding, etc.
    /// </summary>
    [Parameter]
    public string? ContainerClass { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to every item wrapper element.
    /// </summary>
    [Parameter]
    public string? ItemClass { get; set; }

    /// <summary>
    /// Gets or sets whether items in this sortable can be dragged at all.
    /// When <c>false</c> all items become non-draggable and cannot be picked up.
    /// External drops from other zones are still accepted when <see cref="AllowExternalDrop"/> is <c>true</c>.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Sorting { get; set; } = true;

    /// <summary>
    /// Gets or sets the CSS class name of the element inside each item template that acts as the
    /// drag handle. When set, only clicking on an element with this class initiates the drag;
    /// clicking anywhere else on the item does nothing.
    /// When <c>null</c> (default) the entire item is draggable.
    /// </summary>
    [Parameter]
    public string? HandleClass { get; set; }

    /// <summary>
    /// Gets or sets whether dropping an item on another item swaps their positions
    /// instead of inserting before the target.
    /// When <c>true</c> the <see cref="DropItemInfo{T}.TargetIndex"/> represents the swap partner's index
    /// and <see cref="DropItemInfo{T}.SourceIndex"/> is the dragged item's original position.
    /// Defaults to <c>false</c> (insert mode).
    /// </summary>
    [Parameter]
    public bool Swap { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Container is not null)
        {
            Container.TransactionStarted += OnContextTransactionChanged;
            Container.TransactionEnded += OnContextTransactionChanged;
        }
    }

    private void OnContextTransactionChanged(object? sender, EventArgs e) => StateHasChanged();

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/BlazorBlueprint.Components/js/drag-drop.js");

                if (HandleClass is not null)
                {
                    await InitHandlesAsync();
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or InvalidOperationException)
            {
                // JS interop not available (prerendering or circuit disconnect)
            }

            return;
        }

        // Re-initialize handles when HandleClass changes after first render
        if (HandleClass != _initializedHandleClass && _jsModule is not null)
        {
            try
            {
                await InitHandlesAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }

        if (_needsFlip && _jsModule is not null)
        {
            _needsFlip = false;

            try
            {
                await _jsModule.InvokeVoidAsync("playFlip", _containerRef, FlipAnimationDurationMs);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Container is not null)
        {
            Container.TransactionStarted -= OnContextTransactionChanged;
            Container.TransactionEnded -= OnContextTransactionChanged;
        }

        _dotNetRef?.Dispose();

        if (_jsModule is not null)
        {
            _ = _jsModule.DisposeAsync().AsTask();
        }

        GC.SuppressFinalize(this);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // State helpers
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the parent container when one exists, otherwise null.</summary>
    private BbDropContainer<T>? GetEffectiveContext() => Container;

    private bool IsInDragMode =>
        (Container?.HasTransaction ?? false) || _localDraggedItem is not null;

    [return: MaybeNull]
    private T GetDraggedItem() => Container is not null ? Container.DraggedItem : _localDraggedItem;

    private string GetSourceZone() => Container?.SourceZone ?? _localSourceZone;

    private bool IsCurrentDraggedItem(T item)
    {
        var dragged = GetDraggedItem();
        return dragged is not null && dragged.Equals(item);
    }

    private bool HasActiveTransaction() =>
        Container?.HasTransaction ?? _localDraggedItem is not null;

    private IReadOnlyList<T> GetZoneItems()
    {
        // During same-zone drag return live preview list so items animate to new positions
        if (_livePreviewList is not null && GetSourceZone() == ZoneIdentifier && IsInDragMode)
        {
            return _livePreviewList;
        }

        return Items;
    }

    private bool CanDropCurrentItem()
    {
        var item = GetDraggedItem();
        if (item is null)
        {
            return false;
        }

        var sourceZone = GetSourceZone();

        // Never accept a same-zone drop when reordering is disabled
        if (!AllowReorder && sourceZone == ZoneIdentifier)
        {
            return false;
        }

        // Block external drops when the flag is off
        if (!AllowExternalDrop && sourceZone != ZoneIdentifier)
        {
            return false;
        }

        if (CanDrop is not null)
        {
            return CanDrop(item);
        }

        return Container?.CanDropItem(ZoneIdentifier) ?? true;
    }

    private bool IsItemDraggable(T item) => Sorting && !IsItemDisabled(item);

    private bool IsItemDisabled(T item)
    {
        if (ItemDisabled?.Invoke(item) == true)
        {
            return true;
        }

        return Container?.IsItemDisabled(item) ?? false;
    }

    private int GetActiveDropIndex() =>
        _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : _mouseDropIndex;

    private bool ShouldShowDropIndicator(int position)
    {
        if (!IsInDragMode || !CanDropCurrentItem())
        {
            return false;
        }

        // Live reorder (same zone): no indicator — the list itself shows position
        if (_livePreviewList is not null && GetSourceZone() == ZoneIdentifier && AllowReorder && Sorting)
        {
            return false;
        }

        // Swap mode: no insertion-line indicator — swap target is highlighted on the item
        if (Swap)
        {
            return false;
        }

        var idx = GetActiveDropIndex();
        if (idx < 0)
        {
            return false;
        }

        // Skip indicator that would be immediately adjacent to the dragged item's own slot
        // (it would result in a no-op move within the same zone)
        if (GetSourceZone() == ZoneIdentifier && idx < Items.Count && IsCurrentDraggedItem(Items[idx]))
        {
            return false;
        }

        return idx == position;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Transaction helpers (delegates to container when present, self otherwise)
    // ─────────────────────────────────────────────────────────────────────────────

    private void StartLocalTransaction(T item, int index)
    {
        if (Container is not null)
        {
            Container.StartTransaction(item, ZoneIdentifier, index);
            return;
        }

        _localDraggedItem = item;
        _localSourceZone = ZoneIdentifier;
        _localSourceIndex = index;
        Announce($"Grabbed item at position {index + 1} of {Items.Count}. "
            + "Use arrow keys to move. Press Space or Enter to drop, Escape to cancel.");
    }

    private async Task CommitLocalTransactionAsync(int targetIndex)
    {
        if (Container is not null)
        {
            await Container.CommitTransactionAsync(ZoneIdentifier, targetIndex);
            return;
        }

        if (_localDraggedItem is null)
        {
            return;
        }

        var item = _localDraggedItem;
        var sourceZone = _localSourceZone;
        var sourceIndex = _localSourceIndex;
#pragma warning disable CS8601
        _localDraggedItem = default;
#pragma warning restore CS8601
        _localSourceZone = string.Empty;
        _livePreviewList = null;
        Announce("Dropped.");
        await OnItemDropped.InvokeAsync(new DropItemInfo<T>(item, sourceZone, ZoneIdentifier, targetIndex, sourceIndex));
    }

    private void CancelLocalTransaction()
    {
        if (Container is not null)
        {
            Container.CancelTransaction();
            return;
        }

        if (_localDraggedItem is null)
        {
            return;
        }

#pragma warning disable CS8601
        _localDraggedItem = default;
#pragma warning restore CS8601
        _localSourceZone = string.Empty;
        _livePreviewList = null;
        Announce("Drag cancelled.");
    }

    private void Announce(string message)
    {
        _announcement = message;
        StateHasChanged();
    }

    private async Task CaptureFlipPositionsAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("capturePositions", _containerRef);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }
    }

    private async Task InitHandlesAsync()
    {
        _initializedHandleClass = HandleClass;
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("initHandles", _containerRef, HandleClass);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Zone-level drag event handlers
    // ─────────────────────────────────────────────────────────────────────────────

    private void HandleZoneDragEnter(DragEventArgs args)
    {
        _dragOverCount++;
        // Default insertion point: append to end
        if (_mouseDropIndex < 0 && IsInDragMode)
        {
            _mouseDropIndex = Items.Count;
        }
    }

    private void HandleZoneDragLeave(DragEventArgs args)
    {
        _dragOverCount = Math.Max(0, _dragOverCount - 1);
        if (_dragOverCount == 0)
        {
            _mouseDropIndex = -1;
        }
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Blazor event handler must be instance method.")]
    private void HandleZoneDragOver(DragEventArgs args)
    {
        // preventDefault is handled by the :preventDefault directive
    }

    private async Task HandleZoneDrop(DragEventArgs args)
    {
        var draggedItem = GetDraggedItem();
        var sourceZone  = GetSourceZone();

        // Determine drop index
        int dropIdx;
        if (_livePreviewList is not null && sourceZone == ZoneIdentifier && draggedItem is not null)
        {
            // Live reorder: use the item's current position in the preview list
            dropIdx = _livePreviewList.IndexOf(draggedItem);
            if (dropIdx < 0)
            {
                dropIdx = Items.Count;
            }
        }
        else
        {
            dropIdx = _mouseDropIndex >= 0 ? _mouseDropIndex : Items.Count;
        }

        _livePreviewList = null;
        _dragOverCount   = 0;
        _mouseDropIndex  = -1;

        await StopLiveSortAsync();

        if (!HasActiveTransaction() || !CanDropCurrentItem())
        {
            return;
        }

        await CaptureFlipPositionsAsync();
        _needsFlip = true;

        await CommitLocalTransactionAsync(dropIdx);
        // Container.CommitTransactionAsync fires Container.ItemDropped.
        // Standalone CommitLocalTransactionAsync fires OnItemDropped directly.
        // No additional firing needed here — doing so would double-invoke the handler
        // when the caller has wired both Container.ItemDropped and zone.OnItemDropped.
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Item-level drag event handlers
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task HandleItemDragStartAsync(T item, int index)
    {
        if (!Sorting)
        {
            return;
        }

        _keyboardTargetIndex = -1;
        StartLocalTransaction(item, index);

        // Initialise live-reorder preview for same-zone dragging (not swap mode)
        if (AllowReorder && !Swap)
        {
            _livePreviewList = new List<T>(Items);
            await StartLiveSortAsync();
        }
    }

    private async Task HandleItemDragEndAsync(DragEventArgs args)
    {
        // dragend fires even after a successful drop — cancel is a no-op when already committed
        CancelLocalTransaction();
        _mouseDropIndex = -1;
        _dragOverCount = 0;
        _livePreviewList = null;
        await StopLiveSortAsync();
    }

    // Track static drop position for swap mode and external drops.
    // Live same-zone reordering is now handled by JS coordinate tracking
    // (startLiveSort → UpdateLiveIndexAsync), which avoids the cascading
    // dragenter events that fire when items shift position during re-render.
    private void HandleItemDragEnter(int position) => _mouseDropIndex = position;

    /// <summary>
    /// Called from JavaScript (via <c>startLiveSort</c>) when the cursor crosses an item
    /// midpoint, with the new insertion index computed from pointer coordinates.
    /// This is the SortableJS-style approach: position is derived from stable container-
    /// level dragover geometry, not per-item dragenter events that cascade when items shift.
    /// </summary>
    [JSInvokable]
    public async Task UpdateLiveIndexAsync(int newIndex)
    {
        var draggedItem = GetDraggedItem();
        var sourceZone  = GetSourceZone();

        if (_livePreviewList is null || draggedItem is null || sourceZone != ZoneIdentifier
            || !AllowReorder || !Sorting || Swap)
        {
            return;
        }

        // Snapshot current DOM positions BEFORE mutating the list (for FLIP)
        await CaptureFlipPositionsAsync();

        _livePreviewList.Remove(draggedItem);
        var insertIdx = Math.Clamp(newIndex, 0, _livePreviewList.Count);
        _livePreviewList.Insert(insertIdx, draggedItem);
        _needsFlip = true;

        // InvokeAsync marshals the state change onto the Blazor synchronisation context
        await InvokeAsync(StateHasChanged);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Live-sort JS helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task StartLiveSortAsync()
    {
        if (_jsModule is null) { return; }
        _dotNetRef ??= DotNetObjectReference.Create(this);
        var isGrid = Layout == SortableLayout.Grid;
        try
        {
            await _jsModule.InvokeVoidAsync("startLiveSort", _containerRef, _dotNetRef, isGrid);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
    }

    private async Task StopLiveSortAsync()
    {
        if (_jsModule is null) { return; }
        try
        {
            await _jsModule.InvokeVoidAsync("stopLiveSort", _containerRef);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Keyboard drag-and-drop
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task HandleItemKeyDown(KeyboardEventArgs e, T item, int index)
    {
        if (IsItemDisabled(item))
        {
            return;
        }

        switch (e.Key)
        {
            // ── Space / Enter: grab or drop ────────────────────────────────────
            case " " or "Enter" when IsCurrentDraggedItem(item) && HasActiveTransaction():
            {
                var dropIdx = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = -1;
                await CaptureFlipPositionsAsync();
                _needsFlip = true;
                await CommitLocalTransactionAsync(dropIdx);
                break;
            }

            case " " or "Enter" when !HasActiveTransaction():
            {
                _keyboardTargetIndex = index;
                StartLocalTransaction(item, index);
                break;
            }

            // ── Escape: cancel ──────────────────────────────────────────────────
            case "Escape" when HasActiveTransaction():
            {
                _keyboardTargetIndex = -1;
                CancelLocalTransaction();
                break;
            }

            // ── List: arrow up / Grid: arrow left ───────────────────────────────
            case "ArrowUp" when IsCurrentDraggedItem(item) && HasActiveTransaction() && AllowReorder && Layout == SortableLayout.List:
            case "ArrowLeft" when IsCurrentDraggedItem(item) && HasActiveTransaction() && AllowReorder && Layout == SortableLayout.Grid:
            {
                var current = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = Math.Max(0, current - 1);
                Announce($"Position {_keyboardTargetIndex + 1} of {Items.Count}. Press Space to drop here.");
                break;
            }

            // ── List: arrow down / Grid: arrow right ────────────────────────────
            case "ArrowDown" when IsCurrentDraggedItem(item) && HasActiveTransaction() && AllowReorder && Layout == SortableLayout.List:
            case "ArrowRight" when IsCurrentDraggedItem(item) && HasActiveTransaction() && AllowReorder && Layout == SortableLayout.Grid:
            {
                var current = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = Math.Min(Items.Count, current + 1);
                Announce($"Position {_keyboardTargetIndex + 1} of {Items.Count}. Press Space to drop here.");
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CSS class helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private string ZoneClass => ClassNames.cn("relative", Class);

    private string ContainerCssClass => ClassNames.cn(
        Layout == SortableLayout.List ? "flex flex-col" : "grid",
        "gap-2",
        // Ensure empty zones remain a droppable target area during drag
        IsInDragMode && CanDropCurrentItem() && Items.Count == 0 ? "min-h-[3rem]" : null,
        ContainerClass
    );

    private string GetItemWrapperClass(T item, int position)
    {
        var isDragging = IsCurrentDraggedItem(item);
        var isDisabled = IsItemDisabled(item);

        // In swap mode, highlight the item the dragged item would swap with
        var isSwapTarget = Swap
            && IsInDragMode
            && CanDropCurrentItem()
            && !isDragging
            && GetActiveDropIndex() == position;

        // In clone mode (container-level), don't dim the dragged item at its source
        var cloneDimming = Container?.Clone == true;

        return ClassNames.cn(
            "outline-none transition-all duration-200",
            IsItemDraggable(item) ? "cursor-grab active:cursor-grabbing" : null,
            isDisabled ? "opacity-50 cursor-not-allowed" : null,
            isDragging && !cloneDimming ? "opacity-40 scale-[0.98]" : null,
            isSwapTarget ? "ring-2 ring-primary ring-offset-2 rounded-sm" : null,
            // Keyboard focus ring consistent with the rest of the library
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded-sm",
            ItemClass
        );
    }

    private string GetDropIndicatorClass() =>
        Layout == SortableLayout.Grid
            ? "rounded-lg border-2 border-dashed border-primary/60 bg-primary/5 animate-in fade-in-0 duration-150 min-h-16"
            : "h-0.5 rounded-full bg-primary animate-in fade-in-0 slide-in-from-top-1 duration-150 mx-0.5 my-0.5";

    private string GetAriaRole() =>
        Layout == SortableLayout.Grid ? "grid" : "list";
}
