<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { t } from 'svelte-i18n';
	import { pwaStore } from '$lib/stores/pwa';
	import { onMount, onDestroy } from 'svelte';
	import { browser } from '$app/environment';

	type ErrorType = 'notFound' | 'serverError' | 'offline' | 'serverUnavailable';

	function getErrorType(status: number, error: App.Error | null, isOnline: boolean): ErrorType {
		if (!isOnline) return 'offline';
		if (status === 404) return 'notFound';
		if (error?.message?.includes('fetch')) return 'serverUnavailable';
		return 'serverError';
	}

	let errorType = $derived(getErrorType($page.status, $page.error, $pwaStore.isOnline));
	let isRecoverable = $derived(errorType !== 'notFound');

	// Auto-recovery state
	let isChecking = $state(false);
	let checkInterval: ReturnType<typeof setInterval> | null = null;
	let secondsUntilRetry = $state(10);
	let retryCountdown: ReturnType<typeof setInterval> | null = null;

	// API health check endpoint - use relative URL when VITE_API_URL is not set
	const API_URL = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';
	const HEALTH_ENDPOINT = `${API_URL}/health`;

	async function checkServerHealth(): Promise<boolean> {
		try {
			const response = await fetch(HEALTH_ENDPOINT, {
				method: 'GET',
				cache: 'no-store',
				signal: AbortSignal.timeout(5000)
			});
			return response.ok;
		} catch {
			return false;
		}
	}

	async function attemptRecovery() {
		if (isChecking) return;

		isChecking = true;
		const isHealthy = await checkServerHealth();
		isChecking = false;

		if (isHealthy) {
			// Server is back! Reload the page
			location.reload();
		}
	}

	function startAutoRecovery() {
		if (!browser || !isRecoverable) return;

		// Start countdown
		secondsUntilRetry = 10;
		retryCountdown = setInterval(() => {
			secondsUntilRetry--;
			if (secondsUntilRetry <= 0) {
				secondsUntilRetry = 10;
			}
		}, 1000);

		// Check every 10 seconds
		checkInterval = setInterval(() => {
			attemptRecovery();
		}, 10000);

		// Also do an immediate check after 3 seconds
		setTimeout(() => attemptRecovery(), 3000);
	}

	function stopAutoRecovery() {
		if (checkInterval) {
			clearInterval(checkInterval);
			checkInterval = null;
		}
		if (retryCountdown) {
			clearInterval(retryCountdown);
			retryCountdown = null;
		}
	}

	onMount(() => {
		if (isRecoverable) {
			startAutoRecovery();
		}
	});

	onDestroy(() => {
		stopAutoRecovery();
	});

	// Watch for online status changes
	$effect(() => {
		if ($pwaStore.isOnline && isRecoverable) {
			// We're back online, try to recover immediately
			attemptRecovery();
		}
	});

	function goToToday() {
		goto('/today');
	}

	function goBack() {
		history.back();
	}

	function manualRetry() {
		attemptRecovery();
	}
</script>

<svelte:head>
	<title>{$t(`errorPages.${errorType}.title`)} - Help Motivate Me</title>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-gray-50 flex items-center justify-center px-4">
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
				{:else if errorType === 'serverUnavailable' || errorType === 'serverError'}
					<!-- Sleeping/resting icon - friendly "taking a break" -->
					<div class="w-24 h-24 mx-auto bg-indigo-50 rounded-full flex items-center justify-center">
						<svg class="w-14 h-14 text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 9h.01M9 9h.01" />
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13c.5.5 1.5 1 3 1s2.5-.5 3-1" />
						</svg>
					</div>
				{:else}
					<!-- Compass/lost icon - friendly "can't find it" -->
					<div class="w-24 h-24 mx-auto bg-purple-50 rounded-full flex items-center justify-center">
						<svg class="w-14 h-14 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
							<circle cx="12" cy="11" r="1" fill="currentColor" />
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

			<!-- Auto-recovery indicator for recoverable errors -->
			{#if isRecoverable}
				<div class="mb-6 py-3 px-4 bg-warm-cream rounded-2xl">
					{#if isChecking}
						<div class="flex items-center justify-center gap-2 text-sm text-cocoa-600">
							<svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
							<span>{$t('errorPages.autoRecovery.checking')}</span>
						</div>
					{:else}
						<p class="text-sm text-cocoa-500">
							{$t('errorPages.autoRecovery.willRetry', { values: { seconds: secondsUntilRetry } })}
						</p>
					{/if}
				</div>
			{/if}

			<!-- Action Buttons -->
			<div class="flex flex-col gap-3">
				{#if errorType === 'notFound'}
					<!-- 404: Go to Today as primary, Go Back as secondary -->
					<button
						onclick={goToToday}
						class="w-full inline-flex items-center justify-center gap-2 px-5 py-3 border border-transparent text-base font-medium rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
						</svg>
						{$t('errorPages.actions.goToToday')}
					</button>
					<button
						onclick={goBack}
						class="w-full inline-flex items-center justify-center gap-2 px-5 py-3 border border-primary-200 text-base font-medium rounded-xl text-cocoa-700 bg-warm-paper hover:bg-warm-cream transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
						</svg>
						{$t('errorPages.actions.goBack')}
					</button>
				{:else}
					<!-- Recoverable errors: Manual retry as primary, Go to Today as secondary -->
					<button
						onclick={manualRetry}
						disabled={isChecking}
						class="w-full inline-flex items-center justify-center gap-2 px-5 py-3 border border-transparent text-base font-medium rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
					>
						{#if isChecking}
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
					<button
						onclick={goToToday}
						class="w-full inline-flex items-center justify-center gap-2 px-5 py-3 border border-primary-200 text-base font-medium rounded-xl text-cocoa-700 bg-warm-paper hover:bg-warm-cream transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
						</svg>
						{$t('errorPages.actions.goToToday')}
					</button>
				{/if}
			</div>
		</div>

		<!-- Help Link -->
		<div class="mt-6 text-center">
			<p class="text-sm text-cocoa-500 mb-2">
				{$t('errorPages.helpText')}
			</p>
			<a href="/faq" class="text-sm text-indigo-600 hover:text-indigo-700 font-medium">
				{$t('common.info.visitFaq')}
			</a>
		</div>
	</div>
</div>
