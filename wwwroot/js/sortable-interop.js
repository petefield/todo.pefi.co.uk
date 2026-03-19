window.initSortable = (element, dotNetRef) => {
    if (!element) return;
    if (element._sortable) element._sortable.destroy();
    element._sortable = new Sortable(element, {
        handle: '.drag-handle',
        animation: 150,
        ghostClass: 'sortable-ghost',
        chosenClass: 'sortable-chosen',
        dragClass: 'sortable-drag',
        onEnd: (evt) => {
            // Read new order from DOM after SortableJS moved the element
            const ids = Array.from(element.querySelectorAll('[data-id]'))
                .map(el => el.getAttribute('data-id'));

            // Revert the DOM change so Blazor's diffing stays consistent
            // SortableJS moved evt.item from oldIndex to newIndex — put it back
            const parent = evt.from;
            if (evt.oldIndex < evt.newIndex) {
                parent.insertBefore(evt.item, parent.children[evt.oldIndex]);
            } else {
                parent.insertBefore(evt.item, parent.children[evt.oldIndex + 1]);
            }

            // Let Blazor re-render from server state
            dotNetRef.invokeMethodAsync('OnReorder', ids);
        }
    });
};

window.destroySortable = (element) => {
    if (element && element._sortable) {
        element._sortable.destroy();
        element._sortable = null;
    }
};
