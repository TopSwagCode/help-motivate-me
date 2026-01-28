import { writable, get } from 'svelte/store';
import type { TourState } from '$lib/types/tour';

const STORAGE_KEY = 'helpmotivateme_tour_state';

function loadFromStorage(): Partial<TourState> {
	if (typeof window === 'undefined') return {};
	try {
		const stored = localStorage.getItem(STORAGE_KEY);
		if (stored) {
			return JSON.parse(stored);
		}
	} catch {
		// Ignore parse errors
	}
	return {};
}

function saveToStorage(state: TourState) {
	if (typeof window === 'undefined') return;
	try {
		localStorage.setItem(
			STORAGE_KEY,
			JSON.stringify({
				hasCompletedTour: state.hasCompletedTour,
				currentStepIndex: state.currentStepIndex,
				currentPage: state.currentPage
			})
		);
	} catch {
		// Ignore storage errors
	}
}

function createTourStore() {
	const stored = loadFromStorage();
	const { subscribe, set, update } = writable<TourState>({
		isActive: false,
		hasCompletedTour: stored.hasCompletedTour ?? false,
		currentStepIndex: stored.currentStepIndex ?? 0,
		currentPage: stored.currentPage ?? '/today'
	});

	return {
		subscribe,

		startTour() {
			update((state) => {
				const newState = {
					...state,
					isActive: true,
					currentStepIndex: 0,
					currentPage: '/today'
				};
				saveToStorage(newState);
				return newState;
			});
		},

		setStep(stepIndex: number, page: string) {
			update((state) => {
				const newState = {
					...state,
					currentStepIndex: stepIndex,
					currentPage: page
				};
				saveToStorage(newState);
				return newState;
			});
		},

		completeTour() {
			update((state) => {
				const newState = {
					...state,
					isActive: false,
					hasCompletedTour: true,
					currentStepIndex: 0
				};
				saveToStorage(newState);
				return newState;
			});
		},

		skipTour() {
			update((state) => {
				const newState = {
					...state,
					isActive: false,
					currentStepIndex: 0
				};
				saveToStorage(newState);
				return newState;
			});
		},

		resetTour() {
			update((state) => {
				const newState = {
					...state,
					isActive: true,
					hasCompletedTour: false,
					currentStepIndex: 0,
					currentPage: '/today'
				};
				saveToStorage(newState);
				return newState;
			});
		},

		getState() {
			return get({ subscribe });
		}
	};
}

export const tour = createTourStore();
