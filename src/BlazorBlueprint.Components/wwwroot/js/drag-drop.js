/**
 * Drag-and-drop helpers for BbSortable and BbDropZone.
 *
 * FLIP (First, Last, Invert, Play) — smooth reorder animations.
 * initHandles  — restrict drag to a designated handle element by CSS class.
 * startLiveSort / stopLiveSort — SortableJS-style coordinate-based live reordering:
 *   tracks cursor position on the container's dragover event (stable, never fires on
 *   the shifted items themselves) and only notifies Blazor when the insertion index
 *   actually changes.  One rAF per frame limits Blazor re-renders.
 */

/** @type {Map<Element, Map<Element, DOMRect>>} */
const snapshots = new Map();

/**
 * Captures the current bounding rectangles of all animatable children
 * in the container. Call this BEFORE the Blazor state change that reorders items.
 * @param {Element} containerEl
 */
export function capturePositions(containerEl) {
    if (!containerEl) { return; }
    const rects = new Map();
    for (const child of containerEl.children) {
        if (child.dataset.flipIgnore) { continue; }
        rects.set(child, child.getBoundingClientRect());
    }
    snapshots.set(containerEl, rects);
}

/**
 * Plays the FLIP animation — compares stored positions to current positions and
 * animates each child from its old position to its new one.
 * Call this in OnAfterRenderAsync AFTER Blazor has re-rendered.
 * @param {Element} containerEl
 * @param {number}  durationMs
 */
