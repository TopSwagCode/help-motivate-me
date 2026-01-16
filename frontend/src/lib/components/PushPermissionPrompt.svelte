<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from 'svelte-i18n';
	import { browser } from '$app/environment';
	import { auth } from '$lib/stores/auth';
	import {
		isPushSupported,
		requestPushPermission,
		subscribeToPushNotifications,
		checkPushPermission
	} from '$lib/services/pushNotifications';

	let showPrompt = $state(false);
	let loading = $state(false);

	const STORAGE_KEY = 'push_prompt_state';
	const VISIT_COUNT_KEY = 'app_visit_count';
	const VISITS_BEFORE_PROMPT = 3;
	const DISMISS_DURATION_DAYS = 7;

	onMount(() => {
		if (!browser) return;
		
		// Track visits
		const visitCount = parseInt(localStorage.getItem(VISIT_COUNT_KEY) || '0', 10) + 1;
		localStorage.setItem(VISIT_COUNT_KEY, visitCount.toString());

		// Check if we should show the prompt
		checkShouldShowPrompt(visitCount);
	});

	async function checkShouldShowPrompt(visitCount: number) {
		// Need enough visits
		if (visitCount < VISITS_BEFORE_PROMPT) return;

		// Need to be logged in
		if (!$auth.user) return;

		// Need push support
		if (!isPushSupported()) return;

		// Check current permission
		const permission = checkPushPermission();
		
		// Don't show if already granted or denied
		if (permission === 'granted' || permission === 'denied') return;

		// Check if dismissed recently
		const storedState = localStorage.getItem(STORAGE_KEY);
		if (storedState) {
			try {
				const state = JSON.parse(storedState);
				if (state.dismissed) {
					const dismissedAt = new Date(state.dismissedAt);
					const daysSinceDismiss = (Date.now() - dismissedAt.getTime()) / (1000 * 60 * 60 * 24);
					if (daysSinceDismiss < DISMISS_DURATION_DAYS) {
						return;
					}
				}
			} catch {
				// Invalid state, continue
			}
		}

		// Show the prompt after a short delay
		setTimeout(() => {
			showPrompt = true;
		}, 2000);
	}

	async function handleEnable() {
		loading = true;
		try {
			const permission = await requestPushPermission();
			
			if (permission === 'granted') {
				await subscribeToPushNotifications();
				// Mark as enabled
				localStorage.setItem(STORAGE_KEY, JSON.stringify({ enabled: true }));
			}
			
			showPrompt = false;
		} catch (error) {
			console.error('Failed to enable push notifications:', error);
		} finally {
			loading = false;
		}
	}

	function handleDismiss() {
		localStorage.setItem(STORAGE_KEY, JSON.stringify({ 
			dismissed: true, 
			dismissedAt: new Date().toISOString() 
		}));
		showPrompt = false;
	}
</script>

{#if showPrompt}
	<div class="fixed inset-0 z-[200] flex items-end justify-center sm:items-center p-4 bg-black/30 backdrop-blur-sm animate-fade-in">
		<div class="w-full max-w-sm bg-white rounded-2xl shadow-2xl overflow-hidden animate-slide-up">
			<!-- Header with icon -->
			<div class="bg-gradient-to-r from-primary-500 to-primary-600 px-6 py-5 text-white">
				<div class="flex items-center gap-3">
					<div class="w-12 h-12 bg-white/20 rounded-xl flex items-center justify-center">
						<svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
								d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
						</svg>
					</div>
					<div>
						<h3 class="text-lg font-bold">{$t('pushPrompt.title')}</h3>
						<p class="text-sm text-white/80">{$t('pushPrompt.subtitle')}</p>
					</div>
				</div>
			</div>

			<!-- Content -->
			<div class="px-6 py-5">
				<p class="text-gray-600 text-sm leading-relaxed">
					{$t('pushPrompt.description')}
				</p>
				
				<ul class="mt-4 space-y-2">
					<li class="flex items-center gap-2 text-sm text-gray-700">
						<svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
						{$t('pushPrompt.benefit1')}
					</li>
					<li class="flex items-center gap-2 text-sm text-gray-700">
						<svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
						{$t('pushPrompt.benefit2')}
					</li>
					<li class="flex items-center gap-2 text-sm text-gray-700">
						<svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
						{$t('pushPrompt.benefit3')}
					</li>
				</ul>
			</div>

			<!-- Actions -->
			<div class="px-6 pb-6 flex gap-3">
				<button
					onclick={handleDismiss}
					class="flex-1 px-4 py-3 text-gray-600 font-medium rounded-xl hover:bg-gray-100 transition-colors touch-manipulation"
				>
					{$t('pushPrompt.notNow')}
				</button>
				<button
					onclick={handleEnable}
					disabled={loading}
					class="flex-1 px-4 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors touch-manipulation disabled:opacity-50 flex items-center justify-center gap-2"
				>
					{#if loading}
						<svg class="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
					{:else}
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
						</svg>
					{/if}
					{$t('pushPrompt.enable')}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	@keyframes fade-in {
		from { opacity: 0; }
		to { opacity: 1; }
	}
	
	@keyframes slide-up {
		from { 
			opacity: 0;
			transform: translateY(20px);
		}
		to { 
			opacity: 1;
			transform: translateY(0);
		}
	}
	
	.animate-fade-in {
		animation: fade-in 0.2s ease-out;
	}
	
	.animate-slide-up {
		animation: slide-up 0.3s ease-out;
	}
</style>
