/**
 * Pointer-events based drag-and-drop for BbDragDrop<TItem>.
 * Works with mouse, touch, and stylus via the Pointer Events API.
 * Does NOT use the HTML5 Drag and Drop API.
 */

/** @type {Map<string, ListState>} */
const lists = new Map();

/** @type {Map<string, Set<string>>} group name -> Set of listIds */
const groupMap = new Map();

/** @type {ActiveDrag|null} */
let activeDrag = null;

/**
 * Register a drag-drop list.
 * @param {HTMLElement} element
 * @param {object} dotNetRef
 * @param {string} listId
 * @param {string|null} group
 * @param {string|null} handle
 * @param {boolean} sort
 * @param {boolean} put
 * @param {boolean} clone
 */
export function initList(element, dotNetRef, listId, group, handle, sort, put, clone) {
    disposeList(listId);

    const state = {
        element,
        dotNetRef,
        listId,
        group:  group  || null,
        handle: handle || null,
        sort:   !!sort,
        put:    !!put,
        clone:  !!clone,
        _pointerDownHandler: null,
    };

    const handler = (e) => onPointerDown(state, e);
    state._pointerDownHandler = handler;
    element.addEventListener('pointerdown', handler);
    lists.set(listId, state);

    if (state.group) {
        if (!groupMap.has(state.group)) { groupMap.set(state.group, new Set()); }
        groupMap.get(state.group).add(listId);
    }
}

/**
 * Update mutable options when Blazor parameters change.
 */
export function updateList(listId, group, handle, sort, put, clone) {
    const state = lists.get(listId);
    if (!state) { return; }

    const newGroup = group || null;
    if (state.group !== newGroup) {
        if (state.group) { groupMap.get(state.group)?.delete(listId); }
        if (newGroup) {
            if (!groupMap.has(newGroup)) { groupMap.set(newGroup, new Set()); }
            groupMap.get(newGroup).add(listId);
        }
        state.group = newGroup;
    }

    state.handle = handle || null;
    state.sort   = !!sort;
    state.put    = !!put;
    state.clone  = !!clone;
}

/**
 * Unregister a drag-drop list.
 */
export function disposeList(listId) {
    const state = lists.get(listId);
    if (!state) { return; }

    if (state._pointerDownHandler) {
        state.element.removeEventListener('pointerdown', state._pointerDownHandler);
    }
    if (state.group) { groupMap.get(state.group)?.delete(listId); }
    lists.delete(listId);
}

// ── Pointer event handlers ───────────────────────────────────────────────────

function onPointerDown(state, e) {
    if (!e.isPrimary) { return; }
    if (e.pointerType === 'mouse' && e.button !== 0) { return; }
    if (activeDrag) { return; }

    // Find the item wrapper that was clicked.
    const itemEl = e.target.closest('[data-drag-index]');
    if (!itemEl) { return; }

    // Ensure this item belongs to THIS list (not a nested one).
    const ownerList = itemEl.closest('[data-drag-list]');
    if (!ownerList || ownerList !== state.element) { return; }

    // Reject disabled items.
    if (itemEl.getAttribute('data-drag-disabled') === 'true') { return; }

    // Handle constraint: pointer must originate from the handle element.
    if (state.handle && !e.target.closest('.' + state.handle)) { return; }

    const index = parseInt(itemEl.getAttribute('data-drag-index'), 10);
    if (isNaN(index)) { return; }

    e.preventDefault();

    const rect = itemEl.getBoundingClientRect();

    // Ghost: visual clone that follows the pointer.
    const ghost = itemEl.cloneNode(true);
    ghost.style.cssText = [
        'position:fixed',
        `left:${rect.left}px`,
        `top:${rect.top}px`,
        `width:${rect.width}px`,
        `height:${rect.height}px`,
        'margin:0',
        'pointer-events:none',
        'z-index:10000',
        'opacity:0.85',
        'transform:scale(1.02) rotate(0.5deg)',
        'box-shadow:0 8px 24px rgba(0,0,0,.18)',
        'transition:none',
    ].join(';');
    ghost.removeAttribute('data-drag-index');
    ghost.removeAttribute('data-drag-disabled');
    document.body.appendChild(ghost);

    // Placeholder: keeps the space the dragged item occupied.
    const placeholder = createPlaceholder(rect.width, rect.height);
    state.element.insertBefore(placeholder, itemEl);

    // Dim the original so the user knows it's "in flight".
    itemEl.style.opacity = '0.15';
    itemEl.style.pointerEvents = 'none';

    activeDrag = {
        sourceState:         state,
        sourceIndex:         index,
        itemEl,
        ghost,
        placeholder,
        offsetX:             e.clientX - rect.left,
        offsetY:             e.clientY - rect.top,
        pointerId:           e.pointerId,
        currentListId:       state.listId,
        currentDropIndex:    index,
        prevNotifiedListId:  null,
    };

    document.body.style.userSelect = 'none';
    document.body.style.cursor = 'grabbing';

    document.addEventListener('pointermove',   onPointerMove);
    document.addEventListener('pointerup',     onPointerUp);
    document.addEventListener('pointercancel', onPointerCancel);

    state.dotNetRef.invokeMethodAsync('JsDragStart', index).catch(noop);
}

