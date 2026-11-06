import { jest } from '@jest/globals';
import { onLoad } from '../../../NTComponents/Menus/NTContextMenu.razor.js';

function rect(values) {
   return {
      bottom: values.bottom,
      height: values.height,
      left: values.left,
      right: values.right,
      top: values.top,
      width: values.width,
      x: values.left,
      y: values.top,
      toJSON: () => ({})
   };
}

function pointerEvent(type, values) {
   const event = new Event(type, { bubbles: true, cancelable: true });
   Object.defineProperty(event, 'clientX', { value: values.clientX });
   Object.defineProperty(event, 'clientY', { value: values.clientY });
   Object.defineProperty(event, 'isPrimary', { value: values.isPrimary ?? true });
   Object.defineProperty(event, 'pointerType', { value: values.pointerType ?? 'touch' });
   return event;
}

function createContextMenu({ disabled = false, longPressDelay = 500, menuId = 'menu' } = {}) {
   onLoad(null);

   const contextMenu = document.createElement('nt-context-menu');
   contextMenu.dataset.disabled = disabled ? 'true' : 'false';
   contextMenu.dataset.longPressDelay = String(longPressDelay);
   contextMenu.dataset.menuId = menuId;

   const target = document.createElement('span');
   target.className = 'nt-context-menu-target';
   target.tabIndex = 0;
   target.getBoundingClientRect = jest.fn(() => rect({
      bottom: 130,
      height: 30,
      left: 40,
      right: 200,
      top: 100,
      width: 160
   }));

   const menu = document.createElement('nt-menu');
   menu.id = menuId;
   menu.openAt = jest.fn();
   menu.hidePopover = jest.fn();
   menu.matches = jest.fn(() => false);

   contextMenu.append(target, menu);
   document.body.append(contextMenu);
   onLoad(contextMenu);

   return { contextMenu, menu, target };
}

function createNestedContextMenu() {
   onLoad(null);

   const parent = document.createElement('nt-context-menu');
   parent.dataset.disabled = 'false';
   parent.dataset.longPressDelay = '250';
   parent.dataset.menuId = 'parent-menu';
   const parentTarget = document.createElement('span');
   parentTarget.className = 'nt-context-menu-target';
   const parentMenu = document.createElement('nt-menu');
   parentMenu.id = 'parent-menu';
   parentMenu.openAt = jest.fn();

   const child = document.createElement('nt-context-menu');
   child.dataset.disabled = 'false';
   child.dataset.longPressDelay = '250';
   child.dataset.menuId = 'child-menu';
   const childTarget = document.createElement('span');
   childTarget.className = 'nt-context-menu-target';
   childTarget.tabIndex = 0;
   childTarget.getBoundingClientRect = jest.fn(() => rect({
      bottom: 84,
      height: 24,
      left: 20,
      right: 120,
      top: 60,
      width: 100
   }));
   const childMenu = document.createElement('nt-menu');
   childMenu.id = 'child-menu';
   childMenu.openAt = jest.fn();

   child.append(childTarget, childMenu);
   parentTarget.append(child);
   parent.append(parentTarget, parentMenu);
   document.body.append(parent);
   onLoad(parent);
   onLoad(child);

   return { childMenu, childTarget, parentMenu, parentTarget };
}

