<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { t } from 'svelte-i18n';
	import { pwaStore } from '$lib/stores/pwa';

	type ErrorType = 'notFound' | 'serverError' | 'offline' | 'serverUnavailable';

	function getErrorType(status: number, error: App.Error | null, isOnline: boolean): ErrorType {
		if (!isOnline) return 'offline';
		if (status === 404) return 'notFound';
		if (error?.message?.includes('fetch')) return 'serverUnavailable';
		return 'serverError';
	}

	let errorType = $derived(getErrorType($page.status, $page.error, $pwaStore.isOnline));

	function goBack() {
		history.back();
	}

	function goHome() {
		goto('/');
	}

	function tryAgain() {
		location.reload();
	}
</script>

<svelte:head>
	<title>{$t(`errorPages.${errorType}.title`)} - Help Motivate Me</title>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-gray-50 flex items-center justify-center px-4">
	<div class="max-w-md w-full">
		<div class="bg-white rounded-2xl shadow-lg border border-gray-200 p-8 text-center">
			<!-- Status Code / Icon -->
			<div class="mb-6">
				{#if errorType === 'offline'}
					<div class="w-20 h-20 mx-auto bg-yellow-100 rounded-full flex items-center justify-center">
						<svg class="w-10 h-10 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M15.536 8.464a5 5 0 010 7.072m0 0l-2.829-2.829m-4.243 2.829a4.978 4.978 0 01-1.414-2.83m-1.414 5.658a9 9 0 01-2.167-9.238m7.824 2.167a1 1 0 111.414 1.414m-1.414-1.414L3 3m8.293 8.293l1.414 1.414" />
						</svg>
					</div>
				{:else if errorType === 'serverUnavailable'}
					<div class="w-20 h-20 mx-auto bg-orange-100 rounded-full flex items-center justify-center">
						<svg class="w-10 h-10 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01" />
						</svg>
					</div>
				{:else if errorType === 'notFound'}
					<div class="text-7xl font-bold text-indigo-200">
						{$t('errorPages.notFound.code')}
					</div>
				{:else}
					<div class="w-20 h-20 mx-auto bg-red-100 rounded-full flex items-center justify-center">
						<svg class="w-10 h-10 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
						</svg>
					</div>
				{/if}
			</div>

			<!-- Title -->
			<h1 class="text-2xl font-bold text-gray-900 mb-2">
				{$t(`errorPages.${errorType}.title`)}
			</h1>

			<!-- Message -->
			<p class="text-gray-600 mb-2">
				{$t(`errorPages.${errorType}.message`)}
			</p>

			<!-- Suggestion -->
			<p class="text-gray-500 text-sm mb-8">
				{$t(`errorPages.${errorType}.suggestion`)}
			</p>

			<!-- Action Buttons -->
			<div class="flex flex-col sm:flex-row gap-3 justify-center">
				<button
					onclick={goHome}
					class="inline-flex items-center justify-center px-5 py-2.5 border border-transparent text-sm font-medium rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
				>
					{$t('errorPages.actions.goHome')}
				</button>

				{#if errorType === 'offline' || errorType === 'serverUnavailable' || errorType === 'serverError'}
					<button
						onclick={tryAgain}
						class="inline-flex items-center justify-center px-5 py-2.5 border border-gray-300 text-sm font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 transition-colors"
					>
						{$t('errorPages.actions.tryAgain')}
					</button>
				{:else}
					<button
						onclick={goBack}
						class="inline-flex items-center justify-center px-5 py-2.5 border border-gray-300 text-sm font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 transition-colors"
					>
						{$t('errorPages.actions.goBack')}
					</button>
				{/if}
			</div>
		</div>

		<!-- Help Link -->
		<div class="mt-6 text-center text-sm text-gray-500">
			<a href="/faq" class="text-indigo-600 hover:text-indigo-700">
				{$t('common.info.visitFaq')}
			</a>
		</div>
	</div>
</div>
