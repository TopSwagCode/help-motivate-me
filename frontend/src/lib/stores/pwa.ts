import { writable } from 'svelte/store';
import { browser } from '$app/environment';

interface PWAState {
	needsRefresh: boolean;
	offlineReady: boolean;
	isOnline: boolean;
	updateServiceWorker?: () => Promise<void>;
}

function createPWAStore() {
	const { subscribe, set, update } = writable<PWAState>({
		needsRefresh: false,
		offlineReady: false,
		isOnline: browser ? navigator.onLine : true
	});

	if (browser) {
		window.addEventListener('online', () => {
			update((state) => ({ ...state, isOnline: true }));
		});

		window.addEventListener('offline', () => {
			update((state) => ({ ...state, isOnline: false }));
		});
	}

	return { subscribe, set, update };
}

export const pwaStore = createPWAStore();
