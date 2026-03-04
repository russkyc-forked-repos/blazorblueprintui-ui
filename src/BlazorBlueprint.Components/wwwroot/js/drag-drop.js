/**
 * Drag-and-drop helpers for BbSortable and BbDropZone.
 *
 * FLIP (First, Last, Invert, Play) — smooth reorder animations.
 * initHandles — restrict drag to a designated handle element by CSS class.
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
