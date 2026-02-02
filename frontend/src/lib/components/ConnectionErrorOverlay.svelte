<script lang="ts">
	import { t } from 'svelte-i18n';
	import { connectionStore } from '$lib/stores/connection';
	import { pwaStore } from '$lib/stores/pwa';

	let isRetrying = $state(false);

	// Determine error type based on connection state
	let errorType = $derived.by(() => {
		if (!$pwaStore.isOnline) return 'offline';
		if (!$connectionStore.isApiReachable) return 'serverUnavailable';
		return null;
	});

	let shouldShow = $derived(errorType !== null);

	async function handleRetry() {
		isRetrying = true;
		await connectionStore.retryNow();
		isRetrying = false;
	}
</script>

{#if shouldShow}
	<div class="fixed inset-0 z-[100] bg-gradient-to-br from-indigo-50 via-white to-gray-50 flex items-center justify-center px-4">
		<div class="max-w-lg w-full">
			<div class="bg-warm-paper rounded-2xl shadow-lg border border-primary-100 p-8 text-center">
				<!-- Friendly Icon -->
				<div class="mb-6">
					{#if errorType === 'offline'}
						<!-- Cloud with X - friendly "no connection" icon -->
						<div class="w-24 h-24 mx-auto bg-amber-50 rounded-full flex items-center justify-center">
							<svg class="w-14 h-14 text-amber-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z" />
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.5 12.5l5 5m0-5l-5 5" />
							</svg>
						</div>
					{:else}
						<!-- Sleeping/resting icon - friendly "taking a break" -->
						<div class="w-24 h-24 mx-auto bg-indigo-50 rounded-full flex items-center justify-center">
							<svg class="w-14 h-14 text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 9h.01M9 9h.01" />
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13c.5.5 1.5 1 3 1s2.5-.5 3-1" />
							</svg>
						</div>
					{/if}
				</div>

				<!-- Friendly Title -->
				<h1 class="text-2xl font-bold text-cocoa-800 mb-3">
					{$t(`errorPages.${errorType}.title`)}
				</h1>

				<!-- Friendly Message -->
				<p class="text-cocoa-600 mb-2 text-lg">
					{$t(`errorPages.${errorType}.message`)}
				</p>

				<!-- Helpful Suggestion -->
				<p class="text-cocoa-500 mb-6">
					{$t(`errorPages.${errorType}.suggestion`)}
				</p>

				<!-- Auto-recovery indicator -->
				<div class="mb-6 py-3 px-4 bg-warm-cream rounded-2xl">
					{#if $connectionStore.isChecking || isRetrying}
						<div class="flex items-center justify-center gap-2 text-sm text-cocoa-600">
							<svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
							<span>{$t('errorPages.autoRecovery.checking')}</span>
						</div>
					{:else}
						<p class="text-sm text-cocoa-500">
							{$t('errorPages.autoRecovery.willRetry', { values: { seconds: $connectionStore.retryCountdown } })}
						</p>
					{/if}
				</div>

				<!-- Retry Button -->
				<button
					onclick={handleRetry}
					disabled={$connectionStore.isChecking || isRetrying}
					class="w-full inline-flex items-center justify-center gap-2 px-5 py-3 border border-transparent text-base font-medium rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
				>
					{#if $connectionStore.isChecking || isRetrying}
						<svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						{$t('errorPages.autoRecovery.checking')}
					{:else}
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
						</svg>
						{$t('errorPages.actions.tryAgain')}
					{/if}
				</button>
			</div>

			<!-- Help Link -->
			<div class="mt-6 text-center">
				<p class="text-sm text-cocoa-500">
					{$t('errorPages.helpText')}
				</p>
			</div>
		</div>
	</div>
{/if}
