using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBlueprint.Components;

/// <summary>
/// A drag-and-drop sortable list implemented with the Pointer Events API and Blazor —
/// works on mouse, touch, and stylus without the HTML5 Drag and Drop API.
/// <para>
/// <strong>Basic reorder</strong> — bind <see cref="Items"/> and handle
/// <see cref="OnUpdate"/> to reorder your list.
/// </para>
/// <para>
/// <strong>Cross-list (kanban / move)</strong> — give multiple lists the same
/// <see cref="Group"/> name. Handle <see cref="OnRemove"/> on the source and
/// <see cref="OnAdd"/> on the target.
/// </para>
/// <para>
/// <strong>Clone mode</strong> — set <see cref="Pull"/> to <c>"clone"</c> on the
/// source. <see cref="OnRemove"/> is never fired; <see cref="OnAdd"/> receives a copy.
/// </para>
/// <para>
/// <strong>Handle</strong> — set <see cref="Handle"/> to a CSS class inside your
/// <see cref="ItemTemplate"/>. Only dragging that element initiates a drag.
/// </para>
/// </summary>
/// <typeparam name="TItem">Type of item in the list. Must be non-null.</typeparam>
public partial class BbDragDrop<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private readonly string _listId = Guid.NewGuid().ToString("N");

    private ElementReference              _container;
    private IJSObjectReference?           _jsModule;
    private DotNetObjectReference<BbDragDrop<TItem>>? _dotNetRef;
    private bool _jsInitialized;

    // Visual drag state — set via JS callbacks.
    private bool _dragging;
    private int  _dragIndex    = -1;
    private bool _receivingDrop;

    [Inject] private IJSRuntime    JSRuntime { get; set; } = default!;
    [Inject] private DragDropState DragState { get; set; } = default!;

    // ─────────────────────────────────────────────────────────────────────────
    // Parameters (unchanged public API)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Group name — lists sharing a group can exchange items.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary><c>null</c> = move; <c>"clone"</c> = copy items on cross-list drag.</summary>
    [Parameter] public string? Pull { get; set; }

    /// <summary>Whether this list accepts drops from other lists in the same group.</summary>
    [Parameter] public bool Put { get; set; } = true;

    /// <summary>Whether items within this list can be reordered.</summary>
    [Parameter] public bool Sort { get; set; } = true;

    /// <summary>CSS class of the drag-handle element inside each item template.</summary>
    [Parameter] public string? Handle { get; set; }

    /// <summary>Predicate that returns <c>true</c> for items that cannot be dragged.</summary>
    [Parameter] public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>The list of items to render.</summary>
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>Template used to render each item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Fired on in-list reorder. Receives <c>(OldIndex, NewIndex)</c>.</summary>
    [Parameter] public EventCallback<(int OldIndex, int NewIndex)> OnUpdate { get; set; }

    /// <summary>
    /// Fired when an item leaves this list (not fired in clone mode).
    /// Receives <c>(OldIndex, NewIndex)</c>.
    /// </summary>
    [Parameter] public EventCallback<(int OldIndex, int NewIndex)> OnRemove { get; set; }

    /// <summary>
    /// Fired when an item arrives from another list.
    /// Receives <c>(Item, OldIndex, NewIndex)</c>.
    /// </summary>
    [Parameter] public EventCallback<(TItem Item, int OldIndex, int NewIndex)> OnAdd { get; set; }

    /// <summary>Accessible label for the container (<c>aria-label</c>).</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>CSS layout classes for the container (e.g. <c>"flex flex-col gap-2"</c>).</summary>
    [Parameter] public string? Class { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        DragState.StateChanged += HandleDragStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) { return; }

        try
        {
            _jsModule  = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorBlueprint.Components/js/drag-drop.js");
            _dotNetRef = DotNetObjectReference.Create(this);

            await _jsModule.InvokeVoidAsync(
                "initList",
                _container, _dotNetRef, _listId,
                Group, Handle, Sort, Put, Pull == "clone");

            _jsInitialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                       or ObjectDisposedException or InvalidOperationException)
        {
            // JS unavailable during prerendering or after circuit disconnect.
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_jsInitialized || _jsModule is null) { return; }

        try
        {
            await _jsModule.InvokeVoidAsync(
                "updateList",
                _listId, Group, Handle, Sort, Put, Pull == "clone");
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                       or ObjectDisposedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        DragState.StateChanged -= HandleDragStateChanged;

        if (_jsModule is not null && _jsInitialized)
        {
            try { await _jsModule.InvokeVoidAsync("disposeList", _listId); }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                           or ObjectDisposedException) { }
        }

        _dotNetRef?.Dispose();

        if (_jsModule is not null)
        {
            try { await _jsModule.DisposeAsync(); }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                           or ObjectDisposedException) { }
        }

        GC.SuppressFinalize(this);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JS-invokable callbacks (called by drag-drop.js)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called by JS when a drag starts on this list.</summary>
    [JSInvokable]
    public void JsDragStart(int index)
    {
        if (index < 0 || index >= Items.Count) { return; }

        _dragging   = true;
        _dragIndex  = index;

        if (Group is not null)
        {
            DragState.Begin(Items[index], Group, Pull == "clone", index);
        }

        InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when a same-list drop completes.</summary>
    [JSInvokable]
    public async Task JsDrop(int oldIndex, int newIndex)
    {
        _dragging  = false;
        _dragIndex = -1;

        if (Sort && oldIndex != newIndex && OnUpdate.HasDelegate)
        {
            await OnUpdate.InvokeAsync((oldIndex, newIndex));
        }

        DragState.Clear();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when this list is the DROP TARGET of a cross-list drag.</summary>
    [JSInvokable]
    public async Task JsReceiveDrop(int sourceIndex, int targetIndex)
    {
        _receivingDrop = false;

        if (!Put || !DragState.IsActive) { return; }
        if (DragState.Item is not TItem item) { return; }

        var newIndex = Math.Clamp(targetIndex, 0, Items.Count);

        if (OnAdd.HasDelegate)
        {
            await OnAdd.InvokeAsync((item, sourceIndex, newIndex));
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when this list is the SOURCE of a cross-list drag that completed.</summary>
    [JSInvokable]
    public async Task JsDragEndCrossListSource(int sourceIndex, int targetIndex)
    {
        _dragging  = false;
        _dragIndex = -1;

        if (!DragState.IsClone && OnRemove.HasDelegate)
        {
            await OnRemove.InvokeAsync((sourceIndex, targetIndex));
        }

        DragState.Clear();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when the drag is cancelled (pointer cancel or dropped outside).</summary>
    [JSInvokable]
    public void JsDragCancel()
    {
        _dragging      = false;
        _dragIndex     = -1;
        _receivingDrop = false;
        DragState.Clear();
        InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when a cross-list drag enters this (empty) list.</summary>
    [JSInvokable]
    public void JsCrossListEnter()
    {
        _receivingDrop = true;
        InvokeAsync(StateHasChanged);
    }

    /// <summary>Called by JS when a cross-list drag leaves this list without dropping.</summary>
    [JSInvokable]
    public void JsCrossListLeave()
    {
        _receivingDrop = false;
        InvokeAsync(StateHasChanged);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDragStateChanged() => InvokeAsync(StateHasChanged);

    private bool IsDraggable(TItem item)
    {
        if (!Sort && Group is null) { return false; }
        if (ItemDisabled?.Invoke(item) == true) { return false; }
        return true;
    }

    private bool CanReceiveCrossListDrop()
    {
        if (!Put || !DragState.IsActive) { return false; }
        if (Group is null || DragState.Group != Group) { return false; }
        if (DragState.Item is not TItem) { return false; }
        return true;
    }

    private string GetWrapperClass(TItem item, int index)
    {
        if (_dragging && _dragIndex == index)
        {
            return "opacity-20";
        }

        return string.Empty;
    }

    private string CssClass => ClassNames.cn("bb-drag-list", Class);

    private bool ShouldShowEmptyHint =>
        Items.Count == 0 && (_receivingDrop || (_dragging && Sort) || CanReceiveCrossListDrop());
}
