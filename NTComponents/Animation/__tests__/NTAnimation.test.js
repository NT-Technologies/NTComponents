import { jest } from '@jest/globals';
import { enhanceAll, onDispose, onLoad, onUpdate } from '../NTAnimation.razor.js';

class MockIntersectionObserver {
    static instances = [];
    static observeCalls = 0;
    static unobserveCalls = 0;

    constructor(callback, options) {
        if (options.rootMargin === 'bad') {
            throw new SyntaxError('Invalid rootMargin');
        }

        this.callback = callback;
        this.options = options;
        this.elements = new Set();
        this.disconnected = false;
        MockIntersectionObserver.instances.push(this);
    }

    observe(element) {
        MockIntersectionObserver.observeCalls++;
        this.elements.add(element);
    }

    unobserve(element) {
        MockIntersectionObserver.unobserveCalls++;
        this.elements.delete(element);
    }

    disconnect() {
        this.disconnected = true;
        this.elements.clear();
    }

    // Delivers one entry per observed element (or the supplied targets) with overridable geometry.
    trigger(init = {}, targets = Array.from(this.elements)) {
        this.callback(targets.map(target => entryFor(target, init)));
    }
}

const ROOT = { width: 1000, height: 800 };

function entryFor(target, { isIntersecting = true, intersectionRatio = isIntersecting ? 1 : 0, rect, root = ROOT } = {}) {
    const bounds = rect ?? target.getBoundingClientRect();
    return {
        target,
        isIntersecting,
        intersectionRatio,
        boundingClientRect: { width: bounds.width, height: bounds.height },
        rootBounds: root
    };
}

// Creates a connected visible-trigger wrapper. Defaults to a 100x100 box below the fold.
function createWrapper({ trigger = 'visible', repeat, threshold, rootMargin, notify = false, rect = {}, inViewport = false, className = 'nt-animation' } = {}) {
    const element = document.createElement('div');
    element.className = className;
    if (trigger !== null) {
        element.dataset.ntAnimationTrigger = trigger;
    }
    if (repeat !== undefined) {
        element.dataset.ntAnimationRepeat = repeat;
    }
    if (threshold !== undefined) {
        element.dataset.ntAnimationThreshold = String(threshold);
    }
    if (rootMargin !== undefined) {
        element.dataset.ntAnimationRootMargin = rootMargin;
    }
    if (notify) {
        element.setAttribute('data-nt-animation-notify', '');
    }

    const width = rect.width ?? 100;
    const height = rect.height ?? 100;
    const top = rect.top ?? (inViewport ? 10 : 2000);
    const box = { width, height, top, bottom: top + height, left: 0, right: width };
    element.getBoundingClientRect = () => box;
    document.body.appendChild(element);
    return element;
}

function createDotNetRef(impl) {
    return { invokeMethodAsync: jest.fn(impl ?? (() => Promise.resolve())) };
}

const stateOf = element => element.dataset.ntAnimationState;
const flush = () => new Promise(resolve => setTimeout(resolve, 0));

