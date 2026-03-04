using System.Diagnostics.CodeAnalysis;
using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBlueprint.Components;

/// <summary>
/// A versatile drag-and-drop zone that can act as a sortable list/grid, a drop-zone sink,
/// or both. Powered by SortableJS for stable, smooth drag-and-drop behaviour.
/// Supports keyboard reordering, handles, swap mode, clone, and cross-zone transfers via
/// <see cref="BbDropContainer{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sortable list (default)</strong> — place <c>BbSortable</c> standalone for
/// in-list drag-to-reorder. Handle <see cref="OnItemDropped"/> to update your list.
/// </para>
/// <para>
/// <strong>Drop-zone sink</strong> — set <see cref="DropZone"/> to <c>true</c> (or omit
/// <see cref="Items"/>) inside a <see cref="BbDropContainer{T}"/> to turn the component
/// into a sink-only drop target with dashed-border visual feedback. Provide
/// <see cref="ChildContent"/> for custom idle content or use the built-in drag-state icons.
/// </para>
/// <para>
/// <strong>Cross-list (kanban)</strong> — wrap two or more <c>BbSortable</c> instances
/// inside a shared <see cref="BbDropContainer{T}"/> with the same <see cref="Group"/>.
/// Handle drops container-wide via <see cref="BbDropContainer{T}.ItemDropped"/> and/or
/// per-zone via <see cref="OnItemDropped"/>.
/// </para>
/// <para>
/// <strong>Keyboard</strong> — every item is focusable. Press <kbd>Space</kbd> to grab,
/// <kbd>↑</kbd>/<kbd>↓</kbd> (list) or <kbd>↑↓←→</kbd> (grid) to move, <kbd>Space</kbd>
/// again to drop, <kbd>Escape</kbd> to cancel.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item in the sortable list. Must be non-null.</typeparam>
public partial class BbSortable<T> : ComponentBase, IAsyncDisposable where T : notnull
{
    // ── Standalone drag state (used when no BbDropContainer is present) ──────────
#pragma warning disable CS8601
    [MaybeNull]
    private T _localDraggedItem = default;
#pragma warning restore CS8601

    private string _localSourceZone  = string.Empty;
    private int    _localSourceIndex;

    // ── Keyboard drag state ───────────────────────────────────────────────────────
    private int    _keyboardTargetIndex = -1;
    private string _announcement = string.Empty;

    // ── Zone visual state ─────────────────────────────────────────────────────────
    private int _dragOverCount;

    // ── JS interop ────────────────────────────────────────────────────────────────
    private IJSObjectReference?            _jsModule;
    private DotNetObjectReference<BbSortable<T>>? _dotNetRef;
    private ElementReference              _containerRef;
    private string?                       _initializedHandleClass;
    private bool                          _sortableInitialized;
    private string?                       _prevZoneId;

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
    /// Gets or sets the unique identifier for this zone.
    /// Required when used inside a <see cref="BbDropContainer{T}"/> for cross-zone drag.
    /// Also used as the SortableJS <c>group</c> name when <see cref="Group"/> is not set.
    /// </summary>
    [Parameter]
    public string ZoneIdentifier { get; set; } = "default";

    /// <summary>
    /// Gets or sets the SortableJS group name for cross-zone drag.
    /// When omitted the <see cref="ZoneIdentifier"/> value is used as the group.
    /// All zones that share the same group name (and the same <see cref="BbDropContainer{T}"/>)
    /// can exchange items.
    /// </summary>
    [Parameter]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the list of items to render and sort.
    /// </summary>
    [Parameter]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Gets or sets the template used to render each item.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets an alternate template shown in the item's original slot while it
    /// is being dragged (a ghost placeholder). Falls back to <see cref="ItemTemplate"/>
    /// when not set.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? DragItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template rendered when <see cref="Items"/> is empty and no drag
    /// is in progress. Hidden automatically during a drag so the zone stays a drop target.
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
    /// Gets or sets whether items in this zone can be reordered.
    /// When <c>false</c> items cannot be dragged within the zone but cross-zone drops
    /// are still accepted if <see cref="AllowExternalDrop"/> is <c>true</c>.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool AllowReorder { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this zone accepts items dragged from other zones.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool AllowExternalDrop { get; set; } = true;

