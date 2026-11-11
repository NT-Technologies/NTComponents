import { onDispose, onLoad, onUpdate } from '../NTLazyLoader.razor.js';
import { jest } from '@jest/globals';

class FakeIntersectionObserver {
    static instances = [];

    constructor(callback, options) {
        this.callback = callback;
        this.options = options;
        this.observed = [];
        this.disconnect = jest.fn();
        FakeIntersectionObserver.instances.push(this);
    }

    observe(element) {
        this.observed.push(element);
    }

    report(isIntersecting) {
        this.callback(this.observed.map(target => ({ target, isIntersecting })), this);
    }
}

function createPlaceholder() {
    const placeholder = document.createElement('div');
    document.body.append(placeholder);
    return placeholder;
}

function createReference() {
    return { invokeMethodAsync: jest.fn(() => Promise.resolve()) };
}

describe('NTLazyLoader browser behavior', () => {
    const originalIntersectionObserver = globalThis.IntersectionObserver;

    beforeEach(() => {
        document.body.innerHTML = '';
        FakeIntersectionObserver.instances = [];
        globalThis.IntersectionObserver = FakeIntersectionObserver;
    });

    afterEach(() => {
        globalThis.IntersectionObserver = originalIntersectionObserver;
    });

    test('watches the placeholder with a margin so content renders before it scrolls into view', () => {
        const placeholder = createPlaceholder();

        onLoad(placeholder, createReference());

        const [observer] = FakeIntersectionObserver.instances;
        expect(observer.observed).toEqual([placeholder]);
        expect(observer.options).toEqual({ rootMargin: '400px' });
    });

    test('asks for the content once the placeholder is visible and stops watching', () => {
        const reference = createReference();
        onLoad(createPlaceholder(), reference);
        const [observer] = FakeIntersectionObserver.instances;

        observer.report(true);

        expect(reference.invokeMethodAsync).toHaveBeenCalledTimes(1);
        expect(reference.invokeMethodAsync).toHaveBeenCalledWith('ShowAsync');
        expect(observer.disconnect).toHaveBeenCalled();
    });

    test('keeps waiting while the placeholder is not visible', () => {
        const reference = createReference();
        onLoad(createPlaceholder(), reference);
        const [observer] = FakeIntersectionObserver.instances;

        observer.report(false);
        onUpdate();

        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
        expect(observer.disconnect).not.toHaveBeenCalled();
    });

    test('loading the same placeholder again replaces its observer', () => {
        const placeholder = createPlaceholder();
        onLoad(placeholder, createReference());

        onLoad(placeholder, createReference());

        expect(FakeIntersectionObserver.instances[0].disconnect).toHaveBeenCalled();
        expect(FakeIntersectionObserver.instances).toHaveLength(2);
    });

    test('disposing stops watching the placeholder', () => {
        const placeholder = createPlaceholder();
        onLoad(placeholder, createReference());

        onDispose(placeholder);

        expect(FakeIntersectionObserver.instances[0].disconnect).toHaveBeenCalled();
    });

    test('ignores a missing placeholder', () => {
        expect(() => {
            onLoad(null, createReference());
            onDispose(null);
        }).not.toThrow();
        expect(FakeIntersectionObserver.instances).toHaveLength(0);
    });
});

describe('NTLazyLoader static SSR behavior', () => {
    const originalIntersectionObserver = globalThis.IntersectionObserver;
    const pageScript = document.createElement('nt-page-script');

    beforeEach(() => {
        document.body.innerHTML = '';
        FakeIntersectionObserver.instances = [];
        globalThis.IntersectionObserver = FakeIntersectionObserver;
    });

    afterEach(() => {
        onDispose();
        globalThis.IntersectionObserver = originalIntersectionObserver;
    });

    function setStaticPlaceholder(html) {
        document.body.innerHTML = `<section><div class="nt-lazy-loader" data-nt-lazy-loader aria-hidden="true"><template>${html}</template></div></section>`;
        return document.querySelector('[data-nt-lazy-loader]');
    }

    test('keeps the content out of the page and watches the placeholder', () => {
        const placeholder = setStaticPlaceholder('<p class="lazy-content">Expensive editor</p>');

        onLoad(pageScript);

        const [observer] = FakeIntersectionObserver.instances;
        expect(observer.observed).toEqual([placeholder]);
        expect(observer.options).toEqual({ rootMargin: '400px' });
        expect(document.querySelector('.lazy-content')).toBeNull();
        expect(placeholder.querySelector('template')).toBeNull();
    });

    test('replaces the placeholder with the content once it is visible', () => {
        setStaticPlaceholder('<p class="lazy-content">Expensive editor</p>');
        onLoad(pageScript);
        const [observer] = FakeIntersectionObserver.instances;

        observer.report(true);

        expect(document.querySelector('section').innerHTML).toBe('<p class="lazy-content">Expensive editor</p>');
        expect(observer.disconnect).toHaveBeenCalled();
    });

    test('keeps the content out of the page while the placeholder is not visible', () => {
        setStaticPlaceholder('<p class="lazy-content">Expensive editor</p>');
        onLoad(pageScript);

        FakeIntersectionObserver.instances[0].report(false);

        expect(document.querySelector('.lazy-content')).toBeNull();
    });

    test('updating does not watch a placeholder twice', () => {
        setStaticPlaceholder('<p>Content</p>');
        onLoad(pageScript);

        onUpdate(pageScript);

        expect(FakeIntersectionObserver.instances).toHaveLength(1);
    });

    test('shows the content of a template that enhanced navigation inserted into a kept placeholder', () => {
        const placeholder = setStaticPlaceholder('<p class="lazy-content">Old page</p>');
        onLoad(pageScript);
        const template = document.createElement('template');
        template.innerHTML = '<p class="lazy-content">New page</p>';
        placeholder.append(template);

        onUpdate(pageScript);
        FakeIntersectionObserver.instances[1].report(true);

        expect(FakeIntersectionObserver.instances[0].disconnect).toHaveBeenCalled();
        expect(document.querySelector('.lazy-content').textContent).toBe('New page');
    });

    test('stops watching placeholders that navigation removed', () => {
        setStaticPlaceholder('<p>Content</p>');
        onLoad(pageScript);
        document.body.innerHTML = '';

        onDispose(pageScript);

        expect(FakeIntersectionObserver.instances[0].disconnect).toHaveBeenCalled();
    });

    test('ignores placeholders rendered interactively', () => {
        document.body.innerHTML = '<div class="nt-lazy-loader" aria-hidden="true"></div>';

        onLoad(pageScript);

        expect(FakeIntersectionObserver.instances).toHaveLength(0);
    });
});
