import { writable } from 'svelte/store';

// Store to track which emoji picker is currently open (by entry ID)
// Only one picker can be open at a time
export const activeEmojiPickerId = writable<string | null>(null);

export function openEmojiPicker(entryId: string) {
	activeEmojiPickerId.set(entryId);
}

export function closeEmojiPicker() {
	activeEmojiPickerId.set(null);
}

export function toggleEmojiPicker(entryId: string) {
	activeEmojiPickerId.update(current => current === entryId ? null : entryId);
}
