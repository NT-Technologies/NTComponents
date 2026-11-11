import { jest } from '@jest/globals';
import { prefixCurrencyInput } from '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.prefixCurrencyInput', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('prepends currency symbol without decimal formatting', () => {
      const input = document.createElement('input');
      input.value = '1';
      input.setAttribute('cultureCode', 'en-US');
      input.setAttribute('currencyCode', 'USD');
      input.setAttribute('currencyDecimalDigits', '2');
      input.setAttribute('currencyDecimalSeparator', '.');
      input.setAttribute('currencyGroupSeparator', ',');
      input.setAttribute('currencySymbol', '$');

      prefixCurrencyInput(input);

      expect(input.value).toBe('$1');
   });

   test('preserves existing typed separators and digits', () => {
      const input = document.createElement('input');
      input.value = '$12,000';
      input.setAttribute('cultureCode', 'en-US');
      input.setAttribute('currencyCode', 'USD');
      input.setAttribute('currencyDecimalDigits', '2');
      input.setAttribute('currencyDecimalSeparator', '.');
      input.setAttribute('currencyGroupSeparator', ',');
      input.setAttribute('currencySymbol', '$');

      prefixCurrencyInput(input);

      expect(input.value).toBe('$12,000');
   });

   test('limits fractional digits without padding decimals', () => {
      const input = document.createElement('input');
      input.value = '1.234';
      input.setAttribute('cultureCode', 'en-US');
      input.setAttribute('currencyCode', 'USD');
      input.setAttribute('currencyDecimalDigits', '2');
      input.setAttribute('currencyDecimalSeparator', '.');
      input.setAttribute('currencyGroupSeparator', ',');
      input.setAttribute('currencySymbol', '$');

      prefixCurrencyInput(input);

      expect(input.value).toBe('$1.23');
   });

   test('does not add decimals while typing whole dollars', () => {
      const input = document.createElement('input');
      input.value = '12000333';
      input.setAttribute('cultureCode', 'en-US');
      input.setAttribute('currencyCode', 'USD');
      input.setAttribute('currencyDecimalDigits', '2');
      input.setAttribute('currencyDecimalSeparator', '.');
      input.setAttribute('currencyGroupSeparator', ',');
      input.setAttribute('currencySymbol', '$');

      prefixCurrencyInput(input);

      expect(input.value).toBe('$12000333');
   });

   test('updates hidden form value as a decimal string', () => {
      document.body.innerHTML = `
         <span class="nt-input-control-container">
            <input class="nt-input-control" value="$12,000.34" cultureCode="en-US" currencyCode="USD" currencyDecimalDigits="2" currencyDecimalSeparator="." currencyGroupSeparator="," currencySymbol="$" />
            <input type="hidden" name="Amount" value="" />
         </span>`;
      const input = document.querySelector('.nt-input-control');

      prefixCurrencyInput(input);

      expect(document.querySelector('input[type="hidden"]').value).toBe('12000.34');
   });

   test('limits hidden form value decimals', () => {
      document.body.innerHTML = `
         <span class="nt-input-control-container">
            <input class="nt-input-control" value="1.234" cultureCode="en-US" currencyCode="USD" currencyDecimalDigits="2" currencyDecimalSeparator="." currencyGroupSeparator="," currencySymbol="$" />
            <input type="hidden" name="Amount" value="" />
         </span>`;
      const input = document.querySelector('.nt-input-control');

      prefixCurrencyInput(input);

      expect(input.value).toBe('$1.23');
      expect(document.querySelector('input[type="hidden"]').value).toBe('1.23');
   });

   test('uses culture decimal separator for hidden form value', () => {
      document.body.innerHTML = `
         <span class="nt-input-control-container">
            <input class="nt-input-control" value="€1.234,56" cultureCode="de-DE" currencyCode="EUR" currencyDecimalDigits="2" currencyDecimalSeparator="," currencyGroupSeparator="." currencySymbol="€" />
            <input type="hidden" name="Amount" value="" />
         </span>`;
      const input = document.querySelector('.nt-input-control');

      prefixCurrencyInput(input);

      expect(document.querySelector('input[type="hidden"]').value).toBe('1234.56');
   });

   test('does not prefix empty input', () => {
      const input = document.createElement('input');
      input.value = '';

      prefixCurrencyInput(input);

      expect(input.value).toBe('');
   });
});
