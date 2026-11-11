import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.formatToPhone', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('formats 10 digits to (XXX) XXX-XXXX', () => {
      const input = document.createElement('input');
      input.value = '1234567890';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('(123) 456-7890');
   });

   test('formats 6 digits to (XXX) XXX', () => {
      const input = document.createElement('input');
      input.value = '123456';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('(123) 456');
   });

   test('formats 3 digits to (XXX', () => {
      const input = document.createElement('input');
      input.value = '123';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('(123');
   });

   test('formats 0 digits to empty string', () => {
      const input = document.createElement('input');
      input.value = '';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('');
   });

   test('strips non-numeric characters before formatting', () => {
      const input = document.createElement('input');
      input.value = '(123) 456-7890';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('(123) 456-7890');
   });

   test('limits to first 10 digits', () => {
      const input = document.createElement('input');
      input.value = '12345678901234';

      const event = new Event('keyup');
      Object.defineProperty(event, 'target', { value: input });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('(123) 456-7890');
   });

   test('returns early for modifier keys', () => {
      const input = document.createElement('input');
      input.value = '123';

      const event = new KeyboardEvent('keyup', { keyCode: 8 });
      Object.defineProperty(event, 'target', { value: input, configurable: true });

      NTComponents.formatToPhone(event);

      expect(input.value).toBe('123');
   });
});