function onPointerMove(e) {
    if (!activeDrag || e.pointerId !== activeDrag.pointerId) { return; }

    const { ghost, placeholder, offsetX, offsetY, sourceState } = activeDrag;

    // Move ghost.
    ghost.style.left = (e.clientX - offsetX) + 'px';
    ghost.style.top  = (e.clientY - offsetY) + 'px';

    // Hide ghost briefly so elementFromPoint sees what's beneath it.
    ghost.style.display = 'none';
    const elUnder = document.elementFromPoint(e.clientX, e.clientY);
    ghost.style.display = '';

    if (!elUnder) { return; }

    const targetListEl = elUnder.closest('[data-drag-list]');
    if (!targetListEl) {
        clearCrossListNotification();
        return;
    }

    const targetListId = targetListEl.getAttribute('data-drag-list');
    const targetState  = lists.get(targetListId);
    if (!targetState) { return; }

    // Cross-list: validate group and put.
    if (targetListId !== sourceState.listId) {
        if (!sourceState.group || sourceState.group !== targetState.group) { return; }
        if (!targetState.put) { return; }
    } else {
        if (!sourceState.sort) { return; }
    }

    const items     = Array.from(targetListEl.querySelectorAll('[data-drag-index]'));
    const dropIndex = calcDropIndex(items, e.clientX, e.clientY);

    if (targetListId !== activeDrag.currentListId || dropIndex !== activeDrag.currentDropIndex) {
        activeDrag.currentListId    = targetListId;
        activeDrag.currentDropIndex = dropIndex;
        movePlaceholder(placeholder, targetListEl, items, dropIndex, activeDrag.itemEl);

        // Notify Blazor of cross-list enter/leave for empty-list hints.
        if (targetListId !== sourceState.listId) {
            if (activeDrag.prevNotifiedListId !== targetListId) {
                clearCrossListNotification();
                activeDrag.prevNotifiedListId = targetListId;
                targetState.dotNetRef.invokeMethodAsync('JsCrossListEnter').catch(noop);
            }
        } else {
            clearCrossListNotification();
        }
    }
}

function onPointerUp(e) {
    if (!activeDrag || e.pointerId !== activeDrag.pointerId) { return; }
    commitDrop();
}

function onPointerCancel(e) {
    if (!activeDrag || e.pointerId !== activeDrag.pointerId) { return; }
    cancelDrop();
}

// ── Drop logic ───────────────────────────────────────────────────────────────

