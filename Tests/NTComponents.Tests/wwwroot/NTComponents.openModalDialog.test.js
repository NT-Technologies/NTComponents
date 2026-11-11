import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.openModalDialog', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('shows modal dialog by ID', () => {
      const dialog = document.createElement('dialog');
      dialog.id = 'test-dialog';
      dialog.showModal = jest.fn();
      document.body.appendChild(dialog);

      NTComponents.openModalDialog('test-dialog');
      expect(dialog.showModal).toHaveBeenCalled();
   });

   test('does nothing if dialog does not exist', () => {
      expect(() => NTComponents.openModalDialog('non-existent')).not.toThrow();
   });

   test('prevents default cancel event', () => {
      const dialog = document.createElement('dialog');
      dialog.id = 'test-dialog';
      dialog.showModal = jest.fn();
      document.body.appendChild(dialog);

      NTComponents.openModalDialog('test-dialog');

      const cancelEvent = new Event('cancel', { cancelable: true });
      const preventSpy = jest.spyOn(cancelEvent, 'preventDefault');
      dialog.dispatchEvent(cancelEvent);
      expect(preventSpy).toHaveBeenCalled();
   });

   test('stops cancel event propagation', () => {
      const dialog = document.createElement('dialog');
      dialog.id = 'test-dialog';
      dialog.showModal = jest.fn();
      document.body.appendChild(dialog);

      NTComponents.openModalDialog('test-dialog');

      const cancelEvent = new Event('cancel', { bubbles: true, cancelable: true });
      const stopSpy = jest.spyOn(cancelEvent, 'stopPropagation');
      dialog.dispatchEvent(cancelEvent);
      expect(stopSpy).toHaveBeenCalled();
   });
});
