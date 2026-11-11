import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.downloadFromUrl', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('fetches blob from URL and calls downloadFileFromBlob', async () => {
      global.fetch = jest.fn().mockResolvedValue({
         blob: jest.fn().mockResolvedValue(new Blob(['test content'])),
      });

      NTComponents.downloadFileFromBlob = jest.fn();

      await NTComponents.downloadFromUrl('test.txt', 'https://example.com/file.txt');

      expect(fetch).toHaveBeenCalledWith('https://example.com/file.txt');
      expect(NTComponents.downloadFileFromBlob).toHaveBeenCalled();
   });
});
