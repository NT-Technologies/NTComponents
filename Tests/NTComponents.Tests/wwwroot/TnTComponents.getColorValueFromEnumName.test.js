import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.getColorValueFromEnumName', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      document.documentElement.style.cssText = '';
   });

   test('returns null for null or non-string input', () => {
      expect(NTComponents.getColorValueFromEnumName(null)).toBeNull();
      expect(NTComponents.getColorValueFromEnumName(undefined)).toBeNull();
      expect(NTComponents.getColorValueFromEnumName(123)).toBeNull();
   });

   test('converts PascalCase color name to kebab-case CSS variable', () => {
      document.documentElement.style.setProperty('--nt-color-primary', '#ff0000');
      const result = NTComponents.getColorValueFromEnumName('Primary');
      expect(result).toBe('#ff0000');
   });

   test('converts complex camelCase color names correctly', () => {
      document.documentElement.style.setProperty('--nt-color-on-primary-container', '#00ff00');
      const result = NTComponents.getColorValueFromEnumName('OnPrimaryContainer');
      expect(result).toBe('#00ff00');
   });

   test('returns null for non-existent color variable', () => {
      const result = NTComponents.getColorValueFromEnumName('NonExistentColor');
      expect(result).toBeNull();
   });

   test('converts rgb format to hex', () => {
      document.documentElement.style.setProperty('--nt-color-test', 'rgb(255, 0, 0)');
      const result = NTComponents.getColorValueFromEnumName('Test');
      expect(result).toBe('#ff0000');
   });

   test('converts rgb with spaces to hex', () => {
      document.documentElement.style.setProperty('--nt-color-test', 'rgb(0 255 0)');
      const result = NTComponents.getColorValueFromEnumName('Test');
      expect(result).toBe('#00ff00');
   });

   test('converts rgba format to hex (ignoring alpha)', () => {
      document.documentElement.style.setProperty('--nt-color-test', 'rgba(0, 0, 255, 0.5)');
      const result = NTComponents.getColorValueFromEnumName('Test');
      expect(result).toBe('#0000ff');
   });

   test('returns original value if not rgb/rgba format', () => {
      document.documentElement.style.setProperty('--nt-color-test', '#abcdef');
      const result = NTComponents.getColorValueFromEnumName('Test');
      expect(result).toBe('#abcdef');
   });
});
