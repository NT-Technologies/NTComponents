type Maybe<T> = T | null | undefined;

interface SavedScroll {
    path: string;
    top: number;
}

interface BodyScrollState {
    body: HTMLElement;
    observer: MutationObserver;
    path: string;
    // The history entry the body is showing. It changes with the path, so scrolling before the new page renders still counts for the old one.
    entryKey?: string;
    saved: Map<string, SavedScroll>;
    // Retried as the page renders until it reaches its target. User input or the next navigation drops it.
    pending: (() => boolean) | null;
    onScroll: () => void;
    onUserInput: () => void;
}

const userInputEvents = ['wheel', 'touchstart', 'keydown', 'pointerdown'];

// Keyed by the body's nt-page-script marker, which is what the page-script loader passes to every hook.
const states = new Map<Element, BodyScrollState>();

// The Navigation API key stays with a history entry across Back and Forward. Without the API, Back and Forward start at the top.
function getEntryKey(): string | undefined {
    return (window as Window & { navigation?: { currentEntry?: { key?: string } | null } }).navigation?.currentEntry?.key;
}

function getFragmentId(): string {
    const fragment = window.location.hash.slice(1);

    try {
        return decodeURIComponent(fragment);
    } catch {
        return fragment;
    }
}

// Scrolls the URL fragment's target into view once it is in the body. Null when the URL has no fragment.
function fragmentScroll(body: HTMLElement): (() => boolean) | null {
    const id = getFragmentId();

    return id ? () => {
        const target = document.getElementById(id);

        if (!target || !body.contains(target)) {
            return false;
        }

        target.scrollIntoView();
        return true;
    } : null;
}

// The offset is clamped until the page is tall enough, so the restore counts only once it lands.
function restoreTop(body: HTMLElement, top: number): boolean {
    body.scrollTop = top;
    return Math.abs(body.scrollTop - top) < 1;
}

function applyPending(state: BodyScrollState): void {
    if (state.pending?.()) {
        state.pending = null;
    }
}

// The body is the page's scroll container, so the browser's own navigation scrolling never reaches it. A page the user comes back to
// returns to where they left it; a new page starts at the top, or at its fragment target. Query-string and fragment changes on the
// same path keep the position, as the browser does.
function syncNavigationScroll(state: BodyScrollState): void {
    const path = window.location.pathname;

    if (path === state.path) {
        applyPending(state);
        return;
    }

    state.path = path;
    state.entryKey = getEntryKey();
    const saved = state.entryKey ? state.saved.get(state.entryKey) : undefined;

    if (saved?.path === path) {
        state.pending = () => restoreTop(state.body, saved.top);
    } else {
        state.body.scrollTop = 0;
        state.pending = fragmentScroll(state.body);
    }

    applyPending(state);
}

export function onLoad(element?: Maybe<Element>): void {
    const body = element?.closest<HTMLElement>('.nt-body');

    if (!element || !body || states.has(element)) {
        return;
    }

    const state: BodyScrollState = {
        body,
        observer: new MutationObserver(() => syncNavigationScroll(state)),
        path: window.location.pathname,
        entryKey: getEntryKey(),
        saved: new Map(),
        pending: fragmentScroll(body),
        onScroll: () => {
            if (state.entryKey) {
                state.saved.set(state.entryKey, { path: state.path, top: body.scrollTop });
            }
        },
        onUserInput: () => {
            state.pending = null;
        }
    };
    // Interactive navigation renders the new page into the body without notifying this module, so watch for that render.
    state.observer.observe(body, { childList: true, subtree: true });
    body.addEventListener('scroll', state.onScroll, { passive: true });
    userInputEvents.forEach(name => body.addEventListener(name, state.onUserInput, { passive: true }));
    states.set(element, state);
    applyPending(state);
}

export function onUpdate(element?: Maybe<Element>): void {
    const state = element ? states.get(element) : undefined;

    if (state) {
        syncNavigationScroll(state);
    }
}

export function onDispose(element?: Maybe<Element>): void {
    const disposed = element ? [element] : [...states.keys()];

    disposed.forEach(key => {
        const state = states.get(key);

        if (state) {
            state.observer.disconnect();
            state.body.removeEventListener('scroll', state.onScroll);
            userInputEvents.forEach(name => state.body.removeEventListener(name, state.onUserInput));
            states.delete(key);
        }
    });
}
