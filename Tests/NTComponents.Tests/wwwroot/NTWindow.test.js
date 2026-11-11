import { jest } from '@jest/globals';
import { onDispose, onLoad } from '../../../NTComponents/Window/NTWindow.razor.js';

describe('NTWindow module', () => {
   const initialInnerHeight = window.innerHeight;
   const initialInnerWidth = window.innerWidth;

   beforeEach(() => {
      jest.useFakeTimers();
      document.body.innerHTML = '';
   });

   afterEach(() => {
      expect(jest.getTimerCount()).toBe(0);
      jest.runOnlyPendingTimers();
      jest.useRealTimers();
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: initialInnerHeight });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: initialInnerWidth });
      document.body.innerHTML = '';
   });

   function createWindow({ initialize = true } = {}) {
      document.body.innerHTML = `
         <section id="report-window"
                  class="nt-window"
                  role="dialog"
                  data-nt-window="true"
                  data-nt-window-dock-position="bottom-right"
                  data-nt-window-draggable="true"
                  data-nt-window-state="normal">
            <header class="nt-window-header" data-nt-window-drag-handle="true">
               <h2 class="nt-window-title">Report</h2>
               <div class="nt-window-controls">
                  <button data-nt-window-action="minimize" aria-label="Minimize Report" aria-expanded="true">
                     <span class="nt-icon">minimize</span>
                  </button>
                  <button data-nt-window-action="fullscreen" aria-label="Show Report fullscreen" aria-pressed="false">
                     <span class="nt-icon">fullscreen</span>
                  </button>
                  <button data-nt-window-action="close" aria-label="Close Report">
                     <span class="nt-icon">close</span>
                  </button>
               </div>
            </header>
            <div class="nt-window-content-frame" aria-hidden="false">
               <div class="nt-window-content">Quarterly results</div>
            </div>
         </section>
         <nt-page-script></nt-page-script>`;

      const element = document.querySelector('[data-nt-window]');
      const pageScript = document.querySelector('nt-page-script');
      if (initialize) {
         onLoad(pageScript);
      }
      return { element, pageScript };
   }

   test('static markup supports minimize, fullscreen, and close without Blazor', () => {
      const { element } = createWindow();

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.dataset.ntWindowState).toBe('minimized');
      expect(element.classList.contains('nt-window-minimized')).toBe(true);
      expect(element.querySelector('.nt-window-content-frame').getAttribute('aria-hidden')).toBe('true');
      expect(element.querySelector('[data-nt-window-action="minimize"]').getAttribute('aria-label')).toBe('Restore Report');

      element.querySelector('[data-nt-window-action="fullscreen"]').click();

      expect(element.dataset.ntWindowState).toBe('fullscreen');
      expect(element.classList.contains('nt-window-fullscreen')).toBe(true);
      expect(element.classList.contains('nt-window-minimized')).toBe(false);
      expect(element.querySelector('[data-nt-window-action="fullscreen"]').getAttribute('aria-label')).toBe('Restore Report');

      element.querySelector('[data-nt-window-action="close"]').click();

      expect(element.classList.contains('nt-window-closing')).toBe(true);
      jest.advanceTimersByTime(200);
      expect(element.hidden).toBe(true);
   });

   test('browser bridge reopens a statically closed window', () => {
      const { element } = createWindow();
      element.hidden = true;

      expect(window.NTWindow.openWindow('report-window')).toBe(true);
      expect(element.hidden).toBe(false);
   });

   test('reopening a closing window cancels the pending close', () => {
      const { element } = createWindow();
      const closeHandler = jest.fn();
      element.addEventListener('ntwindowclose', closeHandler);

      expect(window.NTWindow.closeWindow(element)).toBe(true);
      expect(element.classList.contains('nt-window-closing')).toBe(true);

      expect(window.NTWindow.openWindow(element)).toBe(true);
      jest.advanceTimersByTime(200);

      expect(element.hidden).toBe(false);
      expect(element.classList.contains('nt-window-closing')).toBe(false);
      expect(closeHandler).not.toHaveBeenCalled();
   });

   test('closing an already closing window is idempotent', () => {
      const { element } = createWindow();
      const closeHandler = jest.fn();
      element.addEventListener('ntwindowclose', closeHandler);

      expect(window.NTWindow.closeWindow(element)).toBe(true);
      expect(window.NTWindow.closeWindow(element)).toBe(false);
      jest.advanceTimersByTime(200);

      expect(element.hidden).toBe(true);
      expect(closeHandler).toHaveBeenCalledTimes(1);
   });

   test('data attribute trigger reopens a window from static markup', () => {
      const { element } = createWindow();
      const trigger = document.createElement('button');
      trigger.dataset.ntWindowOpen = 'report-window';
      document.body.appendChild(trigger);
      element.hidden = true;

      trigger.click();

      expect(element.hidden).toBe(false);
   });

   test('dragging the header moves and clamps the floating window', () => {
      const { element } = createWindow();
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 800 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
      element.getBoundingClientRect = jest.fn(() => ({
         bottom: 320,
         height: 200,
         left: 100,
         right: 400,
         toJSON: () => ({}),
         top: 120,
         width: 300,
         x: 100,
         y: 120
      }));

      element.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));
      window.dispatchEvent(new MouseEvent('pointermove', {
         clientX: 900,
         clientY: 900
      }));

      expect(element.style.left).toBe('500px');
      expect(element.style.top).toBe('400px');
      expect(element.style.translate).toBe('none');
      expect(element.dataset.ntWindowDragging).toBe('true');

      window.dispatchEvent(new MouseEvent('pointerup'));
      expect(element.dataset.ntWindowDragging).toBeUndefined();
   });

   test('a window rendered after the page script loaded can still be raised and dragged', () => {
      // An interactive render can open a window long after the page script ran onLoad, so nothing initialized it.
      const { element } = createWindow({ initialize: false });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 800 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
      element.getBoundingClientRect = jest.fn(() => ({ bottom: 320, height: 200, left: 100, right: 400, toJSON: () => ({}), top: 120, width: 300, x: 100, y: 120 }));

      element.querySelector('.nt-window-title').dispatchEvent(new MouseEvent('pointerdown', { bubbles: true, button: 0, clientX: 150, clientY: 150 }));
      window.dispatchEvent(new MouseEvent('pointermove', { clientX: 250, clientY: 200 }));
      window.dispatchEvent(new MouseEvent('pointerup'));

      expect(element.style.left).toBe('200px');
      expect(element.style.top).toBe('170px');
      expect(Number(element.style.zIndex)).toBeGreaterThan(1100);
   });

   test('pressing a window control or the content does not start a drag', () => {
      const { element } = createWindow({ initialize: false });

      element.querySelector('[data-nt-window-action="minimize"]').dispatchEvent(new MouseEvent('pointerdown', { bubbles: true, button: 0 }));
      element.querySelector('.nt-window-content').dispatchEvent(new MouseEvent('pointerdown', { bubbles: true, button: 0 }));

      expect(element.dataset.ntWindowDragging).toBeUndefined();
      expect(element.style.left).toBe('');
   });

   test('disposing a removed page-script marker cleans up an active drag', () => {
      const { element, pageScript } = createWindow();
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 800 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
      element.getBoundingClientRect = jest.fn(() => ({
         bottom: 320,
         height: 200,
         left: 100,
         right: 400,
         toJSON: () => ({}),
         top: 120,
         width: 300,
         x: 100,
         y: 120
      }));

      element.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));
      const leftAtDisposal = element.style.left;
      const topAtDisposal = element.style.top;

      pageScript.remove();
      onDispose(pageScript);
      const draggingAfterDisposal = element.dataset.ntWindowDragging;
      window.dispatchEvent(new MouseEvent('pointermove', {
         clientX: 700,
         clientY: 500
      }));
      const leftAfterMove = element.style.left;
      const topAfterMove = element.style.top;
      window.dispatchEvent(new MouseEvent('pointerup'));

      expect(draggingAfterDisposal).toBeUndefined();
      expect(leftAfterMove).toBe(leftAtDisposal);
      expect(topAfterMove).toBe(topAtDisposal);
   });

   test('grabbing a window raises it above every existing window', () => {
      const { element } = createWindow();
      const second = element.cloneNode(true);
      second.id = 'second-window';
      document.body.insertBefore(second, document.querySelector('nt-page-script'));
      element.style.zIndex = '9000';
      onLoad(document.body);

      second.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));

      expect(Number(second.style.zIndex)).toBeGreaterThan(Number(element.style.zIndex));
      window.dispatchEvent(new MouseEvent('pointerup'));
   });

   test('the most recently grabbed window stays above many competing windows', () => {
      const { element, pageScript } = createWindow();
      const windows = [element];

      for (let index = 1; index < 48; index++) {
         const candidate = element.cloneNode(true);
         candidate.id = `stress-window-${index}`;
         candidate.style.zIndex = String(5000 + index * 17);
         document.body.insertBefore(candidate, pageScript);
         windows.push(candidate);
      }
      onLoad(document.body);

      const grabOrder = [...windows].reverse().concat(windows.filter((_, index) => index % 2 === 0));
      for (const candidate of grabOrder) {
         candidate.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
            bubbles: true,
            button: 0,
            clientX: 150,
            clientY: 150
         }));
         window.dispatchEvent(new MouseEvent('pointerup'));

         const competingZIndexes = windows
            .filter(windowElement => windowElement !== candidate)
            .map(windowElement => Number(window.getComputedStyle(windowElement).zIndex));
         expect(Number(window.getComputedStyle(candidate).zIndex)).toBeGreaterThan(Math.max(...competingZIndexes));
      }
   });

   test('minimizing docks the window and restoring returns it to its floating position', () => {
      const { element } = createWindow();
      element.style.inset = 'auto';
      element.style.left = '120px';
      element.style.top = '80px';
      element.style.translate = 'none';

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.left).toBe('');
      expect(element.style.top).toBe('');
      expect(element.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.inset).toBe('auto');
      expect(element.style.left).toBe('120px');
      expect(element.style.top).toBe('80px');
      expect(element.style.translate).toBe('none');
   });

   test('minimized windows sharing a dock stack without overlapping', () => {
      const { element } = createWindow();
      const second = element.cloneNode(true);
      second.id = 'second-window';
      document.body.insertBefore(second, document.querySelector('nt-page-script'));
      onLoad(document.body);

      element.querySelector('[data-nt-window-action="minimize"]').click();
      second.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');
      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('56px');
   });

   test('removing a minimized window compacts the remaining docked windows', () => {
      const { element, pageScript } = createWindow();
      const second = element.cloneNode(true);
      const secondPageScript = document.createElement('nt-page-script');
      second.id = 'second-window';
      document.body.append(second, secondPageScript);
      onLoad(secondPageScript);

      element.querySelector('[data-nt-window-action="minimize"]').click();
      second.querySelector('[data-nt-window-action="minimize"]').click();
      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('56px');

      element.remove();
      pageScript.remove();
      onDispose(pageScript);

      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');
   });

   test('progressive enhancement preserves custom state control labels', () => {
      const { element, pageScript } = createWindow({ initialize: false });
      const minimizeButton = element.querySelector('[data-nt-window-action="minimize"]');
      const fullscreenButton = element.querySelector('[data-nt-window-action="fullscreen"]');
      minimizeButton.setAttribute('aria-label', 'Collapse report workspace');
      fullscreenButton.setAttribute('aria-label', 'Expand report workspace');

      onLoad(pageScript);

      expect(minimizeButton.getAttribute('aria-label')).toBe('Collapse report workspace');
      expect(fullscreenButton.getAttribute('aria-label')).toBe('Expand report workspace');

      minimizeButton.click();
      fullscreenButton.click();

      expect(minimizeButton.getAttribute('aria-label')).toBe('Collapse report workspace');
      expect(fullscreenButton.getAttribute('aria-label')).toBe('Expand report workspace');
   });

   test('onDispose removes progressive enhancement handlers', () => {
      const { element, pageScript } = createWindow();

      onDispose(pageScript);
      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.dataset.ntWindowState).toBe('normal');
   });

   // jsdom has no layout, so each window reports a centred box that moves with its cascade offset, like the real translate does.
   function layOut(element, { left = 300, top = 200, width = 300, height = 200 } = {}) {
      element.getBoundingClientRect = () => {
         const x = left + (Number.parseFloat(element.style.getPropertyValue('--nt-window-cascade-offset-x')) || 0);
         const y = top + (Number.parseFloat(element.style.getPropertyValue('--nt-window-cascade-offset-y')) || 0);
         return { bottom: y + height, height, left: x, right: x + width, toJSON: () => ({}), top: y, width, x, y };
      };
   }

   function addWindow(id, layout, { first = false } = {}) {
      const element = document.querySelector('[data-nt-window]').cloneNode(true);
      element.id = id;
      element.removeAttribute('style');
      if (first) {
         document.body.prepend(element);
      } else {
         document.body.insertBefore(element, document.querySelector('nt-page-script'));
      }
      layOut(element, layout);
      return element;
   }

   function offset(element) {
      return [element.style.getPropertyValue('--nt-window-cascade-offset-x'), element.style.getPropertyValue('--nt-window-cascade-offset-y')];
   }

   function setViewport(width, height) {
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: width });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: height });
   }

   test('windows initialized together cascade sequentially', () => {
      const { element } = createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(element);
      const second = addWindow('second');
      const third = addWindow('third');

      onLoad(document.body);

      expect(offset(element)).toEqual(['0px', '0px']);
      expect(offset(second)).toEqual(['24px', '24px']);
      expect(offset(third)).toEqual(['48px', '48px']);
   });

   test('a slot taken by a dragged window is skipped', () => {
      const { element } = createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(element);
      const dragged = addWindow('dragged', undefined, { first: true });
      dragged.style.left = '300px';
      dragged.style.top = '200px';
      const other = addWindow('other', { left: 324, top: 224 }, { first: true });
      other.style.left = '324px';
      other.style.top = '224px';

      onLoad(document.body);

      expect(offset(dragged)).toEqual(['', '']);
      expect(offset(other)).toEqual(['', '']);
      expect(offset(element)).toEqual(['48px', '48px']);
   });

   test('a slot is reused after its window closes', () => {
      createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(document.querySelector('[data-nt-window]'));
      const second = addWindow('second');
      const third = addWindow('third');
      onLoad(document.body);
      expect(offset(second)).toEqual(['24px', '24px']);
      expect(offset(third)).toEqual(['48px', '48px']);

      window.NTWindow.closeWindow(second);
      jest.advanceTimersByTime(200);
      expect(second.hidden).toBe(true);
      window.NTWindow.openWindow(second);

      expect(offset(second)).toEqual(['24px', '24px']);
      expect(offset(third)).toEqual(['48px', '48px']);
   });

   test('a cascade that would leave the viewport wraps back to the first slot', () => {
      const { element } = createWindow({ initialize: false });
      setViewport(1000, 450);
      layOut(element);
      // Height 200 at top 200: slots 0 and 1 fit (bottom 400 and 424 <= 426), slot 2 does not.
      const windows = [element, addWindow('w2'), addWindow('w3'), addWindow('w4')];

      onLoad(document.body);

      expect(windows.map(offset)).toEqual([['0px', '0px'], ['24px', '24px'], ['0px', '0px'], ['24px', '24px']]);
   });

   test('right-to-left windows cascade toward the inline end', () => {
      const { element } = createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(element);
      const second = addWindow('second');
      element.style.direction = 'rtl';
      second.style.direction = 'rtl';

      onLoad(document.body);

      expect(offset(element)).toEqual(['0px', '0px']);
      expect(offset(second)).toEqual(['-24px', '24px']);
   });

   test('a dragged, minimized, or fullscreen window is not cascaded', () => {
      const { element } = createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(element);
      const dragged = addWindow('dragged');
      dragged.style.left = '10px';
      dragged.style.top = '10px';
      const minimized = addWindow('minimized');
      minimized.dataset.ntWindowState = 'minimized';
      const fullscreen = addWindow('fullscreen');
      fullscreen.dataset.ntWindowState = 'fullscreen';

      onLoad(document.body);

      expect(offset(dragged)).toEqual(['', '']);
      expect(offset(minimized)).toEqual(['', '']);
      expect(offset(fullscreen)).toEqual(['', '']);
   });

   test('a window already initialized is not re-cascaded by later loads', () => {
      createWindow({ initialize: false });
      setViewport(1200, 900);
      layOut(document.querySelector('[data-nt-window]'));
      const second = addWindow('second');
      onLoad(document.body);
      const placed = offset(second);

      onLoad(document.body);
      onLoad(document.body);

      expect(offset(second)).toEqual(placed);
   });

   test('a window that stops being floating clears its inline position and styles', () => {
      const { element, pageScript } = createWindow();
      element.style.inset = 'auto';
      element.style.left = '120px';
      element.style.top = '80px';
      element.style.translate = 'none';
      element.style.zIndex = '1500';
      element.style.setProperty('--nt-window-dock-offset', '56px');
      element.style.setProperty('--nt-window-cascade-offset-x', '24px');
      element.style.setProperty('--nt-window-cascade-offset-y', '24px');
      element.dataset.ntWindowDragging = 'true';
      element.removeAttribute('data-nt-window');
      pageScript.remove();

      onDispose(pageScript);

      expect(element.getAttribute('style') ?? '').toBe('');
      expect(element.dataset.ntWindowDragging).toBeUndefined();
   });

   test('a still-floating window keeps its inline styles when its page script is disposed', () => {
      const { element, pageScript } = createWindow();
      element.style.left = '120px';

      onDispose(pageScript);

      expect(element.style.left).toBe('120px');
   });

   test('the close timeout leaves a window that is no longer a window alone', () => {
      const { element, pageScript } = createWindow();
      const closeHandler = jest.fn();
      element.addEventListener('ntwindowclose', closeHandler);

      window.NTWindow.closeWindow(element);
      element.removeAttribute('data-nt-window');
      pageScript.remove();
      onDispose(pageScript);
      jest.advanceTimersByTime(200);

      expect(element.hidden).toBe(false);
      expect(closeHandler).not.toHaveBeenCalled();
   });

   test('a window that becomes floating again initializes fresh and cascades', () => {
      const { element, pageScript } = createWindow();
      setViewport(1200, 900);
      layOut(element);
      const other = addWindow('other');
      onLoad(document.body);
      expect(offset(other)).toEqual(['24px', '24px']);
      element.removeAttribute('data-nt-window');
      pageScript.remove();
      onDispose(pageScript);

      element.setAttribute('data-nt-window', 'true');
      const newPageScript = document.createElement('nt-page-script');
      element.after(newPageScript);
      onLoad(newPageScript);

      // Cleanup freed slot 0, so the returning window cascades from scratch into it.
      expect(offset(element)).toEqual(['0px', '0px']);
      element.querySelector('[data-nt-window-action="minimize"]').click();
      expect(element.dataset.ntWindowState).toBe('minimized');
   });

});