describe('NTContextMenu custom element', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      jest.useRealTimers();
   });

   test('right-click prevents the native menu and opens at pointer coordinates', () => {
      const { menu, target } = createContextMenu();
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 84,
         clientY: 126
      });

      target.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(true);
      expect(menu.openAt).toHaveBeenCalledWith(84, 126, target);
   });

   test('keyboard context menu key opens at the target lower-left corner', () => {
      const { menu, target } = createContextMenu();

      target.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'ContextMenu'
      }));

      expect(menu.openAt).toHaveBeenCalledWith(40, 130, target);
   });

   test('target selector opens only for matching descendants and leaves other right-clicks native', () => {
      const { contextMenu, menu, target } = createContextMenu();
      contextMenu.dataset.targetSelector = 'td';
      target.innerHTML = '<p>Plain</p><table><tbody><tr><td><b>Cell</b></td></tr></tbody></table>';
      const cellText = target.querySelector('b');

      const plain = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
      target.querySelector('p').dispatchEvent(plain);
      expect(plain.defaultPrevented).toBe(false);
      expect(menu.openAt).not.toHaveBeenCalled();

      const direct = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
      target.dispatchEvent(direct);
      expect(direct.defaultPrevented).toBe(false);
      expect(menu.openAt).not.toHaveBeenCalled();

      const matching = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 5, clientY: 6 });
      cellText.dispatchEvent(matching);
      expect(matching.defaultPrevented).toBe(true);
      expect(menu.openAt).toHaveBeenCalledWith(5, 6, target.querySelector('td'));
   });

   test('target selector keyboard invocation requires the selection inside a matching element', () => {
      const { contextMenu, menu, target } = createContextMenu();
      contextMenu.dataset.targetSelector = 'td';
      target.innerHTML = '<p>Plain</p><table><tbody><tr><td>Cell</td></tr></tbody></table>';
      const cell = target.querySelector('td');
      cell.getBoundingClientRect = jest.fn(() => rect({ bottom: 70, height: 20, left: 12, right: 60, top: 50, width: 48 }));
      const select = (node) => {
         const range = document.createRange();
         range.selectNodeContents(node);
         range.collapse(true);
         window.getSelection().removeAllRanges();
         window.getSelection().addRange(range);
      };

      select(target.querySelector('p'));
      const outside = new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true, cancelable: true });
      target.dispatchEvent(outside);
      expect(outside.defaultPrevented).toBe(false);
      expect(menu.openAt).not.toHaveBeenCalled();

      select(cell);
      const inside = new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true, cancelable: true });
      target.dispatchEvent(inside);
      expect(inside.defaultPrevented).toBe(true);
      expect(menu.openAt).toHaveBeenCalledWith(12, 70, cell);
   });

   test('outside click dismisses the menu while clicks inside its content do not', () => {
      const { menu, target } = createContextMenu();
      target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));

      const contentButton = document.createElement('button');
      menu.append(contentButton);
      contentButton.click();
      expect(menu.hidePopover).not.toHaveBeenCalled();

      document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      expect(menu.hidePopover).toHaveBeenCalledTimes(1);
   });

   test('opening another context menu dismisses the previous one', () => {
      const first = createContextMenu({ menuId: 'first-menu' });
      const second = createContextMenu({ menuId: 'second-menu' });
      first.target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      expect(first.menu.hidePopover).not.toHaveBeenCalled();

      second.target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      expect(first.menu.hidePopover).toHaveBeenCalledTimes(1);
      expect(second.menu.hidePopover).not.toHaveBeenCalled();
   });

   test('Escape dismisses an open menu even after focus leaves its content', () => {
      const { menu, target } = createContextMenu();
      target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      menu.matches.mockReturnValue(true);

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
      expect(menu.hidePopover).not.toHaveBeenCalled();
      const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
      document.dispatchEvent(escape);
      expect(menu.hidePopover).toHaveBeenCalledTimes(1);
      expect(escape.defaultPrevented).toBe(true);
   });

   test('closing or disconnecting the menu removes its document dismissal listeners', () => {
      const { contextMenu, menu, target } = createContextMenu();
      target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      document.body.click();
      expect(menu.hidePopover).toHaveBeenCalledTimes(1);
      menu.hidePopover.mockClear();

      target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      menu.dispatchEvent(new Event('toggle'));
      document.body.click();
      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      expect(menu.hidePopover).not.toHaveBeenCalled();

      target.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
      contextMenu.remove();
      document.body.click();
      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      expect(menu.hidePopover).not.toHaveBeenCalled();
   });

   test('shift f10 opens at the focused target lower-left corner', () => {
      const { menu, target } = createContextMenu();

      target.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'F10',
         shiftKey: true
      }));

      expect(menu.openAt).toHaveBeenCalledWith(40, 130, target);
   });

   test('touch long-press opens after the configured delay', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(249);
      expect(menu.openAt).not.toHaveBeenCalled();

      jest.advanceTimersByTime(1);

      expect(menu.openAt).toHaveBeenCalledWith(64, 92, target);
   });

   test('touch long-press suppresses follow-up native context menu and click', () => {
      jest.useFakeTimers();
      const { target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(250);

      const contextMenuEvent = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      target.dispatchEvent(contextMenuEvent);
      target.dispatchEvent(clickEvent);

      expect(contextMenuEvent.defaultPrevented).toBe(true);
      expect(clickEvent.defaultPrevented).toBe(true);
   });

   test('touch long-press cancels on movement beyond tolerance', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      document.dispatchEvent(pointerEvent('pointermove', { clientX: 73, clientY: 92 }));
      jest.advanceTimersByTime(250);

      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('touch long-press cancels on pointer up', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      document.dispatchEvent(pointerEvent('pointerup', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(250);

      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('disabled context menu does not prevent native context menu or open', () => {
      const { menu, target } = createContextMenu({ disabled: true });
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 84,
         clientY: 126
      });

      target.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(false);
      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('nested child context menu does not also open the parent on right-click', () => {
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 42,
         clientY: 76
      });

      childTarget.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(true);
      expect(childMenu.openAt).toHaveBeenCalledWith(42, 76, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });

   test('nested child context menu does not also open the parent from keyboard', () => {
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();

      childTarget.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'ContextMenu'
      }));

      expect(childMenu.openAt).toHaveBeenCalledWith(20, 84, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });

   test('nested child long-press does not start the parent long-press menu', () => {
      jest.useFakeTimers();
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();

      childTarget.dispatchEvent(pointerEvent('pointerdown', { clientX: 42, clientY: 76 }));
      jest.advanceTimersByTime(250);

      expect(childMenu.openAt).toHaveBeenCalledWith(42, 76, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });

   describe('external target', () => {
      function createExternal(targetId = 'external-a') {
         const { contextMenu, menu } = createContextMenu();
         contextMenu.querySelector('.nt-context-menu-target').remove();
         const targets = ['external-a', 'external-b'].map(id => {
            const element = document.createElement('button');
            element.id = id;
            element.getBoundingClientRect = jest.fn(() => rect({ bottom: 50, height: 20, left: 10, right: 60, top: 30, width: 50 }));
            document.body.append(element);
            return element;
         });
         contextMenu.dataset.targetId = targetId;
         return { contextMenu, menu, a: targets[0], b: targets[1] };
      }

      test('opens on contextmenu and Shift+F10 from an element outside the component', () => {
         const { menu, a } = createExternal();

         a.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 15, clientY: 35 }));
         expect(menu.openAt).toHaveBeenCalledWith(15, 35, a);

         a.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'F10', shiftKey: true }));
         expect(menu.openAt).toHaveBeenLastCalledWith(10, 50, a);
      });

      test('changing data-target-id moves the listeners to the new element', () => {
         const { contextMenu, menu, a, b } = createExternal();

         contextMenu.dataset.targetId = 'external-b';
         a.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
         expect(menu.openAt).not.toHaveBeenCalled();

         b.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
         expect(menu.openAt).toHaveBeenCalledTimes(1);
      });

      test('a missing target element results in no target and does not throw', () => {
         const { contextMenu, menu, a } = createExternal();

         expect(() => { contextMenu.dataset.targetId = 'missing'; }).not.toThrow();
         a.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
         expect(menu.openAt).not.toHaveBeenCalled();
      });
   });
});
