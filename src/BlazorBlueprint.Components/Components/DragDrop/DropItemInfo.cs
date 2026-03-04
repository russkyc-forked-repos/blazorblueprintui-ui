namespace BlazorBlueprint.Components;

/// <summary>
/// Contains information about an item that has been dropped into a zone or sortable list.
/// </summary>
/// <typeparam name="T">The type of item being dragged and dropped.</typeparam>
public sealed class DropItemInfo<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="DropItemInfo{T}"/>.
    /// </summary>
    /// <param name="item">The item that was dropped.</param>
    /// <param name="sourceZone">The identifier of the zone the item was dragged from.</param>
    /// <param name="targetZone">The identifier of the zone the item was dropped into.</param>
    /// <param name="targetIndex">The index in the target zone where the item was dropped, or -1 if not applicable.</param>
    /// <param name="sourceIndex">The original index of the item in the source zone, or -1 if not tracked.</param>
    /// <param name="isClone">
    /// <c>true</c> when the item was cloned rather than moved.
    /// When <c>true</c> the source zone retains the original item; only a copy was added to the target.
    /// </param>
    public DropItemInfo(T? item, string sourceZone, string targetZone, int targetIndex, int sourceIndex = -1, bool isClone = false)
    {
        Item = item;
        SourceZone = sourceZone;
        TargetZone = targetZone;
        TargetIndex = targetIndex;
        SourceIndex = sourceIndex;
        IsClone = isClone;
    }

    /// <summary>Gets the item that was dropped.</summary>
    public T? Item { get; }

    /// <summary>Gets the identifier of the zone the item was dragged from.</summary>
    public string SourceZone { get; }

    /// <summary>Gets the identifier of the zone the item was dropped into.</summary>
    public string TargetZone { get; }

    /// <summary>
    /// Gets the index position in the target zone where the item was dropped.
    /// Returns -1 when the drop zone does not track position (e.g. <see cref="BbDropZone{T}"/>).
    /// In <c>Swap</c> mode this is the index of the item that was swapped with the dragged item.
    /// </summary>
    public int TargetIndex { get; }

    /// <summary>
    /// Gets the index position the item occupied in the source zone before the drag began.
    /// Returns -1 when the source index is not tracked (e.g. drops into <see cref="BbDropZone{T}"/>).
    /// Useful in <c>Swap</c> mode to identify both participants of the swap.
    /// </summary>
    public int SourceIndex { get; }

    /// <summary>
    /// Gets a value indicating whether this drop is a clone operation.
    /// When <c>true</c> the original item remains in the source zone and only a copy was transferred
    /// to the target zone. The consumer's drop handler should add to the target but not remove from the source.
    /// </summary>
    public bool IsClone { get; }
}