export function playFlip(containerEl, durationMs) {
    if (!containerEl) { return; }
    const rects = snapshots.get(containerEl);
    if (!rects) { return; }
    snapshots.delete(containerEl);

    for (const child of containerEl.children) {
        if (child.dataset.flipIgnore) { continue; }
        if (!rects.has(child)) { continue; }

        const prev = rects.get(child);
        const curr = child.getBoundingClientRect();
        const dx = prev.left - curr.left;
        const dy = prev.top  - curr.top;

        if (Math.abs(dx) < 1 && Math.abs(dy) < 1) { continue; }

        // Place child visually at its old position (instant, no transition)
        child.style.transition = 'none';
        child.style.transform  = `translate(${dx}px, ${dy}px)`;

        // On the next frame, animate back to its natural position
        requestAnimationFrame(() => {
            child.style.transition = `transform ${durationMs}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            child.style.transform  = '';
            child.addEventListener('transitionend', () => {
                child.style.transition = '';
            }, { once: true });
        });
    }
}

/**
 * Begins tracking live-sort position via container-level dragover.
 *
 * Uses a SortableJS-inspired coordinate approach: listens on the stable container
 * element (not the shifting item elements) and hit-tests cursor coordinates against
 * item midpoints.  A requestAnimationFrame guard limits Blazor calls to one per frame
 * and only fires when the computed insertion index actually changes.
 *
 * @param {Element}         containerEl  - The sortable inner container element.
 * @param {DotNetObjectRef} dotNetRef    - Blazor component reference.
 * @param {boolean}         isGrid       - Whether the container uses grid layout.
 */
export function startLiveSort(containerEl, dotNetRef, isGrid) {
    stopLiveSort(containerEl);
    if (!containerEl || !dotNetRef) { return; }

    let lastIndex  = -1;
    let rafPending = false;
    let lastX = 0;
    let lastY = 0;

    const onDragOver = (evt) => {
        lastX = evt.clientX;
        lastY = evt.clientY;
        if (rafPending) { return; }
        rafPending = true;
        requestAnimationFrame(() => {
            rafPending = false;
            const idx = _getInsertionIndex(containerEl, lastX, lastY, isGrid);
            if (idx !== lastIndex) {
                lastIndex = idx;
                dotNetRef.invokeMethodAsync('UpdateLiveIndexAsync', idx).catch(() => {});
            }
        });
    };

    containerEl.addEventListener('dragover', onDragOver);
    containerEl._bbLiveSort = { handler: onDragOver };
}

/**
 * Removes the live-sort dragover listener previously set by startLiveSort.
 * Safe to call even if startLiveSort was never called.
 * @param {Element} containerEl
 */
export function stopLiveSort(containerEl) {
    if (!containerEl || !containerEl._bbLiveSort) { return; }
    containerEl.removeEventListener('dragover', containerEl._bbLiveSort.handler);
    delete containerEl._bbLiveSort;
}

/**
 * Returns the insertion index for the dragged item at (clientX, clientY).
 *
 * Only considers children that carry data-draggable-item and are NOT the currently
 * dragged element (data-dragging="true"), so the result is stable regardless of
 * where the dragged item sits in the rendered list.
 *
 * List layout  — purely vertical midpoint comparison.
 * Grid layout  — row-aware: within a row uses horizontal midpoint; above a row
 *                inserts before the first item in that row.
 *
 * @param {Element} containerEl
 * @param {number}  clientX
 * @param {number}  clientY
 * @param {boolean} isGrid
 * @returns {number}
 */
function _getInsertionIndex(containerEl, clientX, clientY, isGrid) {
    /** @type {Element[]} */
    const items = Array.from(containerEl.children).filter(el =>
        el.dataset.draggableItem && el.dataset.dragging !== 'true' && !el.dataset.flipIgnore
    );

    for (let i = 0; i < items.length; i++) {
        const rect = items[i].getBoundingClientRect();
        if (!isGrid) {
            // List: vertical midpoint
            if (clientY < rect.top + rect.height / 2) { return i; }
        } else {
            // Grid: check if cursor is above this item's row
            if (clientY < rect.top) { return i; }
            // Within this item's row: use horizontal midpoint
            if (clientY <= rect.bottom && clientX < rect.left + rect.width / 2) { return i; }
        }
    }
    return items.length;
}

/**
 * Sets up handle-only dragging for a sortable/dropzone container.
 *
 * When a handleClass is provided, item wrappers start with draggable="false".
 * A mousedown on an element with the given class (or a descendant of one) enables
 * draggable on the nearest [data-draggable-item] ancestor so the native drag can start.
 * On dragend the attribute is reset.
 *
 * @param {Element} containerEl  - The sortable container or dropzone root element.
 * @param {string|null} handleClass - CSS class that marks the drag-handle element.
 *                                    Pass null/undefined to remove any existing listener.
 */
export function initHandles(containerEl, handleClass) {
    if (!containerEl) { return; }

    // Remove any previous listeners attached by us (stored on the element)
    if (containerEl._bbDragMouseDown) {
        containerEl.removeEventListener('mousedown', containerEl._bbDragMouseDown, true);
        containerEl.removeEventListener('dragend',   containerEl._bbDragEnd,       true);
        delete containerEl._bbDragMouseDown;
        delete containerEl._bbDragEnd;
    }

    if (!handleClass) { return; }

    const onMouseDown = (e) => {
        const itemWrapper = e.target.closest('[data-draggable-item]');
        if (!itemWrapper || !containerEl.contains(itemWrapper)) { return; }

        // Enable drag only when the mousedown originated on (or inside) the handle
        const onHandle = !!e.target.closest('.' + handleClass);
        itemWrapper.draggable = onHandle;
    };

    const onDragEnd = (e) => {
        const itemWrapper = e.target.closest('[data-draggable-item]');
        if (itemWrapper && containerEl.contains(itemWrapper)) {
            itemWrapper.draggable = false;
        }
    };

    containerEl.addEventListener('mousedown', onMouseDown, true);
    containerEl.addEventListener('dragend',   onDragEnd,   true);

    // Store references so we can remove them later
    containerEl._bbDragMouseDown = onMouseDown;
    containerEl._bbDragEnd       = onDragEnd;

    // Ensure all existing item wrappers start as non-draggable
    for (const el of containerEl.querySelectorAll('[data-draggable-item]')) {
        el.draggable = false;
    }
}
