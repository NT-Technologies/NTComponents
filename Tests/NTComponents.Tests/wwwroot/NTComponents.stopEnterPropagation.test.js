import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.stopEnterPropagation', () => {
   test('stops propagation for Enter without preventing default', () => {
      const event = {
         key: 'Enter',
         stopPropagation: jest.fn(),
         preventDefault: jest.fn()
      };

      NTComponents.stopEnterPropagation(event);

      expect(event.stopPropagation).toHaveBeenCalledTimes(1);
      expect(event.preventDefault).not.toHaveBeenCalled();
   });

   test('does nothing for non-Enter keys', () => {
      const event = {
         key: 'Escape',
         stopPropagation: jest.fn(),
         preventDefault: jest.fn()
      };

      NTComponents.stopEnterPropagation(event);

      expect(event.stopPropagation).not.toHaveBeenCalled();
      expect(event.preventDefault).not.toHaveBeenCalled();
   });
});
