window.ntDocs = window.ntDocs || {};

window.ntDocs.scrollToFragment = (fragmentId, updateHistory) => {
    if (!fragmentId) {
        return;
    }

    const normalizedId = fragmentId.replace(/^#/, "");
    const target = document.getElementById(normalizedId);
    if (!target) {
        return;
    }

    target.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });

    if (updateHistory) {
        const baseUrl = window.location.pathname + window.location.search;
        window.history.replaceState(null, "", `${baseUrl}#${normalizedId}`);
    }
};

window.ntDocs.copyText = async (text) => {
    if (!navigator.clipboard) {
        return;
    }

    await navigator.clipboard.writeText(text || "");
};

// Logs each action pressed in an example preview, so buttons that exist only for the demo still give feedback in the debug console.
const exampleActionSelector = "button, a[href], [role='button'], [role='link'], [role='menuitem'], [role='menuitemradio'], [role='menuitemcheckbox'], [role='tab'], [role='option']";

// The visible label: the accessible name when there is one, otherwise the text without icon ligatures or inline scripts.
function exampleActionLabel(action) {
    const named = action.getAttribute("aria-label");
    if (named) {
        return named.trim();
    }

    let text = "";
    const walker = document.createTreeWalker(action, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        if (!node.parentElement?.closest("script, style, .nt-icon, [aria-hidden='true']")) {
            text += ` ${node.textContent}`;
        }
    }

    return text.replace(/\s+/g, " ").trim()
        || action.getAttribute("title")?.trim()
        || action.querySelector(".nt-icon")?.textContent?.trim()
        || "unlabeled";
}

window.ntDocs.watchExampleActions = (preview, dotNetRef) => {
    preview.addEventListener("click", (event) => {
        const action = event.target instanceof Element ? event.target.closest(exampleActionSelector) : null;
        if (!action || !preview.contains(action) || action.matches(":disabled, [aria-disabled='true']")) {
            return;
        }

        const kind = action.getAttribute("role") || (action.localName === "a" ? "link" : "button");
        dotNetRef.invokeMethodAsync("LogAction", exampleActionLabel(action), kind).catch(() => undefined);
    });
};
