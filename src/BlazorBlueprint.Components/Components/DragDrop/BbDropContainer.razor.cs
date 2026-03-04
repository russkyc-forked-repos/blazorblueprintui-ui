using System.Diagnostics.CodeAnalysis;
using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;

namespace BlazorBlueprint.Components;

/// <summary>
/// Provides a shared drag-and-drop context that coordinates drag operations between
/// <see cref="BbSortable{T}"/> components.
/// </summary>
/// <remarks>
/// <para>
/// Wrap any number of <see cref="BbSortable{T}"/> components (including ones with
/// <see cref="BbSortable{T}.DropZone"/> set to <c>true</c> for sink-only targets) inside a
/// <see cref="BbDropContainer{T}"/> to enable cross-component drag and drop. A standalone
/// <see cref="BbSortable{T}"/> (without a container) handles reordering within itself
/// without needing a context.
/// </para>
/// <para>
/// The container uses a cascading value so all descendant sortables automatically receive
/// the shared drag state. SortableJS uses the container's <see cref="GroupId"/> as the
/// group name so only zones within the same container can exchange items.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item being dragged and dropped. Must be non-null.</typeparam>
public partial class BbDropContainer<T> : ComponentBase where T : notnull
{
#pragma warning disable CS8601
    [MaybeNull]
    private T _draggedItem = default;
#pragma warning restore CS8601

    private string _sourceZone = string.Empty;
    private int _sourceIndex;
    private string _announcement = string.Empty;

    /// <summary>
    /// Gets a unique group identifier shared by all child <see cref="BbSortable{T}"/> zones.
    /// SortableJS uses this as the <c>group.name</c> so only zones within the same container
    /// can exchange items via cross-zone drag.
    /// </summary>
    internal string GroupId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Fires when a drag transaction starts. Descendant components subscribe to trigger re-renders.</summary>
    internal event EventHandler? TransactionStarted;

    /// <summary>Fires when a drag transaction ends (dropped or cancelled). Descendant components subscribe to trigger re-renders.</summary>
    internal event EventHandler? TransactionEnded;

    /// <summary>
    /// Gets or sets the content within this drag-and-drop context.
    /// Should contain <see cref="BbSortable{T}"/> components.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether the currently dragged item
    /// can be dropped into the zone with the given identifier.
    /// </summary>
    [Parameter]
    public Func<T, string, bool>? CanDrop { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether a given item is disabled from being dragged.
    /// </summary>
    [Parameter]
    public Func<T, bool>? ItemDisabled { get; set; }

    /// <summary>
    /// Gets or sets whether dragging an item to another zone clones it instead of moving it.
    /// When <c>true</c> the source zone keeps the original item and a copy is inserted in the target zone.
    /// The <see cref="DropItemInfo{T}.IsClone"/> property on the callback argument will be <c>true</c>
    /// so your handler can add to the target without removing from the source.
    /// Defaults to <c>false</c>.
    /// </summary>
    [Parameter]
    public bool Clone { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item is successfully dropped into any zone or sortable.
    /// </summary>
    [Parameter]
    public EventCallback<DropItemInfo<T>> ItemDropped { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item drag starts (item is picked up).
    /// </summary>
    [Parameter]
    public EventCallback<T> ItemPicked { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the container element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets whether a drag transaction is currently in progress.</summary>
    internal bool HasTransaction => _draggedItem is not null;

    /// <summary>Gets the item currently being dragged, or <c>null</c> if no drag is active.</summary>
    [MaybeNull]
    internal T DraggedItem => _draggedItem;

    /// <summary>Gets the zone identifier where the current drag started.</summary>
    internal string SourceZone => _sourceZone;

    /// <summary>Gets the index within the source zone where the drag started.</summary>
    internal int SourceIndex => _sourceIndex;

    private string CssClass => ClassNames.cn(Class);

    /// <summary>
    /// Determines whether the currently dragged item can be dropped into the specified zone.
    /// </summary>
    internal bool CanDropItem(string zoneId)
    {
        if (_draggedItem is null)
        {
            return false;
        }

        return CanDrop?.Invoke(_draggedItem, zoneId) ?? true;
    }

    /// <summary>Determines whether the specified item is disabled from being dragged.</summary>
    internal bool IsItemDisabled(T item) => ItemDisabled?.Invoke(item) ?? false;

    /// <summary>
    /// Starts a drag transaction for the specified item originating from a given zone.
    /// </summary>
    internal void StartTransaction(T item, string zoneId, int index, string? itemLabel = null)
    {
        _draggedItem = item;
        _sourceZone = zoneId;
        _sourceIndex = index;
        TransactionStarted?.Invoke(this, EventArgs.Empty);
        _ = ItemPicked.InvokeAsync(item);
        Announce($"Grabbed{(itemLabel is not null ? $" {itemLabel}" : string.Empty)}. "
            + "Use arrow keys to reorder, or drag to another zone. Press Escape to cancel.");
    }

    /// <summary>
    /// Commits the current drag transaction to the specified target zone at the given index.
    /// </summary>
    internal async Task CommitTransactionAsync(string targetZone, int targetIndex, bool isClone = false)
    {
        if (_draggedItem is null)
        {
            return;
        }

        var item = _draggedItem;
        var sourceZone = _sourceZone;
        var sourceIndex = _sourceIndex;
        var cloneFlag = Clone || isClone;
#pragma warning disable CS8601
        _draggedItem = default;
#pragma warning restore CS8601
        _sourceZone = string.Empty;
        TransactionEnded?.Invoke(this, EventArgs.Empty);
        Announce("Dropped.");
        await ItemDropped.InvokeAsync(new DropItemInfo<T>(item, sourceZone, targetZone, targetIndex, sourceIndex, cloneFlag));
    }

    /// <summary>Cancels the current drag transaction without committing.</summary>
    internal void CancelTransaction()
    {
        if (_draggedItem is null)
        {
            return;
        }

#pragma warning disable CS8601
        _draggedItem = default;
#pragma warning restore CS8601
        _sourceZone = string.Empty;
        TransactionEnded?.Invoke(this, EventArgs.Empty);
        Announce("Drag cancelled.");
    }

    private void Announce(string message)
    {
        _announcement = message;
        StateHasChanged();
    }
}
