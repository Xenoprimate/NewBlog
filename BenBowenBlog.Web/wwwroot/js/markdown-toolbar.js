// Markdown toolbar for the post editor (EditPost.razor), imported as a module only on that page.
// Wraps or prefixes the current selection in the body textarea, then raises input/change so Blazor's binding updates.

const wrappers = {
    bold: ['**', '**', 'bold text'],
    italic: ['*', '*', 'italic text'],
    code: ['`', '`', 'code'],
    link: ['[', '](https://)', 'link text']
};

// Images go on their own line with a "Caption" placeholder title, since an image alone in its paragraph with a title
// renders as a captioned figure (see FigureExtension.cs). The alt text is selected afterwards so it can be typed over.
export function insertImage(container, alt, url) {
    const textarea = container.querySelector('textarea');
    if (!textarea) {
        return;
    }

    insertBlock(textarea, '![', `](${url} "Caption")`, alt);
}

const linePrefixes = {
    heading: () => '## ',
    quote: () => '> ',
    bullets: () => '- ',
    numbers: index => `${index + 1}. `
};

const blocks = {
    image: ['![', '](/media/ "Caption")', 'alt text'],
    // An admonition: Markdig's custom containers render this as <div class="note"> (styled in app.css).
    note: [':::note\n', '\n:::', 'Important note.'],
    codeblock: ['```csharp\n', '\n```', 'code'],
    table: ['| Column | Column |\n|--------|--------|\n| ', ' | Cell   |', 'Cell  '],
    rule: ['---', '', '']
};

export function apply(container, action) {
    const textarea = container.querySelector('textarea');
    if (!textarea) {
        return;
    }

    const {selectionStart: start, selectionEnd: end, value} = textarea;
    const selected = value.slice(start, end);
    let from = start, to = end, text, selectFrom, selectTo;

    if (wrappers[action]) {
        const [before, after, placeholder] = wrappers[action];
        const inner = selected || placeholder;
        text = before + inner + after;
        selectFrom = start + before.length;
        selectTo = selectFrom + inner.length;
    } else if (linePrefixes[action]) {
        // Prefix every line the selection touches, starting from the beginning of the first one.
        from = value.lastIndexOf('\n', start - 1) + 1;
        text = value.slice(from, end).split('\n').map((line, i) => linePrefixes[action](i) + line).join('\n');
        selectFrom = from;
        selectTo = from + text.length;
    } else if (blocks[action]) {
        const [before, after, placeholder] = blocks[action];
        insertBlock(textarea, before, after, placeholder);
        return;
    } else {
        return;
    }

    replace(textarea, from, to, text, selectFrom, selectTo);
}

// Blocks go on their own lines, with a blank line before them; the selection (or placeholder) sits between before and
// after, and is selected afterwards.
function insertBlock(textarea, before, after, placeholder) {
    const {selectionStart: start, selectionEnd: end, value} = textarea;
    const inner = value.slice(start, end) || placeholder;
    const lead = start === 0 ? '' : value.slice(Math.max(0, start - 2), start) === '\n\n' ? '' : value[start - 1] === '\n' ? '\n' : '\n\n';
    const selectFrom = start + lead.length + before.length;

    replace(textarea, start, end, lead + before + inner + after + '\n', selectFrom, selectFrom + inner.length);
}

function replace(textarea, from, to, text, selectFrom, selectTo) {
    textarea.focus();
    textarea.setSelectionRange(from, to);

    // execCommand keeps the browser's undo history (Ctrl+Z); fall back to setRangeText where it's unsupported.
    if (!document.execCommand('insertText', false, text)) {
        textarea.setRangeText(text, from, to, 'end');
    }

    textarea.setSelectionRange(selectFrom, selectTo);
    textarea.dispatchEvent(new Event('input', {bubbles: true}));
    textarea.dispatchEvent(new Event('change', {bubbles: true}));
}
