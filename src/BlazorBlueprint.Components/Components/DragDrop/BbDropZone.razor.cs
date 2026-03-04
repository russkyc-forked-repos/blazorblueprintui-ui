using Microsoft.AspNetCore.Components;

namespace BlazorBlueprint.Components;

/// <summary>
/// A sink-only drop target that accepts items dragged from <see cref="BbSortable{T}"/> zones.
/// </summary>
/// <remarks>
/// This component is a thin alias for <see cref="BbSortable{T}"/> with
/// <see cref="BbSortable{T}.DropZone"/> set to <c>true</c>.
/// New code should use <c>&lt;BbSortable DropZone="true" /&gt;</c> directly.
/// </remarks>
/// <typeparam name="T">The type of item that can be dropped. Must be non-null.</typeparam>
public partial class BbDropZone<T> : ComponentBase where T : notnull
{
    /// <summary>Gets or sets the unique identifier for this drop zone.</summary>
    [Parameter, EditorRequired]
    public string ZoneIdentifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the custom content rendered inside the drop zone.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the item preview template shown while a valid item is hovering.</summary>
    [Parameter]
    public RenderFragment<T>? DragItemTemplate { get; set; }

    /// <summary>Gets or sets the template shown in the idle (no drag) state.</summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Gets or sets a per-zone CanDrop override.</summary>
    [Parameter]
    public Func<T, bool>? CanDrop { get; set; }

    /// <summary>Gets or sets the callback invoked when an item is successfully dropped.</summary>
    [Parameter]
    public EventCallback<DropItemInfo<T>> OnItemDropped { get; set; }

    /// <summary>Gets or sets the accessible label. Defaults to "Drop zone {ZoneIdentifier}".</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Gets or sets whether this zone accepts any dropped items. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool AllowDrop { get; set; } = true;

    /// <summary>Gets or sets the CSS class of the drag-handle element inside ChildContent items.</summary>
    [Parameter]
    public string? HandleClass { get; set; }

    /// <summary>Gets or sets additional CSS classes applied when a valid drag is hovering.</summary>
    [Parameter]
    public string? DragOverClass { get; set; }

    /// <summary>Gets or sets additional CSS classes applied when a rejected drag is hovering.</summary>
    [Parameter]
    public string? DragOverNoDropClass { get; set; }

    /// <summary>Gets or sets additional CSS classes for the root element.</summary>
    [Parameter]
    public string? Class { get; set; }
}
