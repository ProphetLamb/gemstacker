import type { Page } from '@sveltejs/kit';
import type { Readable } from 'svelte/motion';
import { getFlash } from 'sveltekit-flash-message/client';
import { toast } from '$lib/toast';

export function showFlash(page: Readable<Page<Record<string, string>, string | null>>) {
  const flash = getFlash(page);
  flash.subscribe((msg) => {
    if (msg) {
      toast.show(msg);
    }
    flash.set(undefined);
  });
}