window.initSortable = (element, dotNetRef) => {
    if (!element) return;
    if (element._sortable) element._sortable.destroy();
    element._sortable = new Sortable(element, {
        handle: '.drag-handle',
        animation: 150,
        ghostClass: 'sortable-ghost',
        chosenClass: 'sortable-chosen',
        dragClass: 'sortable-drag',
        onEnd: () => {
            const ids = Array.from(element.querySelectorAll('[data-id]'))
                .map(el => el.getAttribute('data-id'));
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
