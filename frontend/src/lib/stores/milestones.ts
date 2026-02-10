import { writable, get } from 'svelte/store';
import { browser } from '$app/environment';
import type { UserMilestone } from '$lib/types';
import { getUnseenMilestones, markMilestonesSeen } from '$lib/api/milestones';

export interface MilestoneState {
	pendingMilestones: UserMilestone[];
	showCelebration: boolean;
	currentIndex: number;
	isLoading: boolean;
}

function createMilestoneStore() {
	const { subscribe, set, update } = writable<MilestoneState>({
		pendingMilestones: [],
		showCelebration: false,
		currentIndex: 0,
		isLoading: false
	});

	return {
		subscribe,

		async checkForNew(): Promise<UserMilestone[]> {
			if (!browser) return [];

			update((s) => ({ ...s, isLoading: true }));

			try {
				const unseen = await getUnseenMilestones();
				if (unseen.length > 0) {
					update((s) => ({
						...s,
						pendingMilestones: unseen,
						showCelebration: true,
						currentIndex: 0,
						isLoading: false
					}));
				} else {
					update((s) => ({ ...s, isLoading: false }));
				}
				return unseen;
			} catch {
				update((s) => ({ ...s, isLoading: false }));
				return [];
			}
		},

		showMilestones(milestones: UserMilestone[]) {
			if (milestones.length === 0) return;
			update((s) => ({
				...s,
				pendingMilestones: milestones,
				showCelebration: true,
				currentIndex: 0
			}));
		},

		nextMilestone() {
			const state = get({ subscribe });
			if (state.currentIndex < state.pendingMilestones.length - 1) {
				update((s) => ({ ...s, currentIndex: s.currentIndex + 1 }));
			}
		},

		prevMilestone() {
			const state = get({ subscribe });
			if (state.currentIndex > 0) {
				update((s) => ({ ...s, currentIndex: s.currentIndex - 1 }));
			}
		},

		async dismissCelebration() {
			const state = get({ subscribe });
			const milestoneIds = state.pendingMilestones.map((m) => m.id);

			try {
				await markMilestonesSeen(milestoneIds);
			} catch {
				// Silent failure - milestones will be shown again next time
			}

			update((s) => ({
				...s,
				pendingMilestones: [],
				showCelebration: false,
				currentIndex: 0
			}));
		},

		// For preview mode (admin) - show without marking seen
		previewMilestone(milestone: UserMilestone) {
			update((s) => ({
				...s,
				pendingMilestones: [milestone],
				showCelebration: true,
				currentIndex: 0
			}));
		},

		dismissPreview() {
			update((s) => ({
				...s,
				pendingMilestones: [],
				showCelebration: false,
				currentIndex: 0
			}));
		}
	};
}

export const milestoneStore = createMilestoneStore();
