import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.getCurrentLocation', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('returns current window location href', () => {
      const result = NTComponents.getCurrentLocation();
      expect(result).toBe(window.location.href);
   });
});
