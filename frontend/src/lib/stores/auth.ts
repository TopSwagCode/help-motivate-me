import { writable } from 'svelte/store';
import type { User, LoginRequest } from '$lib/types';
import { apiGet, apiPost, NetworkError } from '$lib/api/client';
import { connectionStore } from '$lib/stores/connection';

interface AuthState {
	user: User | null;
	loading: boolean;
	initialized: boolean;
	networkError: boolean; // True if init failed due to network
}

function createAuthStore() {
	const { subscribe, set, update } = writable<AuthState>({
		user: null,
		loading: false,
		initialized: false,
		networkError: false
	});

	return {
		subscribe,

		async init() {
			update((state) => ({ ...state, loading: true }));
			try {
				// Skip auth redirect when checking auth status to avoid redirect loops
				const user = await apiGet<User>('/auth/me', { skipAuthRedirect: true });
				set({ user, loading: false, initialized: true, networkError: false });
			} catch (error) {
				// Check if it's a network error
				const isNetworkError = error instanceof NetworkError ||
					(error instanceof Error && (
						error.message.includes('fetch') ||
						error.message.includes('NetworkError')
					));

				if (isNetworkError) {
					// Network error - start recovery mode
					set({ user: null, loading: false, initialized: true, networkError: true });
					connectionStore.startRecovery();
				} else {
					// Other error (e.g., 401 not logged in) - normal flow
					set({ user: null, loading: false, initialized: true, networkError: false });
				}
			}
		},

		async login(credentials: LoginRequest) {
			update((state) => ({ ...state, loading: true }));
			try {
				const user = await apiPost<User>('/auth/login', credentials);
				set({ user, loading: false, initialized: true, networkError: false });
				return { success: true };
			} catch (error) {
				update((state) => ({ ...state, loading: false }));
				return {
					success: false,
					error: error instanceof Error ? error.message : 'Login failed'
				};
			}
		},

		async logout() {
			try {
				await apiPost('/auth/logout');
			} catch {
				// Ignore errors, just clear local state
			}
			set({ user: null, loading: false, initialized: true, networkError: false });
			window.location.href = '/';
		},

		setUser(user: User) {
			set({ user, loading: false, initialized: true, networkError: false });
		},

		updateUser(updatedUser: User) {
			update((state) => ({ ...state, user: updatedUser }));
		}
	};
}

export const auth = createAuthStore();
