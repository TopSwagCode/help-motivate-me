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
		console.log('[PWA] Update button clicked, updateSW:', !!updateSW);
		if (updateSW) {
			try {
				console.log('[PWA] Calling updateSW(true) to activate new service worker...');
				await updateSW(true);
				console.log('[PWA] updateSW completed, page should reload');
			} catch (error) {
				console.error('[PWA] Error during update:', error);
				// Fallback: force reload the page
				window.location.reload();
			}
		} else {
			console.warn('[PWA] updateSW not available, forcing reload');
			window.location.reload();
		}
	}
</script>

{#if needRefresh}
	<div class="fixed bottom-20 left-4 right-4 sm:left-auto sm:right-4 sm:bottom-20 z-[100] rounded-2xl bg-indigo-600 px-4 py-3 text-white shadow-lg">
		<div class="flex items-center justify-between gap-4">
			<span class="font-medium">New version available!</span>
			<div class="flex items-center gap-2">
				<button
					onclick={handleUpdate}
					class="rounded-2xl bg-warm-paper px-4 py-2 font-semibold text-indigo-600 hover:bg-indigo-50 active:bg-indigo-100 touch-manipulation min-h-[44px] min-w-[80px]"
				>
					Update
				</button>
				<button onclick={close} class="p-2 text-white/80 hover:text-white touch-manipulation min-h-[44px]" aria-label="Dismiss">
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
	</div>
{/if}
