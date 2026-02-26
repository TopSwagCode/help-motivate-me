<script lang="ts">
	import { t } from 'svelte-i18n';
	import {
		requestPushPermission,
		subscribeToPushNotifications
	} from '$lib/services/pushNotifications';

	let { isOpen, onClose }: { isOpen: boolean; onClose: () => void } = $props();
	let loading = $state(false);

	async function handleEnable() {
		loading = true;
		try {
			const permission = await requestPushPermission();

			if (permission === 'granted') {
				await subscribeToPushNotifications();
			}

			localStorage.setItem('notification_fab_dismissed', 'true');
			onClose();
		} catch {
			// Failed to enable push notifications
		} finally {
			loading = false;
		}
	}

	function handleDismiss() {
		localStorage.setItem('notification_fab_dismissed', 'true');
		onClose();
	}
</script>

{#if isOpen}
	<div
		class="fixed inset-0 z-[200] flex items-end justify-center sm:items-center p-4 bg-black/30 backdrop-blur-sm animate-fade-in"
		role="dialog"
		aria-modal="true"
	>
		<div class="w-full max-w-sm bg-warm-paper rounded-2xl shadow-2xl overflow-hidden animate-slide-up">
			<!-- Header with icon -->
			<div class="bg-gradient-to-r from-primary-500 to-primary-600 px-6 py-5 text-white">
				<div class="flex items-center gap-3">
					<div class="w-12 h-12 bg-warm-paper/20 rounded-xl flex items-center justify-center">
						<svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
							/>
						</svg>
					</div>
					<div>
						<h3 class="text-lg font-bold">{$t('notificationFab.title')}</h3>
					</div>
				</div>
			</div>

			<!-- Content -->
			<div class="px-6 py-5">
				<p class="text-cocoa-600 text-sm leading-relaxed">
					{$t('notificationFab.body')}
				</p>
				<p class="text-cocoa-500 text-xs mt-3 leading-relaxed">
					{$t('notificationFab.settingsNote')}
				</p>
			</div>

			<!-- Actions -->
			<div class="px-6 pb-6 flex gap-3">
				<button
					onclick={handleDismiss}
					class="flex-1 px-4 py-3 text-cocoa-600 font-medium rounded-xl hover:bg-primary-50 transition-colors touch-manipulation"
				>
					{$t('notificationFab.dismiss')}
				</button>
				<button
					onclick={handleEnable}
					disabled={loading}
					class="flex-1 px-4 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors touch-manipulation disabled:opacity-50 flex items-center justify-center gap-2"
				>
					{#if loading}
						<svg class="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
						</svg>
					{:else}
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
							/>
						</svg>
					{/if}
					{$t('notificationFab.enable')}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	@keyframes fade-in {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
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
