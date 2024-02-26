import type { IconSource } from '@steeze-ui/svelte-icon';
import { writable, type StartStopNotifier } from 'svelte/store';

export type ToastMessage = {
  type: 'success' | 'warning' | 'error' | 'surface' | 'primary' | 'secondary' | 'tertiary';
  icon?: IconSource;
  options?: {
    dismissable?: boolean;
    duration?: number;
  };
} & (
    | {
      message: string;
    }
    | {
      message: (string | { name: string; url: string })[];
    }
  );

const defaultOptions = {
  dismissable: true,
  duration: 10000,
};

class ToastState {
  toast: ToastMessage;
  handle: string;

  constructor(toast: ToastMessage, handle: string) {
    this.toast = toast;
    this.handle = handle;

    const duration = this.toast.options?.duration === undefined ? defaultOptions.duration : this.toast.options.duration;
    if (duration > 0) {
      setTimeout(() => this.dismiss(), duration);
    }
  }

  dismiss() {
    const dismissable = this.toast.options?.dismissable === undefined ? defaultOptions.dismissable : this.toast.options.dismissable;
    if (dismissable) {
      toast.dismiss(this.handle);
    }
  }
}

export function toaster(start?: StartStopNotifier<ToastState[]>) {
  const store = writable<ToastState[]>([], start);

  function show(message: ToastMessage) {
    const state = new ToastState(message, crypto.randomUUID());
    store.update((messages) => [state, ...messages]);
    return state.handle;
  }

  function dismiss(handle: string) {
    store.update((messages) => messages.filter((m) => m.handle !== handle));
  }

  return { subscribe: store.subscribe, show, dismiss };
}

export const toast = toaster();