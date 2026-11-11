import { applyPhoneMaskInput, formatPhoneValue, normalizePhoneValue } from '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents phone masks', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
   });

   test('formats default US phone mask', () => {
      expect(formatPhoneValue('1234567890')).toBe('(123) 456-7890');
   });

   test('formats partial default phone mask without trailing literals', () => {
      expect(formatPhoneValue('123')).toBe('(123');
      expect(formatPhoneValue('1234')).toBe('(123) 4');
   });

   test('formats custom country mask', () => {
      expect(formatPhoneValue('441234567890', '+## #### ######')).toBe('+44 1234 567890');
   });

   test('limits digits to mask placeholders and strips existing literals', () => {
      expect(formatPhoneValue('+44 1234 567890 999', '+## #### ######')).toBe('+44 1234 567890');
   });

   test('applies mask from input attribute', () => {
      const input = document.createElement('input');
      input.setAttribute('phoneMask', '+## #### ######');
      input.value = '441234567890';

      applyPhoneMaskInput(input);

      expect(input.value).toBe('+44 1234 567890');
   });

   test('normalizes default phone value to digits only', () => {
      expect(normalizePhoneValue('(123) 456-7890')).toBe('1234567890');
   });

   test('normalizes country mask to country code space and digits only', () => {
      expect(normalizePhoneValue('+44 1234 567890', '+## #### ######')).toBe('44 1234567890');
   });

   test('syncs hidden form value to normalized phone value', () => {
      document.body.innerHTML = `
         <div class="nt-input-control-container">
            <input type="tel" class="nt-input-control" phoneMask="+## #### ######" value="441234567890" />
            <input type="hidden" name="Phone" value="" />
         </div>`;

      const input = document.querySelector('input[type="tel"]');
      applyPhoneMaskInput(input);

      expect(input.value).toBe('+44 1234 567890');
      expect(document.querySelector('input[type="hidden"]').value).toBe('44 1234567890');
   });

   test('does not change disabled or readonly inputs', () => {
      const disabledInput = document.createElement('input');
      disabledInput.disabled = true;
      disabledInput.value = '1234567890';

      const readonlyInput = document.createElement('input');
      readonlyInput.readOnly = true;
      readonlyInput.value = '1234567890';

      applyPhoneMaskInput(disabledInput);
      applyPhoneMaskInput(readonlyInput);

      expect(disabledInput.value).toBe('1234567890');
      expect(readonlyInput.value).toBe('1234567890');
   });
});
