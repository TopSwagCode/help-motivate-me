import { writable } from 'svelte/store';
import type { User, LoginRequest, RegisterRequest } from '$lib/types';
import { apiGet, apiPost, getOAuthUrl, ApiError } from '$lib/api/client';

interface AuthState {
	user: User | null;
	loading: boolean;
	initialized: boolean;
}

function createAuthStore() {
	const { subscribe, set, update } = writable<AuthState>({
		user: null,
		loading: false,
		initialized: false
	});

	return {
		subscribe,

		async init() {
			update((state) => ({ ...state, loading: true }));
			try {
				// Skip auth redirect when checking auth status to avoid redirect loops
				const user = await apiGet<User>('/auth/me', { skipAuthRedirect: true });
				set({ user, loading: false, initialized: true });
			} catch {
				set({ user: null, loading: false, initialized: true });
			}
		},

		async login(credentials: LoginRequest) {
			update((state) => ({ ...state, loading: true }));
			try {
				const user = await apiPost<User>('/auth/login', credentials);
				set({ user, loading: false, initialized: true });
				return { success: true };
			} catch (error) {
				update((state) => ({ ...state, loading: false }));
				return {
					success: false,
					error: error instanceof Error ? error.message : 'Login failed'
				};
			}
		},

		async register(data: RegisterRequest) {
			update((state) => ({ ...state, loading: true }));
			try {
				const user = await apiPost<User>('/auth/register', data);
				set({ user, loading: false, initialized: true });
				return { success: true };
			} catch (error) {
				update((state) => ({ ...state, loading: false }));
				return {
					success: false,
					error: error instanceof Error ? error.message : 'Registration failed',
					code: error instanceof ApiError ? error.code : undefined
				};
			}
		},

		async logout() {
			try {
				await apiPost('/auth/logout');
			} catch {
				// Ignore errors, just clear local state
			}
			set({ user: null, loading: false, initialized: true });
			window.location.href = '/';
		},

		loginWithGitHub() {
			window.location.href = getOAuthUrl('GitHub');
		},

		loginWithGoogle() {
			window.location.href = getOAuthUrl('Google');
		},

		loginWithLinkedIn() {
			window.location.href = getOAuthUrl('LinkedIn');
		},

		setUser(user: User) {
			set({ user, loading: false, initialized: true });
		},

		updateUser(updatedUser: User) {
			update((state) => ({ ...state, user: updatedUser }));
		}
	};
}

export const auth = createAuthStore();
