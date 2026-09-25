import { writable } from 'svelte/store';
import { getAiStatus } from '$lib/api/aiConfig';
import type { AiStatus } from '$lib/types';

interface AiConfigState extends AiStatus {
	initialized: boolean;
}

const disabledState: AiConfigState = {
	initialized: false,
	isEnabled: false,
	provider: null,
	chatModel: null,
	isTranscriptionEnabled: false,
	transcriptionModel: null
};

function createAiConfigStore() {
	const { subscribe, set } = writable<AiConfigState>(disabledState);
	let initialization: Promise<void> | null = null;

	return {
		subscribe,
		init(): Promise<void> {
			if (initialization) return initialization;

			initialization = getAiStatus()
				.then((status) => set({ ...status, initialized: true }))
				.catch(() => set({ ...disabledState, initialized: true }));

			return initialization;
		}
	};
}

export const aiConfig = createAiConfigStore();