import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.formatToCurrency', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('formats numeric input to currency', () => {
      const input = document.createElement('input');
      input.value = '1234.5';
      input.setAttribute('cultureCode', 'en-US');
      input.setAttribute('currencyCode', 'USD');

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToCurrency(event);

      expect(input.value).toBe('$1,234.5');
   });

   test('uses default culture code if not provided', () => {
      const input = document.createElement('input');
      input.value = '100';
      input.setAttribute('currencyCode', 'EUR');

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToCurrency(event);

      expect(input.value).toBe('€100');
   });

   test('uses default currency code if not provided', () => {
      const input = document.createElement('input');
      input.value = '100';
      input.setAttribute('cultureCode', 'en-US');

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToCurrency(event);

      expect(input.value).toBe('$100');
   });

   test('returns early for modifier keys', () => {
      const input = document.createElement('input');
      input.value = '100';

      const event = new KeyboardEvent('keyup', { keyCode: 8 });
      Object.defineProperty(event, 'target', { value: input, configurable: true });

      NTComponents.formatToCurrency(event);

      expect(input.value).toBe('100');
   });
});
