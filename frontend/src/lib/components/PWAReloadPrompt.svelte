<script lang="ts">
	import { onMount } from 'svelte';
	import { pwaStore } from '$lib/stores/pwa';
	import { browser } from '$app/environment';

	let needRefresh = $state(false);
	let offlineReady = $state(false);
	let updateSW: ((reloadPage?: boolean) => Promise<void>) | undefined = $state(undefined);

	onMount(async () => {
		if (!browser) return;

		const { useRegisterSW } = await import('virtual:pwa-register/svelte');

		const {
			needRefresh: needRefreshStore,
			offlineReady: offlineReadyStore,
			updateServiceWorker
		} = useRegisterSW({
			immediate: true,
			onRegisteredSW(swUrl: string, registration: ServiceWorkerRegistration | undefined) {
				console.log(`Service Worker registered: ${swUrl}`);

				// Check for updates every hour
				if (registration) {
					setInterval(
						() => {
							registration.update();
						},
						60 * 60 * 1000
					);
				}
			},
			onRegisterError(error: Error) {
				console.error('Service Worker registration error:', error);
			}
		});

		updateSW = updateServiceWorker;

		// Subscribe to stores
		needRefreshStore.subscribe((value: boolean) => {
			needRefresh = value;
			pwaStore.update((state) => ({ ...state, needsRefresh: value }));
		});

		offlineReadyStore.subscribe((value: boolean) => {
			offlineReady = value;
			pwaStore.update((state) => ({ ...state, offlineReady: value }));
		});

		// Store the update function
		pwaStore.update((state) => ({ ...state, updateServiceWorker }));
	});

	function close() {
		offlineReady = false;
		needRefresh = false;
	}

	async function handleUpdate() {
		if (updateSW) {
			await updateSW(true);
		}
	}
</script>

{#if offlineReady}
	<div
		class="fixed bottom-4 right-4 z-50 flex items-center gap-3 rounded-lg bg-green-600 px-4 py-3 text-white shadow-lg"
	>
		<span>App ready to work offline</span>
		<button onclick={close} class="text-white/80 hover:text-white" aria-label="Dismiss">
			<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M6 18L18 6M6 6l12 12"
				/>
			</svg>
		</button>
	</div>
{/if}

{#if needRefresh}
	<div class="fixed bottom-4 right-4 z-50 rounded-lg bg-indigo-600 px-4 py-3 text-white shadow-lg">
		<div class="flex items-center gap-4">
			<span>New version available!</span>
			<button
				onclick={handleUpdate}
				class="rounded bg-white px-3 py-1 font-medium text-indigo-600 hover:bg-indigo-50"
			>
				Update
			</button>
			<button onclick={close} class="text-white/80 hover:text-white" aria-label="Dismiss">
				<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M6 18L18 6M6 6l12 12"
					/>
				</svg>
			</button>
		</div>
	</div>
{/if}
