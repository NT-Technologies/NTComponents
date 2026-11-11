// Jest setup for TnT Editors components
// Mock NTComponents global
global.NTComponents = {
  customAttribute: 'ntid'
};

// Mock CustomElementRegistry if not available in test environment
if (!global.customElements) {
  global.customElements = {
    define: jest.fn(),
    get: jest.fn(),
    whenDefined: jest.fn().mockResolvedValue(undefined)
  };
}

// Mock ResizeObserver if not available in test environment
if (!global.ResizeObserver) {
  global.ResizeObserver = class ResizeObserver {
    constructor(callback) {
      this.callback = callback;
    }
    
    observe() {}
    unobserve() {}
    disconnect() {}
  };
}

// Mock MutationObserver if not available in test environment
if (!global.MutationObserver) {
  global.MutationObserver = class MutationObserver {
    constructor(callback) {
      this.callback = callback;
    }
    
    observe() {}
    disconnect() {}
  };
}

// Mock IntersectionObserver if not available in test environment
if (!global.IntersectionObserver) {
  global.IntersectionObserver = class IntersectionObserver {
    constructor(callback, options) {
      this.callback = callback;
      this.options = options;
    }
    
    observe() {}
    unobserve() {}
    disconnect() {}
  };
}