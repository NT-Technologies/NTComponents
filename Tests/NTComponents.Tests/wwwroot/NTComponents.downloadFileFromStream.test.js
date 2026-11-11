import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.downloadFileFromStream', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('calls downloadFileFromBlob with correct parameters', async () => {
      const mockStreamRef = {
         arrayBuffer: jest.fn().mockResolvedValue(new ArrayBuffer(10)),
      };

      NTComponents.downloadFileFromBlob = jest.fn();

      await NTComponents.downloadFileFromStream('test.txt', mockStreamRef);

      expect(NTComponents.downloadFileFromBlob).toHaveBeenCalled();
      const [fileName, blob] = NTComponents.downloadFileFromBlob.mock.calls[0];
      expect(fileName).toBe('test.txt');
      expect(blob instanceof Blob).toBe(true);
   });
});
