export function debounce(fn, ms) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), ms);
    };
}

export function createListStore(initial) {
    const state = {
        search: initial.search || '',
        filters: { ...(initial.filters || {}) },
        sort: initial.sort || '',
        page: initial.page || 1,
        pageSize: initial.pageSize || 20
    };
    const initialState = {
        search: initial.search || '',
        filters: { ...(initial.filters || {}) },
        sort: initial.sort || '',
        page: initial.page || 1,
        pageSize: initial.pageSize || 20
    };
    const subscribers = [];

    function notify() {
        subscribers.forEach(cb => cb());
    }

    return {
        get() {
            return {
                search: state.search,
                filters: { ...state.filters },
                sort: state.sort,
                page: state.page,
                pageSize: state.pageSize
            };
        },
        setSearch(v) {
            state.search = v;
            state.page = 1;
            notify();
        },
        setFilter(key, v) {
            state.filters[key] = v;
            state.page = 1;
            notify();
        },
        setSort(v) {
            state.sort = v;
            state.page = 1;
            notify();
        },
        setPage(v) {
            state.page = v;
            notify();
        },
        setPageSize(v) {
            state.pageSize = v;
            state.page = 1;
            notify();
        },
        reset() {
            state.search = initialState.search;
            state.filters = { ...initialState.filters };
            state.sort = initialState.sort;
            state.page = 1;
            notify();
        },
        subscribe(cb) {
            subscribers.push(cb);
        },
        toParams() {
            const params = new URLSearchParams();
            if (state.search) params.set('search', state.search);
            for (const [key, value] of Object.entries(state.filters)) {
                if (value) params.set(key, value);
            }
            if (state.sort) params.set('sort', state.sort);
            params.set('page', state.page.toString());
            params.set('pageSize', state.pageSize.toString());
            return params;
        }
    };
}

export function bindListToolbar(containerEl, store, handlers) {
    const el = typeof containerEl === 'string' ? document.querySelector(containerEl) : containerEl;
    if (!el) return;

    const searchInput = el.querySelector('.toolbar-search');
    const filterSelects = el.querySelectorAll('.toolbar-filter');
    const sortSelect = el.querySelector('.toolbar-sort');
    const resetBtn = el.querySelector('.toolbar-reset');
    const prevBtn = el.querySelector('.pg-prev');
    const nextBtn = el.querySelector('.pg-next');

    if (searchInput && handlers.onSearch) {
        searchInput.addEventListener('input', debounce(() => {
            handlers.onSearch(searchInput.value);
        }, 300));
    }

    filterSelects.forEach(select => {
        select.addEventListener('change', () => {
            if (handlers.onFilter) handlers.onFilter(select.dataset.key, select.value);
        });
    });

    if (sortSelect && handlers.onSort) {
        sortSelect.addEventListener('change', () => {
            handlers.onSort(sortSelect.value);
        });
    }

    if (resetBtn && handlers.onReset) {
        resetBtn.addEventListener('click', () => {
            handlers.onReset();
            const s = store.get();
            if (searchInput) searchInput.value = s.search;
            filterSelects.forEach(select => {
                select.value = s.filters[select.dataset.key] || '';
            });
            if (sortSelect) sortSelect.value = s.sort;
        });
    }

    if (prevBtn && handlers.onPrev) {
        prevBtn.addEventListener('click', () => {
            handlers.onPrev();
        });
    }

    if (nextBtn && handlers.onNext) {
        nextBtn.addEventListener('click', () => {
            handlers.onNext();
        });
    }
}

export function syncToolbarUI(containerEl, store) {
    const el = typeof containerEl === 'string' ? document.querySelector(containerEl) : containerEl;
    if (!el) return;
    const s = store.get();
    const searchInput = el.querySelector('.toolbar-search');
    const filterSelects = el.querySelectorAll('.toolbar-filter');
    const sortSelect = el.querySelector('.toolbar-sort');
    if (searchInput) searchInput.value = s.search;
    filterSelects.forEach(select => {
        select.value = s.filters[select.dataset.key] || '';
    });
    if (sortSelect) sortSelect.value = s.sort;
}