    /// <summary>
    /// Gets or sets a per-zone predicate that overrides the container-level
    /// <see cref="BbDropContainer{T}.CanDrop"/> check.
    /// Return <c>true</c> to allow a drop, <c>false</c> to reject it.
    /// </summary>
    [Parameter]
    public Func<T, bool>? CanDrop { get; set; }

    /// <summary>
    /// Gets or sets a predicate that marks individual items as non-draggable.
    /// Disabled items cannot be picked up but receive drops normally.
    /// </summary>
    [Parameter]
    public Func<T, bool>? ItemDisabled { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after an item is dropped.
    /// For standalone zones this fires for every drop (same-zone or cross-zone).
    /// For zones inside a <see cref="BbDropContainer{T}"/>, cross-zone drops also
    /// fire <see cref="BbDropContainer{T}.ItemDropped"/>; you may wire either or both.
    /// </summary>
    [Parameter]
    public EventCallback<DropItemInfo<T>> OnItemDropped { get; set; }

    /// <summary>
    /// Gets or sets any additional content appended after the rendered items.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the accessible label for the sortable region.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the outer zone wrapper element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the inner item-container element.
    /// Use this for grid column count, gap, padding, etc.
    /// </summary>
    [Parameter]
    public string? ContainerClass { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to every item wrapper element.
    /// </summary>
    [Parameter]
    public string? ItemClass { get; set; }

    /// <summary>
    /// Gets or sets whether items can be dragged at all.
    /// When <c>false</c> all items are non-draggable (external drops still accepted).
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Sorting { get; set; } = true;

    /// <summary>
    /// Gets or sets the CSS class name of the drag-handle element inside each item.
    /// When set only elements with this class initiate a drag.
    /// When <c>null</c> (default) the entire item is draggable.
    /// </summary>
    [Parameter]
    public string? HandleClass { get; set; }

    /// <summary>
    /// Gets or sets whether dropping an item on another swaps their positions
    /// instead of inserting before the target.
    /// Defaults to <c>false</c> (insert mode).
    /// </summary>
    [Parameter]
    public bool Swap { get; set; }

    /// <summary>
    /// Gets or sets whether this zone renders in drop-zone sink style
    /// (dashed border with drag-state icons) instead of the sortable list/grid style.
    /// Set to <c>true</c> to replace <see cref="BbDropZone{T}"/> usage.
    /// Defaults to <c>false</c>.
    /// </summary>
    [Parameter]
    public bool DropZone { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the drop-zone root element when
    /// a valid drag is hovering over it (drop-zone style only).
    /// </summary>
    [Parameter]
    public string? DragOverClass { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the drop-zone root element when
    /// a rejected drag (CanDrop returns <c>false</c>) is hovering over it.
    /// </summary>
    [Parameter]
    public string? DragOverNoDropClass { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Container is not null)
        {
            Container.TransactionStarted += OnContextTransactionChanged;
            Container.TransactionEnded   += OnContextTransactionChanged;
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

                await InitSortableAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                            or ObjectDisposedException or InvalidOperationException)
            {
                // JS interop not available (prerendering or circuit disconnect)
            }

            return;
        }

        // Re-initialise when key options change after first render
        if (_jsModule is not null)
        {
            var needsReinit = HandleClass != _initializedHandleClass
                           || ZoneIdentifier != _prevZoneId;
            if (needsReinit)
            {
                try
                {
                    await InitSortableAsync();
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                                or ObjectDisposedException) { }
            }
        }
    }

    private async Task InitSortableAsync()
    {
        if (_jsModule is null) { return; }
        _dotNetRef ??= DotNetObjectReference.Create(this);
        _initializedHandleClass = HandleClass;
        _prevZoneId = ZoneIdentifier;

        var groupName = Group ?? Container?.GroupId;

        try
        {
            await _jsModule.InvokeVoidAsync("initSortable", _containerRef, _dotNetRef, new
            {
                zoneId      = ZoneIdentifier,
                group       = groupName,
                sort        = Sorting && AllowReorder,
                disabled    = false,
                handleClass = HandleClass,
                swap        = Swap,
                clone       = Container?.Clone ?? false,
                allowDrop   = AllowExternalDrop,
            });
            _sortableInitialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                        or ObjectDisposedException) { }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            Container.TransactionStarted -= OnContextTransactionChanged;
            Container.TransactionEnded   -= OnContextTransactionChanged;
        }

        if (_jsModule is not null && _sortableInitialized)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("destroySortable", _containerRef);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                            or ObjectDisposedException) { }
        }

