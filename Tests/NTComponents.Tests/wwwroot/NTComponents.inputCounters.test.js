import { jest } from '@jest/globals';
import { afterWebStarted, updateInputCounter } from '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents input counters', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   afterEach(() => {
      jest.restoreAllMocks();
   });

   test('updates input counters from the shared library module', () => {
      document.body.innerHTML = `
         <div class="nt-input">
            <input class="nt-input-control" maxlength="8" value="abc" />
            <span class="nt-input-counter">0/8</span>
         </div>`;

      updateInputCounter(document.querySelector('.nt-input-control'));

      const counter = document.querySelector('.nt-input-counter');
      expect(counter.textContent).toBe('3/8');
      expect(counter.getAttribute('aria-label')).toBe('Character count 3/8');
   });

   test('shared window function updates the counter from the native input handler', () => {
      document.body.innerHTML = `
         <div class="nt-input">
            <input class="nt-input-control" maxlength="8" value="" />
            <span class="nt-input-counter">0/8</span>
         </div>`;
      afterWebStarted({ addEventListener: jest.fn() });

      const input = document.querySelector('.nt-input-control');
      input.value = 'abcdef';
      window.NTComponents.updateInputCounter(input);

      expect(document.querySelector('.nt-input-counter').textContent).toBe('6/8');
   });

   test('startup does not scan input counters or register delegated counter handlers', () => {
      const addEventListener = jest.spyOn(document, 'addEventListener');
      const querySelectorAll = jest.spyOn(document, 'querySelectorAll');

      afterWebStarted({ addEventListener: jest.fn() });

      expect(addEventListener).not.toHaveBeenCalledWith('change', expect.any(Function));
      expect(querySelectorAll).not.toHaveBeenCalledWith('.nt-input-counter');
      expect(window.NTComponents.updateInputCounters).toBeUndefined();
   });

   test('shared window functions expose textarea autosize bridge', () => {
      afterWebStarted({ addEventListener: jest.fn() });

      expect(typeof window.NTComponents.autoGrowTextArea).toBe('function');
      expect(window.NTComponents.updateInputCounter).toBe(updateInputCounter);
   });
});
