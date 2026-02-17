import { writable } from 'svelte/store';
import { browser } from '$app/environment';
import type { DailyDigest } from '$lib/types';
import { getDailyDigest } from '$lib/api/today';
import { getLocalDateString } from '$lib/utils/date';

const STORAGE_KEY = 'helpmotivateme_daily_digest';

export interface DailyDigestState {
	visible: boolean;
	data: DailyDigest | null;
	loading: boolean;
}

function createDailyDigestStore() {
	const { subscribe, set, update } = writable<DailyDigestState>({
		visible: false,
		data: null,
		loading: false
	});

	function getLastShownDate(): string | null {
		if (!browser) return null;
		return localStorage.getItem(STORAGE_KEY);
	}

	function markShown() {
		if (!browser) return;
		localStorage.setItem(STORAGE_KEY, getLocalDateString());
	}

	return {
		subscribe,

		shouldAutoShow(): boolean {
			if (!browser) return false;
			const lastShown = getLastShownDate();
			return lastShown !== getLocalDateString();
		},

		async showDigest(forceShow = false) {
			if (!browser) return;

			if (!forceShow && !this.shouldAutoShow()) return;

			update((s) => ({ ...s, loading: true }));

			try {
				const data = await getDailyDigest(getLocalDateString());

				// Skip overlay for users with no identities (new users)
				if (data.identities.length === 0) {
					markShown();
					update((s) => ({ ...s, loading: false }));
					return;
				}

				update(() => ({ visible: true, data, loading: false }));

				if (!forceShow) {
					markShown();
				}
			} catch {
				// On error, mark as shown to prevent retry loops
				markShown();
				update((s) => ({ ...s, loading: false }));
			}
		},

		dismiss() {
			markShown();
			update((s) => ({ ...s, visible: false }));
		}
	};
}

export const dailyDigestStore = createDailyDigestStore();
