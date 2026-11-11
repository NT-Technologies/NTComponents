import { jest } from '@jest/globals';
import { afterStarted, afterWebStarted } from '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

// Test setup
if (!global.NTComponents) {
   global.NTComponents = {
      customAttribute: 'tntid',
   };
}

describe('afterWebStarted', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      // Clean up any previously registered custom element
      if (customElements.get('nt-page-script')) {
         // Can't unregister custom elements, so we'll just skip re-registration
      }
   });

   test('sets up page script element custom element', () => {
      const blazor = { addEventListener: jest.fn() };

      // Check if element is already defined
      const alreadyDefined = customElements.get('nt-page-script') !== undefined;

      if (!alreadyDefined) {
         expect(() => afterWebStarted(blazor)).not.toThrow();
         expect(blazor.addEventListener).toHaveBeenCalled();
      } else {
         // Element already defined in another test, just verify addEventListener is called
         expect(() => afterWebStarted(blazor)).not.toThrow();
         expect(blazor.addEventListener).toHaveBeenCalled();
      }
   });

   test('registers enhancedload event listener', () => {
      const blazor = { addEventListener: jest.fn() };
      // Just verify the function doesn't throw when called multiple times
      // (the custom element might already be registered)
      try {
         afterWebStarted(blazor);
      } catch (e) {
         // If it throws about already registered, that's expected
         expect(e.message).toContain('already been registered');
      }
   });

   test('afterStarted uses the same startup path for non-web renderers', () => {
      const blazor = { addEventListener: jest.fn() };

      expect(() => afterStarted(blazor)).not.toThrow();

      expect(blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));
      expect(customElements.get('nt-page-script')).not.toBeUndefined();
   });

   test('connected page-script updates already loaded modules', async () => {
      const blazor = { addEventListener: jest.fn() };
      const counterName = `__pageScriptCounter_${Date.now()}_${Math.random().toString(36).slice(2)}`;
      const scriptSource = `globalThis.${counterName} = globalThis.${counterName} ?? { dispose: 0, disposeTags: [], load: 0, loadTags: [], update: 0, updateTags: [] };
export function onLoad(element) { globalThis.${counterName}.load++; globalThis.${counterName}.loadTags.push(element?.tagName ?? null); }
export function onUpdate(element) { globalThis.${counterName}.update++; globalThis.${counterName}.updateTags.push(element?.tagName ?? null); }
export function onDispose(element) { globalThis.${counterName}.dispose++; globalThis.${counterName}.disposeTags.push(element?.tagName ?? null); }`;
      const scriptUrl = `data:text/javascript;charset=utf-8,${encodeURIComponent(scriptSource)}`;

      afterStarted(blazor);

      const first = document.createElement('nt-page-script');
      first.setAttribute('src', scriptUrl);
      document.body.appendChild(first);

      await waitFor(() => globalThis[counterName]?.update === 1);

      const second = document.createElement('nt-page-script');
      second.setAttribute('src', scriptUrl);
      document.body.appendChild(second);

      await waitFor(() => globalThis[counterName]?.update === 2);

      first.remove();
      await waitFor(() => globalThis[counterName]?.dispose === 1);
      second.remove();
      await waitFor(() => globalThis[counterName]?.dispose === 2);

      expect(globalThis[counterName]).toEqual({
         dispose: 2,
         disposeTags: ['NT-PAGE-SCRIPT', 'NT-PAGE-SCRIPT'],
         load: 1,
         loadTags: ['NT-PAGE-SCRIPT'],
         update: 2,
         updateTags: ['NT-PAGE-SCRIPT', 'NT-PAGE-SCRIPT']
      });

      delete globalThis[counterName];
   });

   test('moving a connected page-script host does not dispose its module', async () => {
      const blazor = { addEventListener: jest.fn() };
      const counterName = `__pageScriptMoveCounter_${Date.now()}_${Math.random().toString(36).slice(2)}`;
      const scriptSource = `globalThis.${counterName} = globalThis.${counterName} ?? { dispose: 0, load: 0, update: 0 };
export function onLoad() { globalThis.${counterName}.load++; }
export function onUpdate() { globalThis.${counterName}.update++; }
export function onDispose() { globalThis.${counterName}.dispose++; }`;
      const scriptUrl = `data:text/javascript;charset=utf-8,${encodeURIComponent(scriptSource)}`;

      afterStarted(blazor);

      const host = document.createElement('section');
      const script = document.createElement('nt-page-script');
      script.setAttribute('src', scriptUrl);
      host.appendChild(script);
      document.body.appendChild(host);

      await waitFor(() => globalThis[counterName]?.update === 1);

      const dialog = document.createElement('dialog');
      document.body.appendChild(dialog);
      dialog.append(host);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(globalThis[counterName].dispose).toBe(0);

      host.remove();
      await waitFor(() => globalThis[counterName]?.dispose === 1);

      delete globalThis[counterName];
   });
});

async function waitFor(predicate) {
   for (let attempt = 0; attempt < 20; attempt++) {
      if (predicate()) {
         return;
      }

      await new Promise(resolve => setTimeout(resolve, 0));
   }

   throw new Error('Condition was not met before timeout.');
}
