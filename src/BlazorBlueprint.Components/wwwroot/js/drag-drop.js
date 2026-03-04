/**
 * Drag-and-drop helpers for BbSortable — powered by SortableJS.
 *
 * API:
 *   initSortable(containerEl, dotNetRef, options)  — create/update a Sortable instance
 *   destroySortable(containerEl)                   — destroy a Sortable instance
 *
 * options:
 *   zoneId       string         data-zone-id attribute placed on the container
 *   group        string | null  shared group name enabling cross-zone drag
 *   sort         boolean        allow reordering (default true)
 *   disabled     boolean        disable all drag (default false)
 *   handleClass  string | null  CSS class of the drag-handle element
 *   swap         boolean        swap mode (default false)
 *   clone        boolean        clone on cross-zone drop, pull:'clone' (default false)
 *   allowDrop    boolean        accept items from other zones (default true)
 *
 * Each sortable item must have data-sortable-item="true" so SortableJS only manages
 * real items and ignores ChildContent / EmptyTemplate wrappers (data-sortable-ignore).
 */
import Sortable from './_content/BlazorBlueprint.Components/js/sortable.esm.js';

/** @type {Map<Element, Sortable>} */
const instances = new Map();

/**
 * Initialises (or re-initialises) a SortableJS instance on the container element.
 * @param {Element}  containerEl
 * @param {object}   dotNetRef    Blazor DotNetObjectReference for the BbSortable component.
 * @param {object}   options
 */
export function initSortable(containerEl, dotNetRef, options) {
    destroySortable(containerEl);
    if (!containerEl || !dotNetRef) { return; }

    if (options.zoneId) {
        containerEl.dataset.zoneId = options.zoneId;
    }

    const groupName = options.group || null;
    const allowDrop = options.allowDrop !== false;
    const clone     = options.clone === true;

    const sortable = Sortable.create(containerEl, {
        // Only treat elements marked as sortable items; ignore ChildContent wrappers
        draggable: '[data-sortable-item]',
        filter:    '[data-disabled="true"], [data-sortable-ignore]',
        preventOnFilter: false,

        group: groupName ? {
            name: groupName,
            pull: clone ? 'clone' : true,
            put:  allowDrop,
        } : undefined,

        sort:      options.sort !== false,
        disabled:  options.disabled === true,
        handle:    options.handleClass ? '.' + options.handleClass : undefined,
        animation: 150,

        ghostClass:  'bb-sortable-ghost',
        chosenClass: 'bb-sortable-chosen',
        dragClass:   'bb-sortable-drag',
        swap:        options.swap === true,
        swapClass:   'bb-sortable-swap',

        onStart(evt) {
            dotNetRef.invokeMethodAsync('OnSortableDragStart', evt.oldDraggableIndex ?? evt.oldIndex ?? 0)
                     .catch(() => {});
        },

        onEnd(evt) {
            const fromZone = evt.from.dataset.zoneId ?? '';
            const toZone   = evt.to.dataset.zoneId   ?? '';
            const isClone  = evt.pullMode === 'clone';

            dotNetRef.invokeMethodAsync('OnSortableDragEnd', {
                oldIndex: evt.oldDraggableIndex ?? evt.oldIndex  ?? 0,
                newIndex: evt.newDraggableIndex ?? evt.newIndex  ?? 0,
                fromZone,
                toZone,
                isClone,
            }).catch(() => {});
        },
    });

    instances.set(containerEl, sortable);
}

/**
 * Destroys the SortableJS instance on the container. Safe to call multiple times.
 * @param {Element} containerEl
 */
export function destroySortable(containerEl) {
    if (!containerEl) { return; }
    const inst = instances.get(containerEl);
    if (inst) {
        inst.destroy();
        instances.delete(containerEl);
    }
}
