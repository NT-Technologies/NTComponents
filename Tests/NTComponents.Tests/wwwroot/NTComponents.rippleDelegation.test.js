import { jest } from '@jest/globals';
import { afterWebStarted } from '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents delegated ripple', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      jest.useFakeTimers();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      jest.useRealTimers();
   });

   test('creates and releases ripple nodes for nt-button ripple hosts', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button nt-ripple';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 100 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 40 });
      button.getBoundingClientRect = jest.fn(() => ({
         top: 10,
         left: 20,
         right: 120,
         bottom: 50,
         width: 100,
         height: 40
      }));

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      const pointerDown = new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      });

      button.dispatchEvent(pointerDown);
      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('40px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('20px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('200px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('80px');

      const pointerUp = new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      });

      button.dispatchEvent(pointerUp);
      jest.advanceTimersByTime(600);

      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('creates a centered ripple for keyboard activation on registered hosts', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button nt-ripple';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 120 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 48 });

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      global.NTComponents.registerRippleHost(host);

      button.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('60px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('24px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('240px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('96px');

      button.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'Enter' }));
      jest.advanceTimersByTime(600);

      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('uses navigation rail item body as the nested ripple boundary', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const item = document.createElement('a');
      item.className = 'nt-navigation-rail-item';

      const body = document.createElement('span');
      body.className = 'nt-navigation-rail-item-body';
      Object.defineProperty(body, 'offsetWidth', { configurable: true, value: 96 });
      Object.defineProperty(body, 'offsetHeight', { configurable: true, value: 64 });
      body.getBoundingClientRect = jest.fn(() => ({
         top: 8,
         left: 8,
         right: 104,
         bottom: 72,
         width: 96,
         height: 64
      }));

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host nt-navigation-rail-item-ripple';
      body.appendChild(host);

      const content = document.createElement('span');
      content.className = 'nt-navigation-rail-item-content';
      Object.defineProperty(content, 'offsetWidth', { configurable: true, value: 56 });
      Object.defineProperty(content, 'offsetHeight', { configurable: true, value: 52 });
      content.getBoundingClientRect = jest.fn(() => ({
         top: 12,
         left: 28,
         right: 84,
         bottom: 64,
         width: 56,
         height: 52
      }));

      body.appendChild(content);
      item.appendChild(body);
      document.body.appendChild(item);

      global.NTComponents.registerButtonInteraction(item);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      item.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 48,
         clientY: 42,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('40px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('34px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('192px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('128px');
   });

   test('registerRippleHost keeps pressed shape until mouseleave', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 100 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 40 });
      button.getBoundingClientRect = jest.fn(() => ({
         top: 10,
         left: 20,
         right: 120,
         bottom: 50,
         width: 100,
         height: 40
      }));

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      global.NTComponents.registerRippleHost(host);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('40px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('20px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('200px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('80px');

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerleave' : 'mouseleave', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      jest.advanceTimersByTime(600);

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(false);
      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('startRippleHost registers the previous host element through the shared module function', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const wrapper = document.createElement('div');
      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      const script = document.createElement('script');
      wrapper.appendChild(host);
      wrapper.appendChild(script);
      document.body.appendChild(wrapper);

      const registerSpy = jest.spyOn(global.NTComponents, 'registerRippleHost');

      global.NTComponents.startRippleHost(script);

      expect(registerSpy).toHaveBeenCalledWith(host);
   });

   test('startButtonInteraction registers button descendants from previous sibling container', () => {
      const wrapper = document.createElement('div');
      const group = document.createElement('div');
      group.className = 'nt-button-group';
      const button = document.createElement('button');
      button.className = 'nt-button';
      const iconButton = document.createElement('button');
      iconButton.className = 'nt-icon-button';
      const groupButton = document.createElement('button');
      groupButton.className = 'nt-btn-grp-btn';
      const splitButtonSegment = document.createElement('button');
      splitButtonSegment.className = 'nt-split-button-segment';
      const script = document.createElement('script');
      group.appendChild(button);
      group.appendChild(iconButton);
      group.appendChild(groupButton);
      group.appendChild(splitButtonSegment);
      wrapper.appendChild(group);
      wrapper.appendChild(script);
      document.body.appendChild(wrapper);

      global.NTComponents.startButtonInteraction(script);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));
      iconButton.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 2,
         pointerType: 'mouse'
      }));
      groupButton.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 3,
         pointerType: 'mouse'
      }));
      splitButtonSegment.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 4,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(iconButton.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(groupButton.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(splitButtonSegment.classList.contains('nt-button--pressed-shape')).toBe(true);
   });

   test('registerButtonInteraction keeps pressed shape without a ripple host', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button';
      document.body.appendChild(button);

      global.NTComponents.registerButtonInteraction(button);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerleave' : 'mouseleave', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(false);
   });
});
