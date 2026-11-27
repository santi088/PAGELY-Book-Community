export function renderToolbar(config) {
    const { searchPlaceholder='Search...', searchValue='', filters=[], sortOptions=[], sortValue='' } = config;
    let html = '<div class="toolbar" style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:12px;">';
    html += `<input type="search" placeholder="${searchPlaceholder}" class="toolbar-search" style="padding:6px;" value="${searchValue}">`;
    for (const f of filters) {
        html += `<select class="toolbar-filter" data-key="${f.key}">`;
        for (const o of f.options) {
            const selected = f.value === o.value ? ' selected' : '';
            html += `<option value="${o.value}"${selected}>${o.label}</option>`;
        }
        html += '</select>';
    }
    if (sortOptions.length) {
        html += '<select class="toolbar-sort">';
        for (const o of sortOptions) {
            const selected = sortValue === o.value ? ' selected' : '';
            html += `<option value="${o.value}"${selected}>${o.label}</option>`;
        }
        html += '</select>';
    }
    html += '<button class="btn btn-sm btn-secondary toolbar-reset">Reset</button>';
    html += '</div>';
    return html;
}

export function renderPagination(page, pageSize, total) {
    const pages = Math.ceil(total / pageSize) || 1;
    return `<div class="pagination" style="display:flex;gap:8px;align-items:center;margin-top:12px;">
        <button class="btn btn-sm btn-secondary pg-prev" ${page<=1?'disabled':''}>Prev</button>
        <span>Page ${page} of ${pages}</span>
        <button class="btn btn-sm btn-secondary pg-next" ${page>=pages?'disabled':''}>Next</button>
    </div>`;
}
