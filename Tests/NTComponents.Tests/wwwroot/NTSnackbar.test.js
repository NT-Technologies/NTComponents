import { jest } from '@jest/globals';
import { addSnackbar, clearSnackbars, closeSnackbar, closeSnackbarFromBlazor, onDispose, onLoad, queueSnackbar, queueSnackbarFromBlazor } from '../../../NTComponents/Snackbar/NTSnackbar.razor.js';

describe('NTSnackbar module', () => {
   let consoleErrorSpy;

   beforeEach(() => {
      jest.useFakeTimers();
      consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => { });
      document.body.innerHTML = '';
      clearSnackbars();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      consoleErrorSpy.mockRestore();
      jest.useRealTimers();
      document.body.innerHTML = '';
   });

   function createHost() {
      const host = document.createElement('div');
      host.className = 'nt-snackbar-container nt-snackbar-bottom-center';
      host.dataset.ntSnackbarHost = 'true';
      host.setAttribute('popover', 'manual');
      document.body.appendChild(host);
      return host;
   }

   function loadHostFromPageScript(host = createHost()) {
      const pageScript = document.createElement('nt-page-script');
      host.after(pageScript);
      onLoad(pageScript);
      return host;
   }

   test('onLoad registers the host and exposes the static bridge', () => {
      const host = loadHostFromPageScript();

      window.NTSnackbar.addSnackbar('Saved');

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Saved');
      expect(host.querySelector('.nt-snackbar').getAttribute('role')).toBe('status');
      expect(host.querySelector('.nt-snackbar').classList.contains('nt-elevation-medium')).toBe(true);
      expect(host.querySelector('.nt-snackbar').style.getPropertyValue('--nt-snackbar-background-color')).toBe('');
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
      const pageScript = document.createElement('nt-page-script');
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

      addSnackbar({ message: 'Snackbar before dialog', timeout: 0 });
      const dialog = document.createElement('dialog');
      document.body.appendChild(dialog);
      dialog.setAttribute('open', '');
      await Promise.resolve();

      expect(host.parentNode).toBe(dialog);
      expect(host.hidePopover).toHaveBeenCalledTimes(1);
      expect(host.showPopover).toHaveBeenCalledTimes(2);

      dialog.removeAttribute('open');
      await Promise.resolve();

      expect(host.nextElementSibling.localName).toBe('nt-page-script');
   });

   test('queueSnackbar waits for a host when called before setup', () => {
      queueSnackbar('Queued before render');

      const host = loadHostFromPageScript();

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Queued before render');
   });

   test('queueSnackbar ignores duplicate explicit ids while pending', () => {
      const firstId = queueSnackbar({ message: 'Original pending', id: 'pending-duplicate', timeout: 0 });
      const duplicateId = queueSnackbar({ message: 'Duplicate pending', id: 'pending-duplicate', timeout: 0 });

      const host = loadHostFromPageScript();

      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Original pending');
   });

   test('queueSnackbar ignores duplicate explicit ids while active', () => {
      const host = loadHostFromPageScript();
      const firstId = queueSnackbar({ message: 'Original active', id: 'active-duplicate', timeout: 0 });
      const duplicateId = queueSnackbar({ message: 'Duplicate active', id: 'active-duplicate', timeout: 0 });

      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Original active');
   });

   test('queueSnackbar ignores duplicate explicit ids while queued', () => {
      const host = loadHostFromPageScript();
      queueSnackbar({ message: 'Visible', id: 'visible', timeout: 0 });

      const firstId = queueSnackbar({ message: 'Original queued', id: 'queued-duplicate', timeout: 0 });
      const duplicateId = queueSnackbar({ message: 'Duplicate queued', id: 'queued-duplicate', timeout: 0 });

      closeSnackbar(undefined, host);
      jest.advanceTimersByTime(200);

      expect(duplicateId).toBe(firstId);
      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Original queued');

      closeSnackbar(undefined, host);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('queueSnackbar preserves pending host target until that host loads', () => {
      queueSnackbar({ message: 'Targeted pending', host: 'target-snackbar-host', id: 'targeted-pending', timeout: 0 });
      const defaultHost = loadHostFromPageScript();

      const targetHost = createHost();
      targetHost.id = 'target-snackbar-host';
      loadHostFromPageScript(targetHost);

      expect(defaultHost.querySelector('.nt-snackbar')).toBeNull();
      expect(targetHost.querySelector('.nt-snackbar-message').textContent).toBe('Targeted pending');
   });

   test('closeSnackbarFromBlazor removes targeted pending snackbar when a default host exists', () => {
      queueSnackbar({ message: 'Targeted pending close', host: 'target-snackbar-host', id: 'targeted-pending-close', timeout: 0 });
      loadHostFromPageScript();

      expect(closeSnackbarFromBlazor('targeted-pending-close')).toBe(true);

      const targetHost = createHost();
      targetHost.id = 'target-snackbar-host';
      loadHostFromPageScript(targetHost);

      expect(targetHost.querySelector('.nt-snackbar')).toBeNull();
   });

   test('clearSnackbars for one host preserves pending snackbars for another host', () => {
      queueSnackbar({ message: 'Other pending', host: 'other-snackbar-host', id: 'other-pending', timeout: 0 });
      const firstHost = loadHostFromPageScript();

      clearSnackbars(firstHost);

      const otherHost = createHost();
      otherHost.id = 'other-snackbar-host';
      loadHostFromPageScript(otherHost);

      expect(firstHost.querySelector('.nt-snackbar')).toBeNull();
      expect(otherHost.querySelector('.nt-snackbar-message').textContent).toBe('Other pending');
   });

   test('queueSnackbarFromBlazor renders scalar service payload without property metadata', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbarFromBlazor(
         'service-snackbar',
         'Saved',
         'Undo',
         9,
         true,
         'var(--nt-color-primary)',
         'var(--nt-color-on-primary)',
         'var(--nt-color-secondary)',
         dotNetReference,
         'InvokeActionFromJavaScript',
         'NotifyClosedFromJavaScript');

      const snackbar = host.querySelector('.nt-snackbar');
      expect(snackbar.id).toBe('service-snackbar');
      expect(snackbar.querySelector('.nt-snackbar-message').textContent).toBe('Saved');
      expect(snackbar.querySelector('.nt-snackbar-action').textContent).toBe('Undo');
      expect(snackbar.querySelector('.nt-snackbar-close')).not.toBeNull();
      expect(snackbar.style.getPropertyValue('--nt-snackbar-background-color')).toBe('var(--nt-color-primary)');
      expect(snackbar.style.getPropertyValue('--nt-snackbar-text-color')).toBe('var(--nt-color-on-primary)');
      expect(snackbar.style.getPropertyValue('--nt-snackbar-action-color')).toBe('var(--nt-color-secondary)');
   });

   test('addSnackbar renders action and close buttons for action snackbars', async () => {
      const host = loadHostFromPageScript();
      const actionCallback = jest.fn();

      addSnackbar({ message: 'Email archived', actionLabel: 'Undo', actionCallback });
      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(actionCallback).toHaveBeenCalledTimes(1);
      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('addSnackbar supports multiple actions and keeps open when action returns false', async () => {
      const host = loadHostFromPageScript();
      const copyCallback = jest.fn(() => false);
      const openCallback = jest.fn(() => true);

      addSnackbar({
         message: 'Draft saved',
         actions: [
            { label: 'Copy', actionCallback: copyCallback },
            { label: 'Open', actionCallback: openCallback }
         ],
         timeout: 0
      });

      const actions = host.querySelectorAll('.nt-snackbar-action');
      expect(actions).toHaveLength(2);
      expect(actions[0].textContent).toBe('Copy');
      expect(actions[1].textContent).toBe('Open');

      actions[0].click();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(copyCallback).toHaveBeenCalledTimes(1);
      expect(openCallback).not.toHaveBeenCalled();
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Draft saved');
      expect(actions[0].disabled).toBe(false);
      expect(actions[1].disabled).toBe(false);

      actions[1].click();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(openCallback).toHaveBeenCalledTimes(1);
      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('queued snackbars render one at a time', () => {
      const host = loadHostFromPageScript();

      queueSnackbar('First', { timeout: 0 });
      queueSnackbar('Second', { timeout: 0 });

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('First');

      closeSnackbar();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Second');
   });

   test('queued snackbar waits for the closing snackbar to leave the DOM', () => {
      const host = loadHostFromPageScript();

      queueSnackbar('First', { timeout: 0 });
      closeSnackbar();
      queueSnackbar('Second', { timeout: 0 });

      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('First');
      expect(host.querySelector('.nt-snackbar').classList.contains('nt-closing')).toBe(true);

      jest.advanceTimersByTime(200);

      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Second');
   });

   test('dotnet action callback closes only after action succeeds and notifies close', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Email archived',
         actionLabel: 'Undo',
         dotNetActionMethod: 'InvokeActionFromJavaScript',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('InvokeActionFromJavaScript', 'service-snackbar', 0);
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('dotnet action returning false keeps snackbar visible', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve(false)) };

      addSnackbar({
         message: 'Draft saved',
         actionLabel: 'Copy',
         dotNetActionMethod: 'InvokeActionFromJavaScript',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('InvokeActionFromJavaScript', 'service-snackbar', 0);
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Draft saved');
      expect(host.querySelector('.nt-snackbar-action').disabled).toBe(false);
   });

   test('dotnet action rejection keeps snackbar visible', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('boom'))) };

      addSnackbar({
         message: 'Email archived',
         actionLabel: 'Undo',
         dotNetActionMethod: 'InvokeActionFromJavaScript',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);
      await Promise.resolve();

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Email archived');
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
      expect(consoleErrorSpy).toHaveBeenCalledWith('Snackbar action failed.', expect.any(Error));
   });

   test('action clicks are ignored while an async action is in progress', async () => {
      const host = loadHostFromPageScript();
      let resolveAction;
      const actionCallback = jest.fn(() => new Promise(resolve => {
         resolveAction = resolve;
      }));

      addSnackbar({ message: 'Email archived', actionLabel: 'Undo', actionCallback, timeout: 0 });
      const action = host.querySelector('.nt-snackbar-action');

      action.click();
      action.click();
      expect(actionCallback).toHaveBeenCalledTimes(1);
      expect(action.disabled).toBe(true);

      resolveAction();
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('closeSnackbarFromBlazor closes without dotnet close notification', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Service close',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      expect(closeSnackbarFromBlazor('service-snackbar')).toBe(true);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('closeSnackbarFromBlazor removes matching pending snackbar without notifying dotnet', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({
         message: 'Pending service close',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'pending-service-snackbar',
         timeout: 0
      });

      expect(closeSnackbarFromBlazor('pending-service-snackbar')).toBe(true);

      const host = loadHostFromPageScript();

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('closeSnackbar finds a snackbar by id across registered hosts', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());

      queueSnackbar({ message: 'First host snackbar', host: firstHost, id: 'first-host', timeout: 0 });
      queueSnackbar({ message: 'Second host snackbar', host: secondHost, id: 'second-host', timeout: 0 });

      expect(closeSnackbar('first-host')).toBe(true);
      jest.advanceTimersByTime(200);

      expect(firstHost.querySelector('.nt-snackbar')).toBeNull();
      expect(secondHost.querySelector('.nt-snackbar-message').textContent).toBe('Second host snackbar');
   });

   test('timeout closes the active snackbar and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Timed',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'timed-snackbar'
      });
      jest.advanceTimersByTime(4000);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'timed-snackbar');
   });

   test('close button closes the active snackbar and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Closable',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'closable-snackbar',
         showClose: true,
         timeout: 0
      });
      host.querySelector('.nt-snackbar-close').click();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'closable-snackbar');
   });

   test('clearSnackbars notifies dotnet for active and queued snackbars', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({ message: 'Active', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'active', timeout: 0 });
      addSnackbar({ message: 'Queued', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'queued', timeout: 0 });

      clearSnackbars();

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'active');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued');
   });

   test('clearSnackbars without a host clears every registered host', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());

      queueSnackbar({ message: 'First host snackbar', host: firstHost, timeout: 0 });
      queueSnackbar({ message: 'Second host snackbar', host: secondHost, timeout: 0 });

      clearSnackbars();

      expect(firstHost.querySelector('.nt-snackbar')).toBeNull();
      expect(secondHost.querySelector('.nt-snackbar')).toBeNull();
   });

   test('clearSnackbars notifies dotnet for pending snackbars', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({ message: 'Pending', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'pending', timeout: 0 });

      clearSnackbars();

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending');
   });

   test('pending queue is capped and dropped dotnet snackbars are notified', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      for (let index = 0; index < 51; index++) {
         queueSnackbar({
            message: `Pending ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            id: `pending-${index}`,
            timeout: 0
         });
      }

      const host = loadHostFromPageScript();

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending-0');
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Pending 1');
   });

   test('host queue is capped and dropped dotnet snackbars are notified', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({ message: 'Active', host, timeout: 0 });
      for (let index = 0; index < 51; index++) {
         queueSnackbar({
            message: `Queued ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            host,
            id: `queued-${index}`,
            timeout: 0
         });
      }

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued-0');

      closeSnackbar(undefined, host);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Queued 1');
   });

   test('onDispose clears host state and notifies active and queued JavaScript snackbars', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({ message: 'Visible', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'visible', timeout: 0 });
      addSnackbar({ message: 'Queued', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'queued', timeout: 0 });

      onDispose(host);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(closeSnackbar()).toBe(false);
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'visible');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued');
   });
});