function commitDrop() {
    const drag = activeDrag;
    activeDrag = null;

    cleanup(drag);

    const { sourceState, sourceIndex, currentListId, currentDropIndex } = drag;
    const targetState = lists.get(currentListId);

    if (!targetState) {
        sourceState.dotNetRef.invokeMethodAsync('JsDragCancel').catch(noop);
        return;
    }

    if (currentListId === sourceState.listId) {
        // Same-list reorder.
        if (!sourceState.sort) {
            sourceState.dotNetRef.invokeMethodAsync('JsDragCancel').catch(noop);
            return;
        }
        sourceState.dotNetRef.invokeMethodAsync('JsDrop', sourceIndex, currentDropIndex).catch(noop);
    } else {
        // Cross-list: call target first so DragDropState has the item when source fires OnRemove.
        targetState.dotNetRef.invokeMethodAsync('JsReceiveDrop', sourceIndex, currentDropIndex).catch(noop);
        sourceState.dotNetRef.invokeMethodAsync('JsDragEndCrossListSource', sourceIndex, currentDropIndex).catch(noop);
    }
}

function cancelDrop() {
    const drag = activeDrag;
    activeDrag = null;
    cleanup(drag);
    drag.sourceState.dotNetRef.invokeMethodAsync('JsDragCancel').catch(noop);
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function cleanup(drag) {
    document.removeEventListener('pointermove',   onPointerMove);
    document.removeEventListener('pointerup',     onPointerUp);
    document.removeEventListener('pointercancel', onPointerCancel);

    document.body.style.userSelect = '';
    document.body.style.cursor     = '';

    drag.ghost.parentNode?.removeChild(drag.ghost);
    drag.placeholder.parentNode?.removeChild(drag.placeholder);

    drag.itemEl.style.opacity       = '';
    drag.itemEl.style.pointerEvents = '';

    // Fire leave on any still-notified cross-list zone.
    if (drag.prevNotifiedListId && drag.prevNotifiedListId !== drag.sourceState.listId) {
        lists.get(drag.prevNotifiedListId)
             ?.dotNetRef.invokeMethodAsync('JsCrossListLeave').catch(noop);
    }
}

function clearCrossListNotification() {
    if (!activeDrag) { return; }
    if (activeDrag.prevNotifiedListId && activeDrag.prevNotifiedListId !== activeDrag.sourceState.listId) {
        lists.get(activeDrag.prevNotifiedListId)
             ?.dotNetRef.invokeMethodAsync('JsCrossListLeave').catch(noop);
    }
    activeDrag.prevNotifiedListId = null;
}

/**
 * Calculate the drop index from pointer position.
 * Supports both vertical (flex-col) and horizontal/grid layouts.
 */
function calcDropIndex(items, clientX, clientY) {
    for (let i = 0; i < items.length; i++) {
        const r    = items[i].getBoundingClientRect();
        const next = items[i + 1]?.getBoundingClientRect();

        // Detect if this item shares a row with the next one (grid / flex-row).
        const sameRowAsNext = next && Math.abs(next.top - r.top) < r.height * 0.5;

        if (sameRowAsNext) {
            // Pointer above this row → insert before this item.
            if (clientY < r.top) { return i; }
            // Pointer within this row → use X midpoint.
            if (clientY <= r.bottom && clientX < r.left + r.width / 2) { return i; }
        } else {
            // Vertical layout or last item in a grid row → use Y midpoint.
            if (clientY < r.top + r.height / 2) { return i; }
        }
    }
    return items.length;
}

/**
 * Move placeholder DOM node to the computed drop position.
 */
function movePlaceholder(placeholder, targetListEl, items, dropIndex, originalItem) {
    if (items.length === 0 || dropIndex >= items.length) {
        targetListEl.appendChild(placeholder);
    } else {
        const refItem = items[dropIndex];
        targetListEl.insertBefore(
            placeholder,
            refItem !== originalItem ? refItem : (items[dropIndex + 1] || null)
        );
    }
}

function createPlaceholder(width, height) {
    const el = document.createElement('div');
    el.className = 'bb-drag-placeholder';
    el.setAttribute('aria-hidden', 'true');
    el.style.height    = height + 'px';
    el.style.width     = width  + 'px';
    el.style.boxSizing = 'border-box';
    return el;
}

function noop() {}
