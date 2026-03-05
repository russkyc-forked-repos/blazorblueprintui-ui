using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBlueprint.Components;

/// <summary>
/// A drag-and-drop sortable list powered by SortableJS, modelled after the
/// BlazorSortable pattern.  A single component handles both in-list reordering
/// and cross-list transfers — no wrapper container required.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Basic reorder</strong> — bind <see cref="Items"/> and handle
/// <see cref="OnUpdate"/> to reorder your list when the user drags an item.
/// </para>
/// <para>
/// <strong>Cross-list (kanban / clone)</strong> — give multiple <c>BbDragDrop</c>
/// components the same <see cref="Group"/> name.  Handle <see cref="OnRemove"/> on
/// the source list and <see cref="OnAdd"/> on the target list.  Set
/// <see cref="Pull"/> to <c>"clone"</c> and <see cref="Put"/> to <c>true</c> to
/// clone items instead of moving them.
/// </para>
/// <para>
/// <strong>Handles</strong> — set <see cref="Handle"/> to a CSS class name inside
/// your <see cref="ItemTemplate"/> to restrict dragging to that element.
/// </para>
/// <para>
/// <strong>Filtering</strong> — set <see cref="Filter"/> to a CSS class name to mark
/// items that cannot be dragged or sorted.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The type of item in the list. Must be non-null.</typeparam>
public partial class BbDragDrop<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private ElementReference _element;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<BbDragDrop<TItem>>? _dotNetRef;
    private bool _initialized;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    // ─────────────────────────────────────────────────────────────────────────────
    // Parameters
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the HTML <c>id</c> attribute of the list element.
    /// When not set a random GUID is generated so multiple lists on the same page
    /// can share the same group without id conflicts.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the SortableJS group name.
    /// Lists with the same group name can exchange items via drag-and-drop.
    /// Leave <c>null</c> (default) to disable cross-list drag.
    /// </summary>
    [Parameter]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the SortableJS <c>pull</c> option.
    /// <list type="bullet">
    ///   <item><c>null</c> (default) — items are moved to the target list.</item>
    ///   <item><c>"clone"</c> — items are cloned; the original stays in place.</item>
    ///   <item><c>"false"</c> — items cannot be pulled out of this list.</item>
    /// </list>
    /// Only meaningful when <see cref="Group"/> is set.
    /// </summary>
    [Parameter]
    public string? Pull { get; set; }

    /// <summary>
    /// Gets or sets whether items from other lists in the same group can be dropped
    /// into this list.  Defaults to <c>true</c>.
    /// Only meaningful when <see cref="Group"/> is set.
    /// </summary>
    [Parameter]
    public bool Put { get; set; } = true;

    /// <summary>
    /// Gets or sets whether items within this list can be reordered by dragging.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Sort { get; set; } = true;

    /// <summary>
    /// Gets or sets a CSS class name inside each <see cref="ItemTemplate"/> that
    /// acts as the drag handle.  When set only elements with this class initiate a
    /// drag; clicking elsewhere does nothing.  Defaults to <c>null</c> (entire item
    /// is draggable).
    /// </summary>
    [Parameter]
    public string? Handle { get; set; }

    /// <summary>
    /// Gets or sets a CSS class name that marks items as non-draggable.
    /// SortableJS will not start a drag on elements that carry this class.
    /// </summary>
    [Parameter]
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the drag animation duration in milliseconds.  Defaults to <c>150</c>.
    /// Set to <c>0</c> to disable animation.
    /// </summary>
    [Parameter]
    public int Animation { get; set; } = 150;

    /// <summary>
    /// Gets or sets whether SortableJS uses its polyfill (CSS transforms) instead of
    /// the native HTML5 Drag-and-Drop API.  Defaults to <c>false</c>.
    /// </summary>
    [Parameter]
    public bool ForceFallback { get; set; }

    /// <summary>
    /// Gets or sets the list of items to render.
    /// The list is not mutated by this component — update it yourself inside
    /// <see cref="OnUpdate"/>, <see cref="OnRemove"/>, and <see cref="OnAdd"/>.
    /// </summary>
    [Parameter]
    public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Gets or sets the template used to render each item.
    /// The template receives the item as its context.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item is reordered within this list.
    /// Receives the <c>oldIndex</c> and <c>newIndex</c> of the moved item.
    /// You must reorder <see cref="Items"/> and call <c>StateHasChanged()</c> (or use
    /// two-way binding) to persist the change.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnUpdate { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item is removed from this list into
    /// another group list.
    /// Receives the <c>oldIndex</c> (position in this list) and <c>newIndex</c>
    /// (position in the target list).
    /// Remove the item at <c>oldIndex</c> from <see cref="Items"/> in this handler.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnRemove { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item from another group list is
    /// dropped into this list.
    /// Receives the <c>oldIndex</c> (position in the source list) and <c>newIndex</c>
    /// (insertion position in this list).
    /// Insert the item from the source list into <see cref="Items"/> in this handler.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnAdd { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the list container element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) { return; }

        try
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorBlueprint.Components/js/drag-drop.js");

            _dotNetRef = DotNetObjectReference.Create(this);

            await _jsModule.InvokeVoidAsync("init", _element, _dotNetRef, new
            {
                group         = Group,
                pull          = Pull,
                put           = Put,
                sort          = Sort,
                handle        = Handle,
                filter        = Filter,
                animation     = Animation,
                forceFallback = ForceFallback,
            });

            _initialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                        or ObjectDisposedException or InvalidOperationException)
        {
            // JS not available during prerendering or after circuit disconnect
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null && _initialized)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("destroy", _element);
            }
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

    // ─────────────────────────────────────────────────────────────────────────────
    // JSInvokable callbacks from SortableJS
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SortableJS <c>onUpdate</c> when an item is moved within this list.
    /// </summary>
    [JSInvokable]
    public async Task JsOnUpdate(int oldIndex, int newIndex)
    {
        await OnUpdate.InvokeAsync((oldIndex, newIndex));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by SortableJS <c>onAdd</c> when an item from another list is dropped here.
    /// </summary>
    [JSInvokable]
    public async Task JsOnAdd(int oldIndex, int newIndex)
    {
        await OnAdd.InvokeAsync((oldIndex, newIndex));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by SortableJS <c>onRemove</c> when an item is dragged out to another list.
    /// </summary>
    [JSInvokable]
    public async Task JsOnRemove(int oldIndex, int newIndex)
    {
        await OnRemove.InvokeAsync((oldIndex, newIndex));
        await InvokeAsync(StateHasChanged);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CSS helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private string CssClass => ClassNames.cn("bb-drag-list", Class);
}
