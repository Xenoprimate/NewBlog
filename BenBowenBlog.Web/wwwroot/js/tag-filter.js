// Filters the /tags grid as you type. Without JavaScript the search form still works (it submits ?q= to the server).
// Listens on the document so it keeps working after Blazor's enhanced navigation replaces the page content.
document.addEventListener('input', event => {
    if (!event.target.matches('#tag-search')) {
        return;
    }

    const query = event.target.value.trim().toLowerCase();
    let visible = 0;

    for (const item of document.querySelectorAll('[data-tag]')) {
        const match = item.dataset.tag.includes(query);
        item.hidden = !match;
        visible += match ? 1 : 0;
    }

    const empty = document.getElementById('tag-search-empty');
    if (empty) {
        empty.hidden = visible > 0;
    }
});
