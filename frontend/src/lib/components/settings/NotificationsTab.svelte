<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from 'svelte-i18n';
	import {
		disablePushNotifications,
		enablePushNotifications,
		getPushState,
		sendTestPushNotification
	} from '$lib/services/pushNotifications';

	let loading = $state(true);
	let testing = $state(false);
	let supported = $state(false);
	let enabled = $state(false);
	let permission = $state<NotificationPermission>('default');
	let registeredDevices = $state(0);
	let error = $state('');
	let testMessage = $state('');
	let testSucceeded = $state(false);

	onMount(refresh);

	async function refresh() {
		loading = true;
		try {
			const state = await getPushState();
			supported = state.supported;
			enabled = state.enabled;
			permission = state.permission;
			registeredDevices = state.registeredDevices;
		} catch (caught) {
			error = caught instanceof Error ? caught.message : $t('settings.notifications.push.errors.generic');
		} finally {
			loading = false;
		}
	}

	async function togglePush() {
		loading = true;
		error = '';
		testMessage = '';
		try {
			if (enabled) await disablePushNotifications();
			else await enablePushNotifications();
			await refresh();
		} catch (caught) {
			error = caught instanceof Error ? caught.message : $t('settings.notifications.push.errors.generic');
			await refresh();
		}
	}

	async function sendTest() {
		testing = true;
		error = '';
		testMessage = '';
		try {
			const result = await sendTestPushNotification();
			testSucceeded = result.successCount > 0 && result.failureCount === 0;
			if (result.totalSubscriptions === 0) {
				testMessage = $t('settings.notifications.test.noDevices');
			} else if (testSucceeded) {
				testMessage = $t('settings.notifications.test.success', {
					values: { count: result.successCount }
				});
			} else {
				testMessage = $t('settings.notifications.test.partial', {
					values: { success: result.successCount, failed: result.failureCount }
				});
			}
			await refresh();
		} catch (caught) {
			testSucceeded = false;
			error = caught instanceof Error ? caught.message : $t('settings.notifications.test.error');
		} finally {
			testing = false;
		}
	}
</script>

<div class="space-y-6">
	<div>
		<h2 class="mb-1 text-lg font-semibold text-cocoa-800">{$t('settings.notifications.title')}</h2>
		<p class="text-sm text-cocoa-500">{$t('settings.notifications.description')}</p>
	</div>

	<section class="overflow-hidden rounded-2xl border border-primary-100">
		<div class="border-b border-primary-100 bg-warm-cream px-4 py-3">
			<h3 class="font-medium text-cocoa-800">{$t('settings.notifications.device.title')}</h3>
		</div>
		<div class="p-4">
			<div class="flex items-center justify-between gap-4">
				<div>
					<p class="text-sm text-cocoa-600">{$t('settings.notifications.device.description')}</p>
					{#if !loading && !supported}
						<p class="mt-1 text-xs text-cocoa-500">{$t('settings.notifications.device.unsupported')}</p>
					{:else if permission === 'denied'}
						<p class="mt-1 text-xs text-red-600">{$t('settings.notifications.device.blocked')}</p>
					{:else if enabled && registeredDevices === 0}
						<p class="mt-1 text-xs text-amber-700">{$t('settings.notifications.device.notRegistered')}</p>
					{:else if enabled}
						<p class="mt-1 text-xs text-sage-600">
							{$t('settings.notifications.device.enabled', { values: { count: registeredDevices } })}
						</p>
					{:else if !loading}
						<p class="mt-1 text-xs text-cocoa-500">{$t('settings.notifications.device.disabled')}</p>
					{/if}
				</div>
				<button
					type="button"
					onclick={togglePush}
					disabled={loading || !supported || permission === 'denied'}
					class="relative h-6 w-11 flex-shrink-0 rounded-full transition-colors disabled:opacity-50 {enabled ? 'bg-primary-600' : 'bg-gray-300'}"
					aria-label={$t('settings.notifications.device.toggle')}
					aria-pressed={enabled}
				>
					<span class="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform {enabled ? 'translate-x-5' : ''}"></span>
				</button>
			</div>
		</div>
	</section>

	<section class="overflow-hidden rounded-2xl border border-primary-100">
		<div class="border-b border-primary-100 bg-warm-cream px-4 py-3">
			<h3 class="font-medium text-cocoa-800">{$t('settings.notifications.test.title')}</h3>
		</div>
		<div class="flex flex-col items-start gap-4 p-4 sm:flex-row sm:items-center sm:justify-between">
			<div>
				<p class="text-sm text-cocoa-600">{$t('settings.notifications.test.description')}</p>
				{#if testMessage}
					<p class="mt-1 text-xs {testSucceeded ? 'text-sage-600' : 'text-amber-700'}" role="status">
						{testMessage}
					</p>
				{/if}
			</div>
			<button
				type="button"
				onclick={sendTest}
				disabled={testing || loading || !enabled || registeredDevices === 0}
				class="btn btn-primary flex-shrink-0"
			>
				{testing ? $t('settings.notifications.test.sending') : $t('settings.notifications.test.button')}
			</button>
		</div>
	</section>

	{#if error}
		<p class="text-sm text-red-600" role="alert">{error}</p>
	{/if}
</div>