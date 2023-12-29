type EventType = 'dblclick' | 'click' | 'copy' | 'cut' | 'paste' | string

export function copy(e: HTMLElement, param: { value: (event: Event) => string, on: EventType | EventType[] }) {
  async function copyText(event: Event): Promise<void> {
    let text = param.value(event);

    try {
      await navigator.clipboard.writeText(text);

      e.dispatchEvent(
        new CustomEvent('copysuccess', {
          bubbles: true
        })
      );
    } catch (error) {
      e.dispatchEvent(
        new CustomEvent('copyerror', {
          bubbles: true,
          detail: error
        })
      );
    }
  }

  if (Array.isArray(param.on)) {
    for (const on of param.on) {
      e.addEventListener(on, copyText);
    }
  } else {
    e.addEventListener(param.on, copyText);
  }

  return {
    destroy() {
      if (Array.isArray(param.on)) {
        for (const on of param.on) {
          e.removeEventListener(on, copyText);
        }
      } else {
        e.removeEventListener(param.on, copyText);
      }
    }
  }
}