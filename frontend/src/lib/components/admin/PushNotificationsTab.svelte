<script lang="ts">
	import { t } from 'svelte-i18n';
	import {
		getPushStats,
		sendPushToAll,
		clearAllPushSubscriptions,
		type PushStats,
		type PushNotificationResult
	} from '$lib/api/admin';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let pushStats = $state<PushStats | null>(null);
	let loading = $state(true);
	let error = $state('');

	// Push notification form
	let pushTitle = $state('');
	let pushBody = $state('');
	let pushUrl = $state('');
	let pushSending = $state(false);
	let pushResult = $state<{ success: boolean; message: string } | null>(null);

	// Clear all subscriptions
	let showClearConfirm = $state(false);
	let clearing = $state(false);
	let clearResult = $state<{ success: boolean; message: string } | null>(null);

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			pushStats = await getPushStats().catch(() => null);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load push stats';
		} finally {
			loading = false;
		}
	}

	async function handleClearAll() {
		clearing = true;
		clearResult = null;
		try {
			const result = await clearAllPushSubscriptions();
			clearResult = { success: true, message: result.message };
			pushStats = await getPushStats().catch(() => null);
		} catch (err) {
			clearResult = {
				success: false,
				message: err instanceof Error ? err.message : 'Failed to clear subscriptions'
			};
		} finally {
			clearing = false;
			showClearConfirm = false;
		}
	}

	async function handleSendPushToAll(e: Event) {
		e.preventDefault();
		if (!pushTitle.trim() || !pushBody.trim()) return;

		pushSending = true;
		pushResult = null;
		try {
			const result = await sendPushToAll(
				pushTitle.trim(),
				pushBody.trim(),
				pushUrl.trim() || undefined
			);
			pushResult = {
				success: true,
				message: `Sent to ${result.successCount} subscriptions (${result.failureCount} failed)`
			};
			// Refresh push stats
			pushStats = await getPushStats().catch(() => null);
			// Clear form on success
			pushTitle = '';
			pushBody = '';
			pushUrl = '';
		} catch (err) {
			pushResult = {
				success: false,
				message: err instanceof Error ? err.message : 'Failed to send push notification'
			};
		} finally {
			pushSending = false;
		}
	}
</script>

