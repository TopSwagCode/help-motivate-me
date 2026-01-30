import { writable, get } from 'svelte/store';
import { browser } from '$app/environment';

export interface ConnectionState {
	isOnline: boolean; // Browser online status
	isApiReachable: boolean; // Can reach API server
	lastError: string | null;
	isChecking: boolean;
	retryCountdown: number;
}

const API_BASE = browser ? (import.meta.env.VITE_API_URL || 'http://localhost:5001') : '';
const HEALTH_ENDPOINT = `${API_BASE}/health`;
const CHECK_INTERVAL = 10000; // 10 seconds
const INITIAL_CHECK_DELAY = 2000; // 2 seconds

function createConnectionStore() {
	const { subscribe, set, update } = writable<ConnectionState>({
		isOnline: browser ? navigator.onLine : true,
		isApiReachable: true, // Assume reachable until proven otherwise
		lastError: null,
		isChecking: false,
		retryCountdown: 0
	});

	let checkInterval: ReturnType<typeof setInterval> | null = null;
	let countdownInterval: ReturnType<typeof setInterval> | null = null;
	let hasStartedRecovery = false;

	async function checkApiHealth(): Promise<boolean> {
		const state = get({ subscribe });
		if (state.isChecking) return state.isApiReachable;

		update((s) => ({ ...s, isChecking: true }));

		try {
			const response = await fetch(HEALTH_ENDPOINT, {
				method: 'GET',
				cache: 'no-store',
				signal: AbortSignal.timeout(5000)
			});
			const isReachable = response.ok;
			update((s) => ({
				...s,
				isApiReachable: isReachable,
				isChecking: false,
				lastError: isReachable ? null : 'Server returned an error'
			}));
			return isReachable;
		} catch (error) {
			update((s) => ({
				...s,
				isApiReachable: false,
				isChecking: false,
				lastError: 'Unable to connect to server'
			}));
			return false;
		}
	}

	function startRecoveryLoop() {
		if (hasStartedRecovery || !browser) return;
		hasStartedRecovery = true;

		// Start countdown
		update((s) => ({ ...s, retryCountdown: 10 }));

		countdownInterval = setInterval(() => {
			update((s) => ({
				...s,
				retryCountdown: s.retryCountdown > 0 ? s.retryCountdown - 1 : 10
			}));
		}, 1000);

		// Check every 10 seconds
		checkInterval = setInterval(async () => {
			const isReachable = await checkApiHealth();
			if (isReachable) {
				stopRecoveryLoop();
				// Reload the page to reinitialize everything
				location.reload();
			}
		}, CHECK_INTERVAL);

		// Do an initial check after a short delay
		setTimeout(checkApiHealth, INITIAL_CHECK_DELAY);
	}

	function stopRecoveryLoop() {
		hasStartedRecovery = false;
		if (checkInterval) {
			clearInterval(checkInterval);
			checkInterval = null;
		}
		if (countdownInterval) {
			clearInterval(countdownInterval);
			countdownInterval = null;
		}
	}

	if (browser) {
		// Listen for online/offline events
		window.addEventListener('online', () => {
			update((s) => ({ ...s, isOnline: true }));
			// When back online, check API immediately
			checkApiHealth().then((isReachable) => {
				if (isReachable && hasStartedRecovery) {
					stopRecoveryLoop();
					location.reload();
				}
			});
		});

		window.addEventListener('offline', () => {
			update((s) => ({ ...s, isOnline: false, isApiReachable: false }));
		});
	}

	return {
		subscribe,

		// Report a connection error (called from API client)
		reportError(error: Error) {
			const isNetworkError =
				error.message.includes('fetch') ||
				error.message.includes('Failed to fetch') ||
				error.message.includes('NetworkError') ||
				error.message.includes('network');

			if (isNetworkError) {
				update((s) => ({
					...s,
					isApiReachable: false,
					lastError: error.message
				}));
				startRecoveryLoop();
			}
		},

		// Mark API as reachable (called after successful API call)
		markReachable() {
			update((s) => ({
				...s,
				isApiReachable: true,
				lastError: null
			}));
			if (hasStartedRecovery) {
				stopRecoveryLoop();
			}
		},

		// Manually retry connection
		async retryNow() {
			const isReachable = await checkApiHealth();
			if (isReachable) {
				stopRecoveryLoop();
				location.reload();
			}
			return isReachable;
		},

		// Start recovery loop (for startup failures)
		startRecovery() {
			startRecoveryLoop();
		},

		// Check if we have any connection issues
		hasConnectionIssue(): boolean {
			const state = get({ subscribe });
			return !state.isOnline || !state.isApiReachable;
		}
	};
}

export const connectionStore = createConnectionStore();
