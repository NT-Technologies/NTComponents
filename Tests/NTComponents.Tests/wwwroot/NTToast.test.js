import { jest } from '@jest/globals';
import { addToast, clearToasts, clearToastsFromBlazor, closeToast, closeToastFromBlazor, onDispose, onLoad, queueToast, queueToastFromBlazor } from '../../../NTComponents/Toast/NTToast.razor.js';

describe('NTToast module', () => {
   let consoleErrorSpy;

   beforeEach(() => {
      jest.useFakeTimers();
      consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => { });
      document.body.innerHTML = '';
      clearToasts();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      consoleErrorSpy.mockRestore();
      jest.useRealTimers();
      document.body.innerHTML = '';
   });

   function createHost() {
      const host = document.createElement('div');
      host.className = 'nt-toast-container nt-toast-bottom-right-corner';
      host.dataset.ntToastHost = 'true';
      host.setAttribute('popover', 'manual');
      document.body.appendChild(host);
      return host;
   }

   function loadHostFromPageScript(host = createHost()) {
      const pageScript = document.createElement('tnt-page-script');
      host.after(pageScript);
      onLoad(pageScript);
      return host;
   }

   test('onLoad registers the host and exposes the static bridge', () => {
      const host = loadHostFromPageScript();

      window.NTToast.addToast({ title: 'Saved', message: 'Changes stored', variant: 'success', timeout: 0 });

      expect(host.querySelector('.nt-toast-title').textContent).toBe('Saved');
      expect(host.querySelector('.nt-toast-message').textContent).toBe('Changes stored');
      expect(host.querySelector('.nt-toast').getAttribute('role')).toBe('status');
      expect(host.querySelector('.nt-toast').classList.contains('nt-toast-success')).toBe(true);
      expect(host.querySelector('.nt-toast').classList.contains('nt-elevation-medium')).toBe(true);
   });

   test('onLoad opens host as a manual popover for top-layer rendering', () => {
      const host = createHost();
      host.showPopover = jest.fn();

      loadHostFromPageScript(host);

      expect(host.getAttribute('popover')).toBe('manual');
      expect(host.showPopover).toHaveBeenCalledTimes(1);
   });

   test('onDispose hides host popover', () => {
      const host = createHost();
      host.showPopover = jest.fn();
      host.hidePopover = jest.fn();
      const pageScript = document.createElement('tnt-page-script');
      host.after(pageScript);

      onLoad(pageScript);
      onDispose(pageScript);

      expect(host.hidePopover).toHaveBeenCalledTimes(1);
   });

   test('open dialog promotes host popover back to the foreground layer', async () => {
      const host = createHost();
      host.showPopover = jest.fn();
      host.hidePopover = jest.fn();
      loadHostFromPageScript(host);

      addToast({ title: 'Toast before dialog', timeout: 4 });
      jest.advanceTimersByTime(2000);
      const dialog = document.createElement('dialog');
      document.body.appendChild(dialog);
      dialog.setAttribute('open', '');
      await Promise.resolve();

      expect(host.parentNode).toBe(dialog);
      expect(host.hidePopover).toHaveBeenCalledTimes(1);
      expect(host.showPopover).toHaveBeenCalledTimes(2);
      expect(host.querySelector('.nt-toast-progress').style.animationDelay).toBe('-2s');

      dialog.removeAttribute('open');
      await Promise.resolve();

      expect(host.nextElementSibling.localName).toBe('tnt-page-script');
      expect(host.querySelector('.nt-toast-progress').style.animationDelay).toBe('-2s');
   });

   test('queueToast waits for a host when called before setup', () => {
      queueToast({ title: 'Queued before render', timeout: 0 });

      const host = loadHostFromPageScript();

      expect(host.querySelector('.nt-toast-title').textContent).toBe('Queued before render');
   });

   test('queueToast ignores duplicate explicit ids while pending', () => {
      const firstId = queueToast({ title: 'Original pending', id: 'pending-duplicate', timeout: 0 });
      const duplicateId = queueToast({ title: 'Duplicate pending', id: 'pending-duplicate', timeout: 0 });

      const host = loadHostFromPageScript();

      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-toast')).toHaveLength(1);
      expect(host.querySelector('.nt-toast-title').textContent).toBe('Original pending');
   });

   test('queueToast ignores duplicate explicit ids while active', () => {
      const host = loadHostFromPageScript();
      const firstId = queueToast({ title: 'Original active', id: 'active-duplicate', timeout: 0 });
      const duplicateId = queueToast({ title: 'Duplicate active', id: 'active-duplicate', timeout: 0 });

      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-toast')).toHaveLength(1);
      expect(host.querySelector('.nt-toast-title').textContent).toBe('Original active');
   });

   test('queueToast ignores duplicate explicit ids while queued', () => {
      const host = loadHostFromPageScript();
      for (let index = 0; index < 5; index++) {
         queueToast({ title: `Visible ${index}`, id: `visible-${index}`, timeout: 0 });
      }

      const firstId = queueToast({ title: 'Original queued', id: 'queued-duplicate', timeout: 0 });
      const duplicateId = queueToast({ title: 'Duplicate queued', id: 'queued-duplicate', timeout: 0 });

      closeToast(undefined, host);
      jest.advanceTimersByTime(150);

      const titles = [...host.querySelectorAll('.nt-toast-title')].map(title => title.textContent);
      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-toast')).toHaveLength(5);
      expect(titles).toContain('Original queued');
      expect(titles).not.toContain('Duplicate queued');
   });

   test('queueToast preserves pending host target until that host loads', () => {
      queueToast({ title: 'Targeted pending', host: 'target-toast-host', id: 'targeted-pending', timeout: 0 });
      const defaultHost = loadHostFromPageScript();

      const targetHost = createHost();
      targetHost.id = 'target-toast-host';
      loadHostFromPageScript(targetHost);

      expect(defaultHost.querySelector('.nt-toast')).toBeNull();
      expect(targetHost.querySelector('.nt-toast-title').textContent).toBe('Targeted pending');
   });

   test('clearToasts for one host preserves pending toasts for another host', () => {
      queueToast({ title: 'Other pending', host: 'other-toast-host', id: 'other-pending', timeout: 0 });
      const firstHost = loadHostFromPageScript();

      clearToasts(firstHost);

      const otherHost = createHost();
      otherHost.id = 'other-toast-host';
      loadHostFromPageScript(otherHost);

      expect(firstHost.querySelector('.nt-toast')).toBeNull();
      expect(otherHost.querySelector('.nt-toast-title').textContent).toBe('Other pending');
   });

   test('queueToastFromBlazor renders scalar service payload without property metadata', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueToastFromBlazor(
         'service-toast',
         'Saved',
         'Changes stored',
         'success',
         9,
         false,
         'check_circle',
         'var(--nt-color-success-container)',
         'var(--nt-color-on-success-container)',
         'var(--nt-color-success)',
         dotNetReference,
         'NotifyClosedFromJavaScript');

      const toast = host.querySelector('.nt-toast');
      expect(toast.id).toBe('service-toast');
      expect(toast.classList.contains('nt-toast-success')).toBe(true);
      expect(toast.querySelector('.nt-toast-title').textContent).toBe('Saved');
      expect(toast.querySelector('.nt-toast-message').textContent).toBe('Changes stored');
      expect(toast.querySelector('.nt-toast-close')).toBeNull();
      expect(toast.style.getPropertyValue('--nt-toast-background-color')).toBe('var(--nt-color-success-container)');
      expect(toast.style.getPropertyValue('--nt-toast-text-color')).toBe('var(--nt-color-on-success-container)');
      expect(toast.style.getPropertyValue('--nt-toast-icon-color')).toBe('var(--nt-color-success)');
      expect(toast.style.getPropertyValue('--nt-toast-timeout')).toBe('9s');
   });

   test('toasts stack up to five visible messages and queue the rest', () => {
      const host = loadHostFromPageScript();

      for (let index = 0; index < 6; index++) {
         queueToast({ title: `Toast ${index}`, timeout: 0 });
      }

      expect(host.querySelectorAll('.nt-toast')).toHaveLength(5);
      expect([...host.querySelectorAll('.nt-toast-title')].map(title => title.textContent)).toEqual(['Toast 0', 'Toast 1', 'Toast 2', 'Toast 3', 'Toast 4']);

      closeToast(undefined, host);
      jest.advanceTimersByTime(150);

      expect(host.querySelectorAll('.nt-toast')).toHaveLength(5);
      expect([...host.querySelectorAll('.nt-toast-title')].map(title => title.textContent)).toEqual(['Toast 0', 'Toast 1', 'Toast 2', 'Toast 3', 'Toast 5']);
   });

   test('timeout closes the toast and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addToast({
         title: 'Timed',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'timed-toast'
      });
      jest.advanceTimersByTime(4000);
      jest.advanceTimersByTime(150);

      expect(host.querySelector('.nt-toast')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'timed-toast');
   });

   test('close button closes the toast and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addToast({
         title: 'Closable',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'closable-toast',
         showClose: true,
         timeout: 0
      });
      host.querySelector('.nt-toast-close').click();
      jest.advanceTimersByTime(150);

      expect(host.querySelector('.nt-toast')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'closable-toast');
   });

   test('closeToastFromBlazor closes without dotnet close notification', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addToast({
         title: 'Service close',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-toast',
         timeout: 0
      });

      expect(closeToastFromBlazor('service-toast')).toBe(true);
      jest.advanceTimersByTime(150);

      expect(host.querySelector('.nt-toast')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('clearToastsFromBlazor clears matching service toasts without dotnet close notification', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addToast({
         title: 'Service active',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-active',
         timeout: 0
      });
      for (let index = 0; index < 5; index++) {
         addToast({ title: `Filler ${index}`, timeout: 0 });
      }
      addToast({
         title: 'Service queued',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-queued',
         timeout: 0
      });

      expect(clearToastsFromBlazor(['service-active', 'service-queued'])).toBe(2);
      jest.advanceTimersByTime(150);

      expect([...host.querySelectorAll('.nt-toast-title')].map(title => title.textContent)).not.toContain('Service active');
      expect([...host.querySelectorAll('.nt-toast-title')].map(title => title.textContent)).not.toContain('Service queued');
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('closeToast finds a toast by id across registered hosts', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());

      queueToast({ title: 'First host toast', host: firstHost, id: 'first-host', timeout: 0 });
      queueToast({ title: 'Second host toast', host: secondHost, id: 'second-host', timeout: 0 });

      expect(closeToast('first-host')).toBe(true);
      jest.advanceTimersByTime(150);

      expect(firstHost.querySelector('.nt-toast')).toBeNull();
      expect(secondHost.querySelector('.nt-toast-title').textContent).toBe('Second host toast');
   });

   test('clearToasts without a host clears every registered host and pending toast', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueToast({ title: 'First host toast', host: firstHost, id: 'first', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, timeout: 0 });
      queueToast({ title: 'Second host toast', host: secondHost, id: 'second', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, timeout: 0 });
      onDispose(secondHost);
      queueToast({ title: 'Pending', id: 'pending', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, timeout: 0 });

      clearToasts();

      expect(firstHost.querySelector('.nt-toast')).toBeNull();
      expect(secondHost.querySelector('.nt-toast')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'first');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'second');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending');
   });

   test('onDispose can clean up a page script after the script element is detached', () => {
      const host = createHost();
      const pageScript = document.createElement('tnt-page-script');
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };
      host.after(pageScript);
      onLoad(pageScript);

      addToast({
         title: 'Detached script toast',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'detached-script-toast'
      });

      expect(host.querySelector('.nt-toast-title').textContent).toBe('Detached script toast');

      pageScript.remove();
      onDispose(pageScript);

      expect(host.querySelector('.nt-toast')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'detached-script-toast');
      expect(jest.getTimerCount()).toBe(0);
   });

   test('pending queue is capped and dropped dotnet toasts are notified', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      for (let index = 0; index < 51; index++) {
         queueToast({
            title: `Pending ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            id: `pending-${index}`,
            timeout: 0
         });
      }

      const host = loadHostFromPageScript();

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending-0');
      expect(host.querySelector('.nt-toast-title').textContent).toBe('Pending 1');
   });

   test('host queue is capped and dropped dotnet toasts are notified', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      for (let index = 0; index < 56; index++) {
         queueToast({
            title: `Queued ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            host,
            id: `queued-${index}`,
            timeout: 0
         });
      }

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued-5');
      expect(host.querySelectorAll('.nt-toast')).toHaveLength(5);
   });

   test('error and assert variants use assertive live regions', () => {
      const host = loadHostFromPageScript();

      queueToast({ title: 'Error', variant: 'error', timeout: 0 });
      queueToast({ title: 'Assert', variant: 'assert', timeout: 0 });

      const toasts = host.querySelectorAll('.nt-toast');
      expect(toasts[0].getAttribute('role')).toBe('alert');
      expect(toasts[0].getAttribute('aria-live')).toBe('assertive');
      expect(toasts[1].getAttribute('role')).toBe('alert');
      expect(toasts[1].getAttribute('aria-live')).toBe('assertive');
   });

   test('close notification errors are reported', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('boom'))) };

      addToast({
         title: 'Close failure',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'close-failure-toast',
         timeout: 0
      });

      host.querySelector('.nt-toast-close').click();
      jest.advanceTimersByTime(150);
      await Promise.resolve();
      await Promise.resolve();

      expect(consoleErrorSpy).toHaveBeenCalledWith('Failed to notify toast close.', expect.any(Error));
   });
});
