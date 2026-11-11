import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.downloadFileFromBlob', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      // Mock URL.createObjectURL and URL.revokeObjectURL
      global.URL.createObjectURL = jest.fn(() => 'blob:mock-url');
      global.URL.revokeObjectURL = jest.fn();
   });

   afterEach(() => {
      delete global.URL.createObjectURL;
      delete global.URL.revokeObjectURL;
   });

   test('creates download link and triggers download', () => {
      const blob = new Blob(['test content']);
      const createElementSpy = jest.spyOn(document, 'createElement');

      NTComponents.downloadFileFromBlob('test.txt', blob);

      expect(global.URL.createObjectURL).toHaveBeenCalledWith(blob);
      expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url');
      expect(createElementSpy).toHaveBeenCalledWith('a');

      createElementSpy.mockRestore();
   });

   test('sets download attribute with filename', () => {
      const blob = new Blob(['test']);
      const clickSpy = jest.fn();

      // Create a mock anchor element
      const mockAnchor = document.createElement('a');
      mockAnchor.click = clickSpy;

      const createElementSpy = jest.spyOn(document, 'createElement').mockReturnValue(mockAnchor);

      NTComponents.downloadFileFromBlob('myfile.pdf', blob);

      expect(mockAnchor.download).toBe('myfile.pdf');
      expect(clickSpy).toHaveBeenCalled();

      createElementSpy.mockRestore();
   });
});