{#if loading}
	<div class="flex justify-center py-12">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{:else if error}
	<div class="card">
		<ErrorState message={error} onRetry={loadData} size="md" />
	</div>
{:else}
	<!-- Push Notifications Header -->
	<div class="mb-6">
		<h2 class="text-lg font-semibold text-cocoa-800">Push Notifications</h2>
		<p class="text-cocoa-500 mt-1">
			{#if pushStats}
				<span class="inline-flex items-center gap-1.5">
					<span class="w-2 h-2 bg-green-500 rounded-full"></span>
					{pushStats.usersWithPush} users with push enabled ({pushStats.totalSubscriptions} subscriptions)
				</span>
			{:else}
				<span class="inline-flex items-center gap-1.5">
					<span class="w-2 h-2 bg-gray-400 rounded-full"></span>
					Push notifications not configured
				</span>
			{/if}
		</p>
	</div>

	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
		<!-- Send Push Notification Form -->
		<div class="card p-6">
			<h3 class="text-lg font-semibold text-cocoa-800 mb-4">Send Push Notification to All Users</h3>

			<form onsubmit={handleSendPushToAll} class="space-y-4">
				<div>
					<label for="push-title" class="block text-sm font-medium text-cocoa-700 mb-1">
						Title <span class="text-red-500">*</span>
					</label>
					<input
						id="push-title"
						type="text"
						bind:value={pushTitle}
						placeholder="Notification title"
						class="input w-full"
						required
						maxlength="100"
					/>
				</div>

				<div>
					<label for="push-body" class="block text-sm font-medium text-cocoa-700 mb-1">
						Message <span class="text-red-500">*</span>
					</label>
					<textarea
						id="push-body"
						bind:value={pushBody}
						placeholder="Notification message"
						class="input w-full h-24 resize-none"
						required
						maxlength="500"
					></textarea>
				</div>

				<div>
					<label for="push-url" class="block text-sm font-medium text-cocoa-700 mb-1">
						Click URL (optional)
					</label>
					<input
						id="push-url"
						type="text"
						bind:value={pushUrl}
						placeholder="/today or https://example.com"
						class="input w-full"
					/>
					<p class="text-xs text-cocoa-500 mt-1">Where users go when they click the notification</p>
				</div>

				{#if pushResult}
					<div class="p-3 rounded-2xl {pushResult.success ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}">
						{pushResult.message}
					</div>
				{/if}

				<button
					type="submit"
					disabled={pushSending || !pushTitle.trim() || !pushBody.trim() || !pushStats || pushStats.usersWithPush === 0}
					class="btn btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed"
				>
					{#if pushSending}
						<svg class="animate-spin h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						Sending...
					{:else}
						<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
						</svg>
						Send to All ({pushStats?.usersWithPush ?? 0} users)
					{/if}
				</button>
			</form>
		</div>

		<!-- Push Stats Card -->
		<div class="card p-6">
			<h3 class="text-lg font-semibold text-cocoa-800 mb-4">Push Notification Stats</h3>

			{#if pushStats}
				<div class="space-y-4">
					<div class="flex items-center justify-between p-3 bg-warm-cream rounded-2xl">
						<div class="flex items-center gap-3">
							<div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
								<svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
								</svg>
							</div>
							<div>
								<p class="text-sm text-cocoa-500">Users with Push Enabled</p>
								<p class="text-xl font-bold text-cocoa-800">{pushStats.usersWithPush}</p>
							</div>
						</div>
						<div class="text-right">
							<p class="text-2xl font-bold text-blue-600">{pushStats.percentageWithPush.toFixed(0)}%</p>
							<p class="text-xs text-cocoa-500">of all users</p>
						</div>
					</div>

					<div class="flex items-center justify-between p-3 bg-warm-cream rounded-2xl">
						<div class="flex items-center gap-3">
							<div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
								<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
								</svg>
							</div>
							<div>
								<p class="text-sm text-cocoa-500">Total Subscriptions</p>
								<p class="text-xl font-bold text-cocoa-800">{pushStats.totalSubscriptions}</p>
							</div>
						</div>
						<p class="text-xs text-cocoa-500">Across all devices</p>
					</div>

					{#if clearResult}
						<div class="p-3 rounded-2xl {clearResult.success ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}">
							{clearResult.message}
						</div>
					{/if}

					<div class="pt-4 border-t border-primary-100 space-y-3">
						<p class="text-sm text-cocoa-500">
							Users can have multiple subscriptions if they enable push notifications on different devices or browsers.
						</p>
						<button
							onclick={() => showClearConfirm = true}
							disabled={!pushStats || pushStats.totalSubscriptions === 0}
							class="btn w-full bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
						>
							<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
							</svg>
							Clear All Subscriptions
						</button>
					</div>
				</div>
			{:else}
				<div class="text-center py-8">
					<div class="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
						<svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
						</svg>
					</div>
					<p class="text-cocoa-500 mb-2">Push notifications not configured</p>
					<p class="text-sm text-gray-400">
						Configure VAPID keys in appsettings to enable push notifications.
					</p>
				</div>
			{/if}
		</div>
	</div>
{/if}

{#if showClearConfirm}
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
		onclick={() => showClearConfirm = false}
		role="presentation"
	>
		<div
			class="bg-warm-paper rounded-2xl shadow-xl max-w-md w-full p-6"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.key === 'Escape' && (showClearConfirm = false)}
			role="dialog"
			aria-modal="true"
			tabindex="-1"
		>
			<div class="flex items-center gap-3 mb-4">
				<div class="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
					</svg>
				</div>
				<h3 class="text-lg font-semibold text-cocoa-800">Clear All Subscriptions</h3>
			</div>

			<p class="text-cocoa-600 mb-6">
				This will permanently delete all <strong>{pushStats?.totalSubscriptions ?? 0}</strong> push subscriptions. Users will need to re-subscribe to receive push notifications. This action cannot be undone.
			</p>

			<div class="flex gap-3 justify-end">
				<button
					onclick={() => showClearConfirm = false}
					class="btn bg-gray-100 text-cocoa-700 hover:bg-gray-200"
				>
					Cancel
				</button>
				<button
					onclick={handleClearAll}
					disabled={clearing}
					class="btn bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
				>
					{#if clearing}
						<svg class="animate-spin h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						Clearing...
					{:else}
						Yes, Clear All
					{/if}
				</button>
			</div>
		</div>
	</div>
{/if}
