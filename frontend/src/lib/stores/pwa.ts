import { writable, get } from 'svelte/store';
import { browser } from '$app/environment';

interface PWAState {
	needsRefresh: boolean;
	offlineReady: boolean;
	isOnline: boolean;
	updateServiceWorker?: () => Promise<void>;
	// Install prompt state
	canInstall: boolean;
	isInstalled: boolean;
	isIOS: boolean;
	isAndroid: boolean;
}

// Store the deferred prompt outside the store (not serializable)
let deferredPrompt: any = null;

function createPWAStore() {
	const { subscribe, set, update } = writable<PWAState>({
		needsRefresh: false,
		offlineReady: false,
		isOnline: browser ? navigator.onLine : true,
		canInstall: false,
		isInstalled: false,
		isIOS: false,
		isAndroid: false
	});

	if (browser) {
		// Detect platform
		const userAgent = navigator.userAgent.toLowerCase();
		const isIOS = /iphone|ipad|ipod/.test(userAgent) && !(window as any).MSStream;
		const isAndroid = /android/.test(userAgent);

		// Check if already installed
		const isInstalled =
			window.matchMedia('(display-mode: standalone)').matches ||
			(window.navigator as any).standalone === true;

		update((state) => ({ ...state, isIOS, isAndroid, isInstalled }));

		// Online/offline listeners
		window.addEventListener('online', () => {
			update((state) => ({ ...state, isOnline: true }));
		});

		window.addEventListener('offline', () => {
			update((state) => ({ ...state, isOnline: false }));
		});

		// Listen for beforeinstallprompt event
		window.addEventListener('beforeinstallprompt', (e: Event) => {
			e.preventDefault();
			deferredPrompt = e;
			update((state) => ({ ...state, canInstall: true }));
		});

		// Listen for app installed event
		window.addEventListener('appinstalled', () => {
			deferredPrompt = null;
			update((state) => ({ ...state, isInstalled: true, canInstall: false }));
		});

		// Listen for display-mode changes
		window.matchMedia('(display-mode: standalone)').addEventListener('change', (e) => {
			update((state) => ({ ...state, isInstalled: e.matches }));
		});
	}

	return {
		subscribe,
		set,
		update,

		async install(): Promise<boolean> {
			if (!deferredPrompt) {
				return false;
			}

			try {
				deferredPrompt.prompt();
				const { outcome } = await deferredPrompt.userChoice;

				if (outcome === 'accepted') {
					update((state) => ({ ...state, isInstalled: true, canInstall: false }));
				}
				deferredPrompt = null;
				return outcome === 'accepted';
			} catch {
				return false;
			}
		},

		hasInstallPrompt(): boolean {
			return deferredPrompt !== null;
		}
	};
}

export const pwaStore = createPWAStore();
