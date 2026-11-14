import { jest } from '@jest/globals';
import { onLoad, onUpdate, setOpen } from '../../../NTComponents/Buttons/NTFabMenu.razor.js';

function createDotNetRef() {
   return {
      invokeMethodAsync: jest.fn(() => Promise.resolve())
   };
}

function createFabMenu({ expanded = false } = {}) {
   onLoad(null, null);

   const host = document.createElement('nt-fab-menu');
   if (expanded) {
      host.classList.add('nt-fab-menu-expanded');
   }

   const button = document.createElement('button');
   button.className = 'nt-fab-menu-button';
   button.setAttribute('aria-expanded', expanded ? 'true' : 'false');

   const panel = document.createElement('div');
   panel.className = 'nt-fab-menu-panel';
   panel.dataset.closeOnItemClick = 'true';

   const item = document.createElement('button');
   item.className = 'nt-fab-menu-item';
   panel.append(item);

   let isOpen = expanded;
   panel.matches = jest.fn((selector) => selector === ':popover-open' ? isOpen : false);
   panel.showPopover = jest.fn(() => {
      if (!isOpen) {
         isOpen = true;
         panel.dispatchEvent(new Event('toggle'));
      }
   });
   panel.hidePopover = jest.fn(() => {
      if (isOpen) {
         isOpen = false;
         panel.dispatchEvent(new Event('toggle'));
      }
   });

   host.append(button, panel);
   document.body.appendChild(host);

   return { host, button, panel, item };
}

describe('NTFabMenu custom element', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      delete window.NTComponents;
      jest.clearAllMocks();
   });

   test('native popover toggle notifies dotnet state changes', () => {
      const { host, button, panel } = createFabMenu();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);

      panel.showPopover();

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyFabMenuExpandedChanged', true);
      expect(host.classList.contains('nt-fab-menu-expanded')).toBe(true);
      expect(button.getAttribute('aria-expanded')).toBe('true');
   });

   test('rendered expanded class does not open the menu', () => {
      const { host, button, panel } = createFabMenu();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);

      host.classList.add('nt-fab-menu-expanded');
      button.setAttribute('aria-expanded', 'true');
      onUpdate(host, dotNetRef);

      expect(panel.showPopover).not.toHaveBeenCalled();
      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
      expect(host.classList.contains('nt-fab-menu-expanded')).toBe(false);
      expect(button.getAttribute('aria-expanded')).toBe('false');
   });

   test('update before a pending toggle event still notifies dotnet of the close', () => {
      const { host, panel } = createFabMenu();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);
      panel.showPopover();
      dotNetRef.invokeMethodAsync.mockClear();
      panel.matches = jest.fn(() => false);

      onUpdate(host, dotNetRef);
      panel.dispatchEvent(new Event('toggle'));

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyFabMenuExpandedChanged', false);
   });

   test('update restores the expanded class a re-render removed from an open menu', () => {
      const { host, panel } = createFabMenu();
      onLoad(host, createDotNetRef());
      panel.showPopover();

      host.classList.remove('nt-fab-menu-expanded');
      onUpdate(host, createDotNetRef());

      expect(host.classList.contains('nt-fab-menu-expanded')).toBe(true);
   });

   test('setOpen opens through the native popover and reports it to dotnet', () => {
      const { host, panel } = createFabMenu();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);

      setOpen(host, true);

      expect(panel.showPopover).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyFabMenuExpandedChanged', true);
   });

   test('setOpen closes an open menu through the native popover and reports it to dotnet', () => {
      const { host, panel } = createFabMenu();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);
      panel.showPopover();
      dotNetRef.invokeMethodAsync.mockClear();

      setOpen(host, false);

      expect(panel.hidePopover).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyFabMenuExpandedChanged', false);
   });

   test('setOpen to the current state does nothing', () => {
      const { host, panel } = createFabMenu();
      onLoad(host, createDotNetRef());

      setOpen(host, false);

      expect(panel.hidePopover).not.toHaveBeenCalled();
   });

   test('enabled item click closes the popover', () => {
      const { host, item, panel } = createFabMenu({ expanded: true });
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);
      dotNetRef.invokeMethodAsync.mockClear();

      item.click();

      expect(panel.hidePopover).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyFabMenuExpandedChanged', false);
   });

   test('disabled item click does not close the popover through component logic', () => {
      const { host, item, panel } = createFabMenu({ expanded: true });
      item.classList.add('nt-fab-menu-item-disabled');
      onLoad(host, createDotNetRef());

      item.click();

      expect(panel.hidePopover).not.toHaveBeenCalled();
   });

   test('unchanged updates do not re-register button interactions', () => {
      const registerButtonInteraction = jest.fn();
      window.NTComponents = { registerButtonInteraction };
      const { host, button, item } = createFabMenu();

      expect(registerButtonInteraction).toHaveBeenCalledWith(button);
      expect(registerButtonInteraction).toHaveBeenCalledWith(item);
      expect(registerButtonInteraction).toHaveBeenCalledTimes(2);

      onLoad(host, createDotNetRef());
      onUpdate(host, createDotNetRef());

      expect(registerButtonInteraction).toHaveBeenCalledTimes(2);
   });

   test('interaction registration retries when the global hook becomes available later', () => {
      const { host, button, item } = createFabMenu();
      const registerButtonInteraction = jest.fn();
      window.NTComponents = { registerButtonInteraction };

      onLoad(host, createDotNetRef());

      expect(registerButtonInteraction).toHaveBeenCalledWith(button);
      expect(registerButtonInteraction).toHaveBeenCalledWith(item);
      expect(registerButtonInteraction).toHaveBeenCalledTimes(2);
   });

   test('update scans component DOM once', () => {
      const { host } = createFabMenu();
      const querySelector = jest.spyOn(host, 'querySelector');
      const querySelectorAll = jest.spyOn(host, 'querySelectorAll');

      onUpdate(host, createDotNetRef());

      expect(querySelector).toHaveBeenCalledTimes(2);
      expect(querySelectorAll).toHaveBeenCalledTimes(1);
   });

   test('item changes do not rebind delegated panel listeners', () => {
      const { host, panel } = createFabMenu();
      const addEventListener = jest.spyOn(panel, 'addEventListener');
      const removeEventListener = jest.spyOn(panel, 'removeEventListener');
      const item = document.createElement('button');
      item.className = 'nt-fab-menu-item';
      panel.append(item);

      onUpdate(host, createDotNetRef());

      expect(addEventListener).not.toHaveBeenCalled();
      expect(removeEventListener).not.toHaveBeenCalled();
   });

   test('unchanged open update does not move focus out of the menu', () => {
      const { host, button, item } = createFabMenu({ expanded: true });
      onLoad(host, createDotNetRef());
      item.focus();

      host.update(createDotNetRef());

      expect(document.activeElement).toBe(item);
      expect(document.activeElement).not.toBe(button);
   });
});
