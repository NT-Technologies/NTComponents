/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals'
import { onDispose, onLoad, onUpdate } from '../NTLoader.razor.js'

describe('NTLoader runtime', () => {
  function createLoaderElement({
    animate = 'true',
    sequence = '1 2 3',
    interval = '1000',
    transitionDuration = '700'
  } = {}) {
    const loader = document.createElement('div')
    loader.classList.add('nt-loader')
    loader.dataset.animate = animate
    loader.dataset.shapeSequence = sequence
    loader.dataset.shapeIntervalMs = interval
    loader.dataset.transitionDurationMs = transitionDuration

    const shape = document.createElement('nt-shape')
    shape.classList.add('nt-loader-shape')
    shape.dataset.shape = sequence.split(' ')[0]
    shape.dataset.animateShapeChanges = 'true'
    shape.dataset.transitionDurationMs = '64'
    shape.dataset.transitionEasing = '0'
    shape.dataset.clipId = 'nt-loader-shape-clip-test'

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
    svg.classList.add('nt-shape-defs')

    const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs')
    const clipPath = document.createElementNS('http://www.w3.org/2000/svg', 'clipPath')
    clipPath.setAttribute('id', 'nt-loader-shape-clip-test')
    clipPath.setAttribute('clipPathUnits', 'objectBoundingBox')

    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path')
    path.classList.add('nt-shape-path')
    clipPath.appendChild(path)
    defs.appendChild(clipPath)
    svg.appendChild(defs)
    shape.appendChild(svg)

    const content = document.createElement('div')
    content.classList.add('nt-shape-content')
    shape.appendChild(content)

    const animation = {
      cancel: jest.fn(),
      addEventListener: jest.fn()
    }
    shape.animate = jest.fn(() => animation)

    loader.appendChild(shape)
    const marker = document.createElement('nt-page-script')
    marker.setAttribute('src', './_content/NTComponents/Progress/NTLoader.razor.js')
    const scriptHost = document.createElement('div')
    scriptHost.hidden = true
    scriptHost.appendChild(marker)
    loader.appendChild(scriptHost)
    document.body.appendChild(loader)

    return { animation, loader, shape, marker }
  }

  beforeEach(() => {
    document.body.innerHTML = ''
    jest.useFakeTimers()
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: jest.fn().mockImplementation((query) => ({
        matches: false,
        media: query,
        addListener() { },
        removeListener() { },
        addEventListener() { },
        removeEventListener() { },
        dispatchEvent() { return false },
        onchange: null
      }))
    })
    // Random 0 always picks the next shape in the list, which keeps cycle assertions deterministic.
    jest.spyOn(Math, 'random').mockReturnValue(0)
  })

  afterEach(() => {
    onDispose()
    jest.useRealTimers()
    jest.restoreAllMocks()
  })

  test('onLoad is intentionally inert because per-render onUpdate starts the indicator', () => {
    const { loader, shape } = createLoaderElement()

    onLoad(loader)

    expect(shape.dataset.shape).toBe('1')
    expect(shape.animate).not.toHaveBeenCalled()
  })

  test('onUpdate starts immediately and unchanged updates do not restart from the first shape', () => {
    const { loader, shape } = createLoaderElement()

    onUpdate(loader)
    expect(shape.dataset.shape).toBe('2')
    expect(shape.animate).toHaveBeenCalledTimes(1)

    onUpdate(loader)
    expect(shape.dataset.shape).toBe('2')
    expect(shape.animate).toHaveBeenCalledTimes(1)
  })

  test('SSR page-script callbacks start the containing loader without an interactive callback', () => {
    const { marker, shape } = createLoaderElement()

    onLoad(marker)
    onUpdate(marker)
    expect(shape.animate).toHaveBeenCalledTimes(1)
    expect(shape.dataset.shape).toBe('2')

    jest.advanceTimersByTime(300)
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(0)
    onUpdate(marker)
    expect(shape.animate).toHaveBeenCalledTimes(1)
    jest.advanceTimersByTime(700)
    expect(shape.dataset.shape).toBe('3')
  })

  test('removing one SSR loader disposes its animation without stopping another loader', () => {
    const first = createLoaderElement()
    const second = createLoaderElement()
    onUpdate(first.marker)
    onUpdate(second.marker)
    jest.advanceTimersByTime(200)

    first.loader.remove()
    onDispose(first.marker)
    const removedPath = first.shape.querySelector('path').getAttribute('d')
    jest.advanceTimersByTime(1000)

    expect(first.animation.cancel).toHaveBeenCalledTimes(1)
    expect(first.shape.querySelector('path').getAttribute('d')).toBe(removedPath)
    expect(second.animation.cancel).not.toHaveBeenCalled()
    expect(second.shape.dataset.shape).toBe('3')
  })

  test('scheduled cycles advance shapes until dispose clears the loader state', () => {
    const { animation, loader, shape } = createLoaderElement()

    onUpdate(loader)
    jest.advanceTimersByTime(1000)

    expect(shape.dataset.shape).toBe('3')
    expect(shape.animate).toHaveBeenCalledTimes(1)

    onDispose(loader)
    jest.advanceTimersByTime(3000)

    expect(shape.animate).toHaveBeenCalledTimes(1)
    expect(animation.cancel).toHaveBeenCalled()
  })

  test('global updates coalesce into one document sync', () => {
    const first = createLoaderElement()
    const second = createLoaderElement()
    const querySelectorAll = jest.spyOn(document, 'querySelectorAll')

    onUpdate()
    onUpdate()

    expect(first.shape.animate).not.toHaveBeenCalled()
    expect(second.shape.animate).not.toHaveBeenCalled()

    jest.runAllTicks()

    expect(querySelectorAll).toHaveBeenCalledTimes(1)
    expect(first.shape.animate).toHaveBeenCalledTimes(1)
    expect(second.shape.animate).toHaveBeenCalledTimes(1)
  })

  test('constant rotation advances 50 degrees per cycle without restarting', () => {
    const { loader, shape, animation } = createLoaderElement({ interval: '650' })

    onUpdate(loader)
    expect(shape.animate).toHaveBeenCalledWith(
      [{ transform: 'rotate(0deg)' }, { transform: 'rotate(360deg)' }],
      { duration: 4680, easing: 'linear', iterations: Infinity })

    jest.advanceTimersByTime(1950)
    expect(shape.animate).toHaveBeenCalledTimes(1)
    expect(animation.cancel).not.toHaveBeenCalled()
  })

  test('spring turn follows the morph and retains its angle between cycles', () => {
    const { loader, shape } = createLoaderElement({ interval: '650' })
    const path = shape.querySelector('path')

    onUpdate(loader)
    const initialPath = path.getAttribute('d')
    jest.advanceTimersByTime(300)
    expect(path.getAttribute('d')).not.toBe(initialPath)
    // The underdamped spring overshoots its 90-degree target.
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(90)
    expect(Number.parseFloat(shape.style.rotate)).toBeLessThan(100)

    jest.advanceTimersByTime(340)
    const beforeNextCycle = Number.parseFloat(shape.style.rotate)
    expect(beforeNextCycle).toBeGreaterThan(89)
    expect(beforeNextCycle).toBeLessThan(91)
    jest.advanceTimersByTime(16)
    expect(Number.parseFloat(shape.style.rotate)).toBeCloseTo(beforeNextCycle, 5)
    jest.advanceTimersByTime(300)
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(180)
    expect(Number.parseFloat(shape.style.rotate)).toBeLessThan(190)
  })

  test.each(['paused', 'reduced motion', 'single shape'])('%s renders a stationary initial shape', (mode) => {
    const { loader, shape } = createLoaderElement({
      animate: mode === 'paused' ? 'false' : 'true',
      sequence: mode === 'single shape' ? '1' : '1 2 3'
    })
    if (mode === 'reduced motion') {
      window.matchMedia.mockReturnValue({ matches: true })
    }

    onUpdate(loader)
    const initialPath = shape.querySelector('path').getAttribute('d')
    jest.advanceTimersByTime(3000)

    expect(shape.dataset.shape).toBe('1')
    expect(shape.querySelector('path').getAttribute('d')).toBe(initialPath)
    expect(shape.animate).not.toHaveBeenCalled()
    expect(shape.style.rotate).toBe('')
  })

  test('pausing cancels both rotation and an in-flight morph, and resuming starts cleanly', () => {
    const { loader, shape, animation } = createLoaderElement()
    onUpdate(loader)
    jest.advanceTimersByTime(200)

    loader.dataset.animate = 'false'
    onUpdate(loader)
    const pausedPath = shape.querySelector('path').getAttribute('d')
    jest.advanceTimersByTime(2000)
    expect(animation.cancel).toHaveBeenCalledTimes(1)
    expect(shape.style.rotate).toBe('')
    expect(shape.dataset.shape).toBe('1')
    expect(shape.querySelector('path').getAttribute('d')).toBe(pausedPath)

    loader.dataset.animate = 'true'
    onUpdate(loader)
    jest.advanceTimersByTime(200)
    expect(shape.animate).toHaveBeenCalledTimes(2)
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(0)
  })

  test('dispose cancels in-flight shape frames as well as the cycle timer', () => {
    const { loader, shape } = createLoaderElement()
    onUpdate(loader)
    jest.advanceTimersByTime(200)
    onDispose(loader)
    const finalPath = shape.querySelector('path').getAttribute('d')
    jest.advanceTimersByTime(3000)

    expect(shape.querySelector('path').getAttribute('d')).toBe(finalPath)
    expect(shape.style.rotate).toBe('')
    expect(jest.getTimerCount()).toBe(0)
  })

  test('each cycle picks a random shape from the list', () => {
    const { loader, shape } = createLoaderElement()
    Math.random.mockReturnValue(0.99)

    onUpdate(loader)
    expect(shape.dataset.shape).toBe('3')

    jest.advanceTimersByTime(1000)
    expect(shape.dataset.shape).toBe('2')
  })

  test('random cycles never repeat the current shape', () => {
    Math.random.mockRestore()
    const { loader, shape } = createLoaderElement({ sequence: '1 2 3 4', interval: '650' })
    onUpdate(loader)

    const shown = [shape.dataset.shape]
    for (let cycle = 0; cycle < 40; cycle++) {
      jest.advanceTimersByTime(650)
      shown.push(shape.dataset.shape)
    }

    expect(shown.every((value, index) => index === 0 || value !== shown[index - 1])).toBe(true)
    expect(new Set(shown).size).toBeGreaterThan(2)
  })

  test('a short cycle can morph back to the initial shape before earlier transitions settle', () => {
    const { loader, shape } = createLoaderElement({ sequence: '1 2', interval: '650' })
    onUpdate(loader)
    jest.advanceTimersByTime(640)
    const firstTurn = Number.parseFloat(shape.style.rotate)
    jest.advanceTimersByTime(320)

    expect(shape.dataset.shape).toBe('1')
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(firstTurn + 90)
    jest.advanceTimersByTime(650)
    expect(shape.dataset.shape).toBe('2')
    expect(Number.parseFloat(shape.style.rotate)).toBeGreaterThan(270)
  })
})