describe('NTAnimation runtime', () => {
    beforeEach(() => {
        MockIntersectionObserver.instances.length = 0;
        MockIntersectionObserver.observeCalls = 0;
        MockIntersectionObserver.unobserveCalls = 0;
        global.IntersectionObserver = MockIntersectionObserver;
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: ROOT.width });
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: ROOT.height });
    });

    afterEach(() => {
        document.body.innerHTML = '';
        onDispose();
    });

    describe('page-script lifecycle', () => {
        test('onDispose with a page-script element releases only wrappers that left the DOM', () => {
            const pageScript = document.createElement('nt-page-script');
            document.body.appendChild(pageScript);
            const first = createWrapper();
            const second = createWrapper();
            onUpdate(pageScript);
            const [observer] = MockIntersectionObserver.instances;
            expect(MockIntersectionObserver.instances).toHaveLength(1);
            expect(observer.elements.size).toBe(2);

            onDispose(pageScript);

            expect(observer.elements.size).toBe(2);
            expect(stateOf(first)).toBe('reset');
            expect(stateOf(second)).toBe('reset');

            first.remove();
            onDispose(pageScript);

            expect(observer.elements.has(first)).toBe(false);
            expect(observer.elements.has(second)).toBe(true);
            expect(observer.disconnected).toBe(false);
            expect(stateOf(first)).toBeUndefined();

            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });
            expect(stateOf(second)).toBe('visible');
        });

        test('repeated onUpdate with a page-script element does not rebuild observers', () => {
            const pageScript = document.createElement('nt-page-script');
            document.body.appendChild(pageScript);
            for (let i = 0; i < 20; i++) {
                createWrapper();
            }

            for (let i = 0; i < 5; i++) {
                onUpdate(pageScript);
            }

            expect(MockIntersectionObserver.instances).toHaveLength(1);
            expect(MockIntersectionObserver.observeCalls).toBe(20);
            expect(MockIntersectionObserver.unobserveCalls).toBe(0);
            expect(MockIntersectionObserver.instances[0].disconnected).toBe(false);
        });

        test('wrappers with identical rootMargin and ratio share one observer; different ones do not', () => {
            createWrapper({ rootMargin: '10px' });
            createWrapper({ rootMargin: '10px' });
            createWrapper({ rootMargin: '20px' });
            createWrapper({ rootMargin: '10px', threshold: 0.5 });

            onUpdate();

            expect(MockIntersectionObserver.instances).toHaveLength(3);
            expect(MockIntersectionObserver.instances.map(o => o.elements.size).sort()).toEqual([1, 1, 2]);
        });

        test('onDispose with an animation element cleans only that element', () => {
            const first = createWrapper();
            const second = createWrapper();
            onUpdate();
            const [observer] = MockIntersectionObserver.instances;

            onDispose(first);

            expect(observer.elements.has(first)).toBe(false);
            expect(observer.elements.has(second)).toBe(true);
            expect(stateOf(first)).toBeUndefined();
            expect(stateOf(second)).toBe('reset');
        });

        test('disposing the last element of a shared observer disconnects it', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            onDispose(element);

            expect(observer.disconnected).toBe(true);
        });

        test('enhanceAll ignores non-visible triggers and elements without the nt-animation class', () => {
            const visible = createWrapper();
            createWrapper({ trigger: 'render' });
            createWrapper({ trigger: 'manual' });
            createWrapper({ trigger: null });
            createWrapper({ className: 'other' });

            enhanceAll(document);

            expect(MockIntersectionObserver.instances).toHaveLength(1);
            expect([...MockIntersectionObserver.instances[0].elements]).toEqual([visible]);
            expect(document.querySelectorAll('[data-nt-animation-state]')).toHaveLength(1);
        });

        test('enhanceAll only synchronizes elements under the supplied root', () => {
            const inside = document.createElement('section');
            document.body.appendChild(inside);
            const insideWrapper = createWrapper();
            inside.appendChild(insideWrapper);
            const outside = createWrapper();

            enhanceAll(inside);

            expect(stateOf(insideWrapper)).toBe('reset');
            expect(stateOf(outside)).toBeUndefined();
        });
    });

    describe('initial state', () => {
        test('wrapper already in the viewport is visible and, with repeat once, never observed', () => {
            const element = createWrapper({ inViewport: true });

            onLoad(element);

            expect(stateOf(element)).toBe('visible');
            expect(MockIntersectionObserver.instances).toHaveLength(0);
        });

        test('wrapper in the viewport with repeat reverse is visible and still observed', () => {
            const element = createWrapper({ inViewport: true, repeat: 'reverse' });

            onLoad(element);

            expect(stateOf(element)).toBe('visible');
            expect(MockIntersectionObserver.instances[0].elements.has(element)).toBe(true);
        });

        test('wrapper below the viewport is reset and observed', () => {
            const element = createWrapper();

            onLoad(element);

            expect(stateOf(element)).toBe('reset');
            expect(MockIntersectionObserver.instances[0].elements.has(element)).toBe(true);
        });

        test('a state attribute already present is preserved', () => {
            const element = createWrapper({ inViewport: true });
            element.dataset.ntAnimationState = 'hidden';

            onLoad(element);

            expect(stateOf(element)).toBe('hidden');
        });

        test('disconnected elements are not tracked', () => {
            const element = createWrapper();
            element.remove();

            onLoad(element);

            expect(stateOf(element)).toBeUndefined();
            expect(MockIntersectionObserver.instances).toHaveLength(0);
        });
    });

    describe('reveal', () => {
        test('entry at the required ratio reveals the element and unobserves it for repeat once', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.35 });

            expect(stateOf(element)).toBe('visible');
            expect(observer.elements.has(element)).toBe(false);
        });

        test('entry just below the required ratio does not reveal and keeps observing', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.34 });

            expect(stateOf(element)).toBe('reset');
            expect(observer.elements.has(element)).toBe(true);
        });

        test('non-intersecting entry never reveals', () => {
            const element = createWrapper({ threshold: 0 });
            onLoad(element);

            MockIntersectionObserver.instances[0].trigger({ isIntersecting: false });

            expect(stateOf(element)).toBe('reset');
        });

        test('tall element uses a reduced threshold and reveals at that ratio', () => {
            const element = createWrapper({ rect: { width: 100, height: 3000, top: 1000 } });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            expect(observer.options.threshold).toEqual([0, 0.09]);

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.1 });

            expect(stateOf(element)).toBe('visible');
        });

        test('entry whose geometry implies a different ratio re-observes instead of revealing', () => {
            const element = createWrapper({ rect: { width: 100, height: 3000, top: 1000 } });
            onLoad(element);
            const [first] = MockIntersectionObserver.instances;

            first.trigger({ isIntersecting: true, intersectionRatio: 0.1, root: { width: 1000, height: 400 } });

            expect(stateOf(element)).toBe('reset');
            expect(first.elements.has(element)).toBe(false);
            expect(MockIntersectionObserver.instances).toHaveLength(2);
            const second = MockIntersectionObserver.instances[1];
            expect(second.options.threshold).toEqual([0, 0.04]);
            expect(second.elements.has(element)).toBe(true);
        });

        test('entries for untracked elements are ignored', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            const stray = createWrapper();

            observer.trigger({ isIntersecting: true }, [stray]);

            expect(stateOf(stray)).toBeUndefined();
        });
    });

    describe('repeat modes', () => {
        test('reverse hides revealed content when it drops below the ratio while still intersecting, and reveals it again', () => {
            const element = createWrapper({ repeat: 'reverse' });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.8 });
            expect(stateOf(element)).toBe('visible');
            expect(observer.elements.has(element)).toBe(true);

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.1 });
            expect(stateOf(element)).toBe('hidden');

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.5 });
            expect(stateOf(element)).toBe('visible');
        });

        test('reverse hides content that fully leaves the viewport', () => {
            const element = createWrapper({ repeat: 'reverse' });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            observer.trigger({ isIntersecting: false });

            expect(stateOf(element)).toBe('hidden');
        });

        test('reverse does not hide content that was never revealed', () => {
            const element = createWrapper({ repeat: 'reverse' });
            onLoad(element);

            MockIntersectionObserver.instances[0].trigger({ isIntersecting: true, intersectionRatio: 0.1 });

            expect(stateOf(element)).toBe('reset');
        });

        test('replay keeps content visible while it still intersects below the ratio', () => {
            const element = createWrapper({ repeat: 'replay' });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            observer.trigger({ isIntersecting: true, intersectionRatio: 0.1 });

            expect(stateOf(element)).toBe('visible');
        });

        test('replay resets content once it is fully out of view and reveals it again on return', () => {
            const element = createWrapper({ repeat: 'replay' });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            observer.trigger({ isIntersecting: false });
            expect(stateOf(element)).toBe('reset');

            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });
            expect(stateOf(element)).toBe('visible');
        });

        test('repeat once ignores leaving the viewport after reveal', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            observer.trigger({ isIntersecting: false }, [element]);

            expect(stateOf(element)).toBe('visible');
        });
    });

    describe('configuration from data attributes', () => {
        test('threshold 0 observes with a plain 0 threshold and reveals on any intersection', () => {
            const element = createWrapper({ threshold: 0 });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            expect(observer.options.threshold).toBe(0);

            observer.trigger({ isIntersecting: true, intersectionRatio: 0 });
            expect(stateOf(element)).toBe('visible');
        });

        test('rootMargin and threshold attributes reach the observer', () => {
            createWrapper({ threshold: 0.5, rootMargin: '0px 0px -10% 0px' });

            onUpdate();

            expect(MockIntersectionObserver.instances[0].options).toEqual({ rootMargin: '0px 0px -10% 0px', threshold: [0, 0.5] });
        });

        test.each([['abc'], [''], [undefined]])('unparseable threshold %p falls back to 0.35', threshold => {
            createWrapper({ threshold });

            onUpdate();

            expect(MockIntersectionObserver.instances[0].options.threshold).toEqual([0, 0.35]);
        });

        test('out-of-range thresholds are clamped', () => {
            createWrapper({ threshold: 5 });
            createWrapper({ threshold: -2 });

            onUpdate();

            const thresholds = MockIntersectionObserver.instances.map(o => o.options.threshold);
            expect(thresholds).toContainEqual([0, 1]);
            expect(thresholds).toContain(0);
        });

        test('missing rootMargin defaults to 0px', () => {
            createWrapper();

            onUpdate();

            expect(MockIntersectionObserver.instances[0].options.rootMargin).toBe('0px');
        });

        test('unknown repeat value behaves as once', () => {
            const element = createWrapper({ repeat: 'sideways' });
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            expect(observer.elements.has(element)).toBe(false);
        });

        test('invalid rootMargin leaves the element visible instead of stuck hidden', () => {
            const element = createWrapper({ rootMargin: 'bad' });

            onLoad(element);

            expect(stateOf(element)).toBe('visible');
            expect(MockIntersectionObserver.instances).toHaveLength(0);
        });
    });

    describe('updates', () => {
        test('changing the repeat attribute re-observes with the new behavior', () => {
            const element = createWrapper();
            onLoad(element);
            const [first] = MockIntersectionObserver.instances;

            element.dataset.ntAnimationRepeat = 'reverse';
            onUpdate(element);

            expect(first.elements.has(element)).toBe(false);
            expect(MockIntersectionObserver.instances).toHaveLength(2);
            const second = MockIntersectionObserver.instances[1];
            expect(second.elements.has(element)).toBe(true);

            second.trigger({ isIntersecting: true, intersectionRatio: 1 });
            expect(second.elements.has(element)).toBe(true);
            second.trigger({ isIntersecting: false });
            expect(stateOf(element)).toBe('hidden');
        });

        test('unchanged configuration is a no-op', () => {
            const element = createWrapper();
            onLoad(element);
            const observeCalls = MockIntersectionObserver.observeCalls;

            onUpdate(element);
            onUpdate(element);

            expect(MockIntersectionObserver.instances).toHaveLength(1);
            expect(MockIntersectionObserver.observeCalls).toBe(observeCalls);
            expect(MockIntersectionObserver.unobserveCalls).toBe(0);
        });

        test.each([['render'], [null]])('trigger changing to %p cleans the element up and removes its state', trigger => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;
            expect(stateOf(element)).toBe('reset');

            if (trigger === null) {
                delete element.dataset.ntAnimationTrigger;
            }
            else {
                element.dataset.ntAnimationTrigger = trigger;
            }
            onUpdate(element);

            expect(stateOf(element)).toBeUndefined();
            expect(observer.elements.has(element)).toBe(false);
            expect(observer.disconnected).toBe(true);
        });

        test('trigger changing to manual cleans up but leaves the Blazor-owned state attribute alone', () => {
            const element = createWrapper();
            onLoad(element);
            const [observer] = MockIntersectionObserver.instances;

            element.dataset.ntAnimationTrigger = 'manual';
            element.dataset.ntAnimationState = 'hidden';
            onUpdate(element);

            expect(stateOf(element)).toBe('hidden');
            expect(observer.elements.has(element)).toBe(false);
        });
    });

    describe('callbacks', () => {
        test('notifies entered on reveal and exited on reverse hide when the notify attribute is present', () => {
            const element = createWrapper({ repeat: 'reverse', notify: true });
            const ref = createDotNetRef();
            onLoad(element, ref);
            const [observer] = MockIntersectionObserver.instances;
            expect(ref.invokeMethodAsync).not.toHaveBeenCalled();

            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });
            expect(ref.invokeMethodAsync.mock.calls).toEqual([['NotifyEnteredAsync']]);

            observer.trigger({ isIntersecting: false });
            expect(ref.invokeMethodAsync.mock.calls).toEqual([['NotifyEnteredAsync'], ['NotifyExitedAsync']]);
        });

        test('notifies exited when replay resets revealed content', () => {
            const element = createWrapper({ repeat: 'replay', notify: true });
            const ref = createDotNetRef();
            onLoad(element, ref);
            const [observer] = MockIntersectionObserver.instances;
            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });

            observer.trigger({ isIntersecting: false });

            expect(ref.invokeMethodAsync.mock.calls).toEqual([['NotifyEnteredAsync'], ['NotifyExitedAsync']]);
        });

        test('does not notify without the notify attribute', () => {
            const element = createWrapper({ repeat: 'reverse' });
            const ref = createDotNetRef();
            onLoad(element, ref);
            const [observer] = MockIntersectionObserver.instances;

            observer.trigger({ isIntersecting: true, intersectionRatio: 1 });
            observer.trigger({ isIntersecting: false });

            expect(stateOf(element)).toBe('hidden');
            expect(ref.invokeMethodAsync).not.toHaveBeenCalled();
        });

        test('wrapper that is visible on first load with a ref notifies entered once', () => {
            const element = createWrapper({ inViewport: true, notify: true });
            const ref = createDotNetRef();

            onLoad(element, ref);
            onUpdate(element, ref);
            onUpdate(element, ref);

            expect(ref.invokeMethodAsync.mock.calls).toEqual([['NotifyEnteredAsync']]);
        });

        test('attaching a ref to an already-visible tracked element notifies entered exactly once', () => {
            const element = createWrapper({ inViewport: true, notify: true });
            onLoad(element);
            const ref = createDotNetRef();

            onUpdate(element, ref);
            onUpdate(element, ref);
            onUpdate(element);

            expect(ref.invokeMethodAsync.mock.calls).toEqual([['NotifyEnteredAsync']]);
        });

        test('a rejected callback does not throw or break later reveals', async () => {
            const unhandled = jest.fn();
            process.on('unhandledRejection', unhandled);
            const element = createWrapper({ repeat: 'reverse', notify: true });
            const ref = createDotNetRef(() => Promise.reject(new Error('disconnected')));
            onLoad(element, ref);
            const [observer] = MockIntersectionObserver.instances;

            expect(() => observer.trigger({ isIntersecting: true, intersectionRatio: 1 })).not.toThrow();
            await flush();
            process.off('unhandledRejection', unhandled);

            expect(stateOf(element)).toBe('visible');
            expect(ref.invokeMethodAsync).toHaveBeenCalledWith('NotifyEnteredAsync');
            expect(unhandled).not.toHaveBeenCalled();
        });
    });
});