        _dotNetRef?.Dispose();

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                            or ObjectDisposedException) { }
        }

        GC.SuppressFinalize(this);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // SortableJS callbacks (called from JavaScript)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SortableJS <c>onStart</c>. Records the dragged item and notifies the
    /// container so all zones can update their visual state.
    /// </summary>
    [JSInvokable]
    public void OnSortableDragStart(int oldIndex)
    {
        if (oldIndex < 0 || oldIndex >= Items.Count) { return; }
        var item = Items[oldIndex];
        StartLocalTransaction(item, oldIndex);
    }

    /// <summary>
    /// Called by SortableJS <c>onEnd</c> on the SOURCE zone (the zone the drag started in).
    /// Commits or cancels the drag transaction.
    /// </summary>
    [JSInvokable]
    public async Task OnSortableDragEnd(SortableEndInfo info)
    {
        // Cancelled / no-op: item returned to its original position
        if (info.FromZone == info.ToZone && info.OldIndex == info.NewIndex)
        {
            CancelLocalTransaction();
            return;
        }

        if (!HasActiveTransaction()) { return; }

        var draggedItem = GetDraggedItem();
        var sourceZone  = GetSourceZone();
        var sourceIndex = Container?.SourceIndex ?? _localSourceIndex;
        var cloneFlag   = info.IsClone || (Container?.Clone ?? false);

        await CommitLocalTransactionAsync(info.NewIndex, info.ToZone);

        // Also fire zone-level OnItemDropped when inside a container and the handler is wired.
        // Container.CommitTransactionAsync fires Container.ItemDropped; this is the per-zone opt-in.
        if (Container is not null && OnItemDropped.HasDelegate && draggedItem is not null)
        {
            await OnItemDropped.InvokeAsync(
                new DropItemInfo<T>(draggedItem, sourceZone, info.ToZone, info.NewIndex, sourceIndex, cloneFlag));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Zone-level native drag event handlers
    // (only used for drag-over visual feedback counting; SortableJS handles the rest)
    // ─────────────────────────────────────────────────────────────────────────────

    private void HandleZoneDragEnter(DragEventArgs args) => _dragOverCount++;

    private void HandleZoneDragLeave(DragEventArgs args) =>
        _dragOverCount = Math.Max(0, _dragOverCount - 1);

    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Blazor event handler must be instance method.")]
    private void HandleZoneDragOver(DragEventArgs args)
    {
        // preventDefault handled by the :preventDefault directive
    }

    /// <summary>
    /// Handles native HTML5 drop events.
    /// <list type="bullet">
    /// <item><see cref="DropZone"/> mode — commits the active container transaction (SortableJS
    ///   does not manage DropZone elements; native events are the only path to commit).</item>
    /// <item>Sortable mode — resets the visual drag-over counter only; the actual commit is
    ///   performed by <see cref="OnSortableDragEnd"/> via the SortableJS <c>onEnd</c> callback.</item>
    /// </list>
    /// </summary>
    private async Task HandleZoneDropAsync(DragEventArgs args)
    {
        _dragOverCount = 0;

        if (!DropZone)
        {
            // Sortable mode: SortableJS fires OnSortableDragEnd for the commit.
            return;
        }

        // ── Drop-zone mode: commit via native drop ─────────────────────────
        // Capture all state synchronously BEFORE any await to eliminate the
        // dragend/drop race where HandleItemDragEnd could cancel the transaction.
        var draggedItem = GetDraggedItem();
        var sourceZone  = GetSourceZone();
        var sourceIndex = Container?.SourceIndex ?? _localSourceIndex;
        var cloneFlag   = Container?.Clone ?? false;

        if (!HasActiveTransaction() || !CanDropCurrentItem() || draggedItem is null)
        {
            return;
        }

        // Container.CommitTransactionAsync clears _draggedItem synchronously before
        // its first await, making any concurrent CancelTransaction a harmless no-op.
        await CommitLocalTransactionAsync(-1, ZoneIdentifier);

        // Also fire zone-level OnItemDropped when the caller has wired it (opt-in per-zone callback).
        // Container.ItemDropped is already fired inside CommitLocalTransactionAsync.
        if (Container is not null && OnItemDropped.HasDelegate)
        {
            await OnItemDropped.InvokeAsync(
                new DropItemInfo<T>(draggedItem, sourceZone, ZoneIdentifier, -1, sourceIndex, cloneFlag));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Keyboard drag-and-drop (independent of SortableJS, works via focus + keydown)
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task HandleItemKeyDown(KeyboardEventArgs e, T item, int index)
    {
        if (IsItemDisabled(item)) { return; }

        switch (e.Key)
        {
            case " " or "Enter" when IsCurrentDraggedItem(item) && HasActiveTransaction():
            {
                var dropIdx = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = -1;
                await CommitLocalTransactionAsync(dropIdx, ZoneIdentifier);
                break;
            }

            case " " or "Enter" when !HasActiveTransaction():
            {
                _keyboardTargetIndex = index;
                StartLocalTransaction(item, index);
                break;
            }

            case "Escape" when HasActiveTransaction():
            {
                _keyboardTargetIndex = -1;
                CancelLocalTransaction();
                break;
            }

            case "ArrowUp" when IsCurrentDraggedItem(item) && HasActiveTransaction()
                             && AllowReorder && Layout == SortableLayout.List:
            case "ArrowLeft" when IsCurrentDraggedItem(item) && HasActiveTransaction()
                              && AllowReorder && Layout == SortableLayout.Grid:
            {
                var current = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = Math.Max(0, current - 1);
                Announce($"Position {_keyboardTargetIndex + 1} of {Items.Count}. Press Space to drop here.");
                break;
            }

            case "ArrowDown" when IsCurrentDraggedItem(item) && HasActiveTransaction()
                              && AllowReorder && Layout == SortableLayout.List:
            case "ArrowRight" when IsCurrentDraggedItem(item) && HasActiveTransaction()
                               && AllowReorder && Layout == SortableLayout.Grid:
            {
                var current = _keyboardTargetIndex >= 0 ? _keyboardTargetIndex : index;
                _keyboardTargetIndex = Math.Min(Items.Count, current + 1);
                Announce($"Position {_keyboardTargetIndex + 1} of {Items.Count}. Press Space to drop here.");
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Transaction helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private void StartLocalTransaction(T item, int index)
    {
        if (Container is not null)
        {
            Container.StartTransaction(item, ZoneIdentifier, index);
            return;
        }

        _localDraggedItem  = item;
        _localSourceZone   = ZoneIdentifier;
        _localSourceIndex  = index;
        Announce($"Grabbed item at position {index + 1} of {Items.Count}. "
            + "Use arrow keys to move. Press Space or Enter to drop, Escape to cancel.");
    }

    private async Task CommitLocalTransactionAsync(int targetIndex, string targetZone)
    {
        if (Container is not null)
        {
            await Container.CommitTransactionAsync(targetZone, targetIndex);
            return;
        }

        if (_localDraggedItem is null) { return; }

        var item        = _localDraggedItem;
        var sourceZone  = _localSourceZone;
        var sourceIndex = _localSourceIndex;
#pragma warning disable CS8601
        _localDraggedItem = default;
#pragma warning restore CS8601
        _localSourceZone = string.Empty;
        Announce("Dropped.");
        await OnItemDropped.InvokeAsync(
            new DropItemInfo<T>(item, sourceZone, targetZone, targetIndex, sourceIndex));
    }

    private void CancelLocalTransaction()
    {
        if (Container is not null)
        {
            Container.CancelTransaction();
            return;
        }

        if (_localDraggedItem is null) { return; }
#pragma warning disable CS8601
        _localDraggedItem = default;
#pragma warning restore CS8601
        _localSourceZone = string.Empty;
        Announce("Drag cancelled.");
    }

    private void Announce(string message)
    {
        _announcement = message;
        StateHasChanged();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // State helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private BbDropContainer<T>? GetEffectiveContext() => Container;

    private bool IsInDragMode =>
        (Container?.HasTransaction ?? false) || _localDraggedItem is not null;

    private bool IsDragOver => _dragOverCount > 0;

    [return: MaybeNull]
    private T GetDraggedItem() =>
        Container is not null ? Container.DraggedItem : _localDraggedItem;

    private string GetSourceZone() =>
        Container?.SourceZone ?? _localSourceZone;

    private bool IsCurrentDraggedItem(T item)
    {
        var dragged = GetDraggedItem();
        return dragged is not null && dragged.Equals(item);
    }

    private bool HasActiveTransaction() =>
        Container?.HasTransaction ?? _localDraggedItem is not null;

    private bool CanDropCurrentItem()
    {
        var item = GetDraggedItem();
        if (item is null) { return false; }

        var sourceZone = GetSourceZone();

        if (!AllowReorder && sourceZone == ZoneIdentifier) { return false; }
        if (!AllowExternalDrop && sourceZone != ZoneIdentifier) { return false; }

        if (CanDrop is not null) { return CanDrop(item); }

        return Container?.CanDropItem(ZoneIdentifier) ?? true;
    }

    private bool IsItemDraggable(T item) => Sorting && !IsItemDisabled(item);

    private bool IsItemDisabled(T item)
    {
        if (ItemDisabled?.Invoke(item) == true) { return true; }
        return Container?.IsItemDisabled(item) ?? false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CSS helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private string ZoneClass => ClassNames.cn("relative", Class);

    private string DropZoneRootClass => ClassNames.cn(
        "relative flex items-center justify-center",
        "min-h-[100px] rounded-lg border-2 border-dashed",
        "transition-all duration-200",
        IsDragOver && CanDropCurrentItem()
            ? ClassNames.cn("border-primary bg-primary/5", DragOverClass)
            : IsDragOver
                ? ClassNames.cn("border-destructive bg-destructive/5", DragOverNoDropClass)
                : Container?.HasTransaction == true && CanDropCurrentItem()
                    ? "border-primary/40 bg-primary/5"
                    : "border-muted-foreground/30 bg-muted/10",
        Class
    );

    private string ContainerCssClass => ClassNames.cn(
        Layout == SortableLayout.List ? "flex flex-col" : "grid",
        "gap-2",
        IsInDragMode && CanDropCurrentItem() && Items.Count == 0 ? "min-h-[3rem]" : null,
        ContainerClass
    );

    private string GetItemWrapperClass(T item)
    {
        var isDragging = IsCurrentDraggedItem(item);
        var isDisabled = IsItemDisabled(item);

        return ClassNames.cn(
            "outline-none transition-all duration-200",
            IsItemDraggable(item) ? "cursor-grab active:cursor-grabbing" : null,
            isDisabled ? "opacity-50 cursor-not-allowed" : null,
            isDragging && Container?.Clone != true ? "opacity-40 scale-[0.98]" : null,
            _keyboardTargetIndex >= 0 && IsCurrentDraggedItem(item) ? "ring-2 ring-primary ring-offset-2 rounded-sm" : null,
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded-sm",
            ItemClass
        );
    }

    private string GetAriaRole() =>
        Layout == SortableLayout.Grid ? "grid" : "list";
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs used with [JSInvokable] callbacks
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data transferred from JavaScript's SortableJS <c>onEnd</c> event to
/// <see cref="BbSortable{T}.OnSortableDragEnd"/>.
/// </summary>
public sealed class SortableEndInfo
{
    /// <summary>Original index of the dragged item in its source zone.</summary>
    public int OldIndex { get; set; }

    /// <summary>Target index where the item was dropped.</summary>
    public int NewIndex { get; set; }

    /// <summary>Zone identifier of the source container.</summary>
    public string FromZone { get; set; } = string.Empty;

    /// <summary>Zone identifier of the target container.</summary>
    public string ToZone { get; set; } = string.Empty;

    /// <summary>Whether the item was cloned (pull:'clone' mode).</summary>
    public bool IsClone { get; set; }
}
