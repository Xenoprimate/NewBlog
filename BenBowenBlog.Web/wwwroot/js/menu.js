// Closes the mobile <details> menu on a click outside it, on Escape, or after choosing a link. Listeners sit on the
// document so they keep working after Blazor's enhanced navigation updates the page.
document.addEventListener('click', event => {
    for (const menu of document.querySelectorAll('details.mobile-menu[open]')) {
        if (!menu.contains(event.target) || event.target.closest('a')) {
            menu.removeAttribute('open');
        }
    }
});

document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') {
        return;
    }

    for (const menu of document.querySelectorAll('details.mobile-menu[open]')) {
        menu.removeAttribute('open');
        menu.querySelector('summary')?.focus();
    }
});
