<script lang="ts">
	import { t } from 'svelte-i18n';
	import { onMount } from 'svelte';
	import { getNotificationPreferences, updateNotificationPreferences } from '$lib/api/notifications';
	import type { NotificationPreferences, UpdateNotificationPreferencesRequest } from '$lib/types';
	import { NotificationDays } from '$lib/types';
	import {
		isPushSupported,
		isSubscribedToPush,
		subscribeToPushNotifications,
		unsubscribeFromPushNotifications,
		requestPushPermission,
		checkPushPermission,
		getPushStatus
	} from '$lib/services/pushNotifications';

	let loading = $state(true);
	let saving = $state(false);
	let error = $state('');
	let preferences = $state<NotificationPreferences | null>(null);

	// Push notification state
	let pushSupported = $state(false);
	let pushPermission = $state<NotificationPermission>('default');
	let pushEnabled = $state(false);
	let pushLoading = $state(false);
	let totalDevices = $state(0);

	// Day flags
	const days = [
		{ key: 'monday', flag: NotificationDays.Monday },
		{ key: 'tuesday', flag: NotificationDays.Tuesday },
		{ key: 'wednesday', flag: NotificationDays.Wednesday },
		{ key: 'thursday', flag: NotificationDays.Thursday },
		{ key: 'friday', flag: NotificationDays.Friday },
		{ key: 'saturday', flag: NotificationDays.Saturday },
		{ key: 'sunday', flag: NotificationDays.Sunday }
	];

	// Time slots
	const timeSlots = [
		{ id: 'Morning', labelKey: 'morning', timeKey: 'morningTime' },
		{ id: 'Afternoon', labelKey: 'afternoon', timeKey: 'afternoonTime' },
		{ id: 'Evening', labelKey: 'evening', timeKey: 'eveningTime' },
		{ id: 'Custom', labelKey: 'custom', timeKey: null }
	] as const;

	// Notification types configuration
	const notificationTypes = [
		{ key: 'habitReminders', field: 'habitRemindersEnabled' as const },
		{ key: 'goalReminders', field: 'goalRemindersEnabled' as const },
		{ key: 'dailyDigest', field: 'dailyDigestEnabled' as const },
		{ key: 'streakAlerts', field: 'streakAlertsEnabled' as const },
		{ key: 'motivationalQuotes', field: 'motivationalQuotesEnabled' as const },
		{ key: 'weeklyReview', field: 'weeklyReviewEnabled' as const },
		{ key: 'buddyUpdates', field: 'buddyUpdatesEnabled' as const }
	];

	let detectedTimezone = $state('');

	onMount(async () => {
		// Detect timezone
		try {
			detectedTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
		} catch {
			detectedTimezone = 'UTC';
		}

		// Check push notification support and status
		pushSupported = isPushSupported();
		if (pushSupported) {
			pushPermission = await checkPushPermission();
			pushEnabled = await isSubscribedToPush();
			// Get total devices from backend
			const status = await getPushStatus();
			totalDevices = status.subscriptionCount;
		}

		await loadPreferences();
	});

	async function loadPreferences() {
		loading = true;
		error = '';
		try {
			preferences = await getNotificationPreferences();

			// Set timezone if not already set
			if (preferences && preferences.timezoneId === 'UTC' && detectedTimezone !== 'UTC') {
				await savePreference({
					timezoneId: detectedTimezone,
					utcOffsetMinutes: -new Date().getTimezoneOffset()
				});
			}
		} catch (e) {
			error = e instanceof Error ? e.message : $t('settings.notifications.errors.loadFailed');
		} finally {
			loading = false;
		}
	}

	async function savePreference(data: UpdateNotificationPreferencesRequest) {
		if (!preferences) return;

		saving = true;
		error = '';

		try {
			preferences = await updateNotificationPreferences(data);
		} catch (e) {
			error = e instanceof Error ? e.message : $t('settings.notifications.errors.saveFailed');
		} finally {
			saving = false;
		}
	}

	// Toggle handlers
	async function toggleMaster() {
		if (!preferences) return;
		await savePreference({ notificationsEnabled: !preferences.notificationsEnabled });
	}

	async function toggleChannel(channel: 'emailEnabled' | 'smsEnabled') {
		if (!preferences) return;
		await savePreference({ [channel]: !preferences[channel] });
	}

	async function toggleNotificationType(field: keyof NotificationPreferences) {
		if (!preferences) return;
		await savePreference({ [field]: !preferences[field] });
	}

	// Push notification toggle
	async function togglePush() {
		if (!pushSupported) return;
		
		pushLoading = true;
		error = '';
		
		try {
			if (pushEnabled) {
				// Unsubscribe
				const success = await unsubscribeFromPushNotifications();
				if (success) {
					pushEnabled = false;
					const status = await getPushStatus();
					totalDevices = status.subscriptionCount;
				} else {
					error = $t('settings.notifications.push.errors.unsubscribeFailed');
				}
			} else {
				// First request permission if needed
				if (pushPermission !== 'granted') {
					pushPermission = await requestPushPermission();
					if (pushPermission !== 'granted') {
						error = $t('settings.notifications.push.errors.permissionDenied');
						pushLoading = false;
						return;
					}
				}
				
				// Subscribe
				const success = await subscribeToPushNotifications();
				if (success) {
					pushEnabled = true;
					const status = await getPushStatus();
					totalDevices = status.subscriptionCount;
				} else {
					error = $t('settings.notifications.push.errors.subscribeFailed');
				}
			}
		} catch (e) {
			error = e instanceof Error ? e.message : $t('settings.notifications.push.errors.generic');
		} finally {
			pushLoading = false;
		}
	}

	// Day selection
	function isDaySelected(dayFlag: number): boolean {
		if (!preferences) return false;
		return (preferences.selectedDays & dayFlag) !== 0;
	}

	async function toggleDay(dayFlag: number) {
		if (!preferences) return;
		const newDays = preferences.selectedDays ^ dayFlag;
		await savePreference({ selectedDays: newDays });
	}

	async function selectDayPreset(preset: 'all' | 'weekdays' | 'weekend') {
		if (!preferences) return;
		let newDays: number;
		switch (preset) {
			case 'all':
				newDays = NotificationDays.All;
				break;
			case 'weekdays':
				newDays = NotificationDays.Weekdays;
				break;
			case 'weekend':
				newDays = NotificationDays.Weekend;
				break;
		}
		await savePreference({ selectedDays: newDays });
	}

	// Time slot selection
	async function selectTimeSlot(slot: string) {
		if (!preferences) return;
		await savePreference({ preferredTimeSlot: slot });
	}

	// Custom time handlers
	async function updateCustomTimeStart(event: Event) {
		const target = event.target as HTMLInputElement;
		await savePreference({ customTimeStart: target.value || null });
	}

	async function updateCustomTimeEnd(event: Event) {
		const target = event.target as HTMLInputElement;
		await savePreference({ customTimeEnd: target.value || null });
	}

	// Check if all notification types are enabled
	let allTypesEnabled = $derived(
		preferences
			? preferences.habitRemindersEnabled &&
				preferences.goalRemindersEnabled &&
				preferences.dailyDigestEnabled &&
				preferences.streakAlertsEnabled &&
				preferences.motivationalQuotesEnabled &&
				preferences.weeklyReviewEnabled &&
				preferences.buddyUpdatesEnabled
			: false
	);

	async function toggleAllTypes() {
		if (!preferences) return;
		const newValue = !allTypesEnabled;
		await savePreference({
			habitRemindersEnabled: newValue,
			goalRemindersEnabled: newValue,
			dailyDigestEnabled: newValue,
			streakAlertsEnabled: newValue,
			motivationalQuotesEnabled: newValue,
			weeklyReviewEnabled: newValue,
			buddyUpdatesEnabled: newValue
		});
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-gray-900 mb-2">{$t('settings.notifications.title')}</h2>
	<p class="text-sm text-gray-600 mb-6">{$t('settings.notifications.description')}</p>

	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
			{error}
		</div>
	{/if}

	{#if loading}
		<div class="flex justify-center py-8">
			<div
				class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
			></div>
		</div>
	{:else if preferences}
		<!-- Master Toggle -->
		<div class="mb-8">
			<button
				onclick={toggleMaster}
				disabled={saving}
				class="w-full flex items-center justify-between p-4 rounded-lg border-2 transition-all duration-200 {preferences.notificationsEnabled
					? 'border-primary-500 bg-primary-50'
					: 'border-gray-200 bg-white hover:border-gray-300'}"
			>
				<div class="flex-1 text-left">
					<h3 class="font-medium text-gray-900">{$t('settings.notifications.master.label')}</h3>
					<p class="text-sm text-gray-500">{$t('settings.notifications.master.description')}</p>
				</div>
				<div
					class="relative w-12 h-6 rounded-full transition-colors duration-200 {preferences.notificationsEnabled
						? 'bg-primary-600'
						: 'bg-gray-300'}"
				>
					<div
						class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow-sm transition-transform duration-200 {preferences.notificationsEnabled
							? 'translate-x-6'
							: 'translate-x-0.5'}"
					></div>
				</div>
			</button>
		</div>

		{#if preferences.notificationsEnabled}
			<!-- Delivery Channels -->
			<div class="mb-8">
				<h3 class="text-sm font-medium text-gray-900 mb-3">
					{$t('settings.notifications.channels.title')}
				</h3>
				<div class="space-y-2">
					<!-- Email -->
					<button
						onclick={() => toggleChannel('emailEnabled')}
						disabled={saving}
						class="w-full flex items-center justify-between p-3 rounded-lg border transition-all duration-200 {preferences.emailEnabled
							? 'border-primary-300 bg-primary-50'
							: 'border-gray-200 bg-white hover:border-gray-300'}"
					>
						<div class="flex items-center gap-3">
							<div
								class="w-8 h-8 rounded-full flex items-center justify-center {preferences.emailEnabled
									? 'bg-primary-100 text-primary-600'
									: 'bg-gray-100 text-gray-500'}"
							>
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
									/>
								</svg>
							</div>
							<div class="text-left">
								<p class="font-medium text-gray-900">{$t('settings.notifications.channels.email')}</p>
								<p class="text-xs text-gray-500">
									{$t('settings.notifications.channels.emailDescription')}
								</p>
							</div>
						</div>
						<div
							class="w-10 h-5 rounded-full transition-colors duration-200 {preferences.emailEnabled
								? 'bg-primary-600'
								: 'bg-gray-300'}"
						>
							<div
								class="w-4 h-4 mt-0.5 bg-white rounded-full shadow-sm transition-transform duration-200 {preferences.emailEnabled
									? 'translate-x-5'
									: 'translate-x-0.5'}"
							></div>
						</div>
					</button>

					<!-- SMS -->
					<button
						onclick={() => toggleChannel('smsEnabled')}
						disabled={saving}
						class="w-full flex items-center justify-between p-3 rounded-lg border transition-all duration-200 {preferences.smsEnabled
							? 'border-primary-300 bg-primary-50'
							: 'border-gray-200 bg-white hover:border-gray-300'}"
					>
						<div class="flex items-center gap-3">
							<div
								class="w-8 h-8 rounded-full flex items-center justify-center {preferences.smsEnabled
									? 'bg-primary-100 text-primary-600'
									: 'bg-gray-100 text-gray-500'}"
							>
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
									/>
								</svg>
							</div>
							<div class="text-left">
								<p class="font-medium text-gray-900">{$t('settings.notifications.channels.sms')}</p>
								<p class="text-xs text-gray-500">
									{$t('settings.notifications.channels.smsDescription')}
								</p>
							</div>
						</div>
						<div
							class="w-10 h-5 rounded-full transition-colors duration-200 {preferences.smsEnabled
								? 'bg-primary-600'
								: 'bg-gray-300'}"
						>
							<div
								class="w-4 h-4 mt-0.5 bg-white rounded-full shadow-sm transition-transform duration-200 {preferences.smsEnabled
									? 'translate-x-5'
									: 'translate-x-0.5'}"
							></div>
						</div>
					</button>

					<!-- Push Notifications -->
					{#if pushSupported}
						<div class="space-y-2">
							<button
								onclick={togglePush}
								disabled={pushLoading}
								class="w-full flex items-center justify-between p-3 rounded-lg border transition-all duration-200 {pushEnabled
									? 'border-primary-300 bg-primary-50'
									: 'border-gray-200 bg-white hover:border-gray-300'}"
							>
								<div class="flex items-center gap-3">
									<div
										class="w-8 h-8 rounded-full flex items-center justify-center {pushEnabled
											? 'bg-primary-100 text-primary-600'
											: 'bg-gray-100 text-gray-500'}"
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path
												stroke-linecap="round"
												stroke-linejoin="round"
												stroke-width="2"
												d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
											/>
										</svg>
									</div>
									<div class="text-left">
										<p class="font-medium text-gray-900">{$t('settings.notifications.channels.push')}</p>
										<p class="text-xs text-gray-500">
											{#if pushPermission === 'denied'}
												{$t('settings.notifications.channels.pushBlocked')}
											{:else if pushEnabled}
												{$t('settings.notifications.channels.pushEnabled')} · {totalDevices} {totalDevices === 1 ? 'device' : 'devices'}
											{:else}
												{$t('settings.notifications.channels.pushDescription')}
											{/if}
										</p>
									</div>
								</div>
								{#if pushLoading}
									<div class="animate-spin w-5 h-5 border-2 border-primary-600 border-t-transparent rounded-full"></div>
								{:else}
									<div
										class="w-10 h-5 rounded-full transition-colors duration-200 {pushEnabled
											? 'bg-primary-600'
										: 'bg-gray-300'} {pushPermission === 'denied' ? 'opacity-50' : ''}"
								>
									<div
										class="w-4 h-4 mt-0.5 bg-white rounded-full shadow-sm transition-transform duration-200 {pushEnabled
											? 'translate-x-5'
											: 'translate-x-0.5'}"
									></div>
								</div>
							{/if}
						</button>
						{#if totalDevices > 0}
							<p class="text-xs text-gray-500 px-3 py-1 bg-gray-50 rounded">
								💡 Push notifications are enabled on <strong>{totalDevices}</strong> {totalDevices === 1 ? 'device' : 'devices'}. 
								Enable on each device separately via Settings → Notifications.
							</p>
						{:else if pushEnabled}
							<p class="text-xs text-amber-600 px-3 py-1 bg-amber-50 rounded">
								⚠️ This device is subscribed but not registered. Try toggling off and on again.
							</p>
						{/if}
						</div>
					{/if}

					<!-- Phone (Coming Soon) -->
					<div
						class="w-full flex items-center justify-between p-3 rounded-lg border border-gray-200 bg-gray-50 opacity-60"
					>
						<div class="flex items-center gap-3">
							<div class="w-8 h-8 rounded-full flex items-center justify-center bg-gray-100 text-gray-400">
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
									/>
								</svg>
							</div>
							<div class="text-left">
								<p class="font-medium text-gray-500">{$t('settings.notifications.channels.phone')}</p>
								<p class="text-xs text-gray-400">
									{$t('settings.notifications.channels.phoneDescription')}
								</p>
							</div>
						</div>
						<span
							class="px-2 py-0.5 text-xs font-medium bg-gray-200 text-gray-600 rounded-full"
						>
							{$t('settings.notifications.channels.comingSoon')}
						</span>
					</div>
				</div>
			</div>

			<!-- Notification Types -->
			<div class="mb-8">
				<div class="flex items-center justify-between mb-3">
					<h3 class="text-sm font-medium text-gray-900">
						{$t('settings.notifications.types.title')}
					</h3>
					<button
						onclick={toggleAllTypes}
						disabled={saving}
						class="text-xs font-medium text-primary-600 hover:text-primary-700"
					>
						{allTypesEnabled ? 'Disable All' : 'Enable All'}
					</button>
				</div>
				<div class="space-y-2">
					{#each notificationTypes as type}
						<button
							onclick={() => toggleNotificationType(type.field)}
							disabled={saving}
							class="w-full flex items-center justify-between p-3 rounded-lg border transition-all duration-200 {preferences[
								type.field
							]
								? 'border-primary-300 bg-primary-50'
								: 'border-gray-200 bg-white hover:border-gray-300'}"
						>
							<div class="text-left">
								<p class="font-medium text-gray-900">
									{$t(`settings.notifications.types.${type.key}`)}
								</p>
								<p class="text-xs text-gray-500">
									{$t(`settings.notifications.types.${type.key}Description`)}
								</p>
							</div>
							<div
								class="w-10 h-5 rounded-full transition-colors duration-200 flex-shrink-0 ml-3 {preferences[
									type.field
								]
									? 'bg-primary-600'
									: 'bg-gray-300'}"
							>
								<div
									class="w-4 h-4 mt-0.5 bg-white rounded-full shadow-sm transition-transform duration-200 {preferences[
										type.field
									]
										? 'translate-x-5'
										: 'translate-x-0.5'}"
								></div>
							</div>
						</button>
					{/each}
				</div>
			</div>

			<!-- Schedule Section -->
			<div class="mb-6">
				<h3 class="text-sm font-medium text-gray-900 mb-1">
					{$t('settings.notifications.schedule.title')}
				</h3>
				<p class="text-xs text-gray-500 mb-4">
					{$t('settings.notifications.schedule.description')}
				</p>

				<!-- Days Selection -->
				<div class="mb-6">
					<p class="text-xs font-medium text-gray-700 mb-2">
						{$t('settings.notifications.schedule.days.title')}
					</p>

					<!-- Quick select buttons -->
					<div class="flex gap-2 mb-3">
						<button
							onclick={() => selectDayPreset('all')}
							disabled={saving}
							class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors {preferences.selectedDays ===
							NotificationDays.All
								? 'bg-primary-600 text-white'
								: 'bg-gray-100 text-gray-600 hover:bg-gray-200'}"
						>
							{$t('settings.notifications.schedule.days.selectAll')}
						</button>
						<button
							onclick={() => selectDayPreset('weekdays')}
							disabled={saving}
							class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors {preferences.selectedDays ===
							NotificationDays.Weekdays
								? 'bg-primary-600 text-white'
								: 'bg-gray-100 text-gray-600 hover:bg-gray-200'}"
						>
							{$t('settings.notifications.schedule.days.weekdays')}
						</button>
						<button
							onclick={() => selectDayPreset('weekend')}
							disabled={saving}
							class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors {preferences.selectedDays ===
							NotificationDays.Weekend
								? 'bg-primary-600 text-white'
								: 'bg-gray-100 text-gray-600 hover:bg-gray-200'}"
						>
							{$t('settings.notifications.schedule.days.weekend')}
						</button>
					</div>

					<!-- Individual day toggles -->
					<div class="flex gap-1 flex-wrap">
						{#each days as day}
							<button
								onclick={() => toggleDay(day.flag)}
								disabled={saving}
								class="w-10 h-10 rounded-lg text-xs font-medium transition-colors {isDaySelected(
									day.flag
								)
									? 'bg-primary-600 text-white'
									: 'bg-gray-100 text-gray-600 hover:bg-gray-200'}"
							>
								{$t(`settings.notifications.schedule.days.${day.key}`)}
							</button>
						{/each}
					</div>
				</div>

				<!-- Time Slot Selection -->
				<div class="mb-6">
					<p class="text-xs font-medium text-gray-700 mb-2">
						{$t('settings.notifications.schedule.timeSlot.title')}
					</p>
					<div class="grid grid-cols-2 gap-2">
						{#each timeSlots as slot}
							<button
								onclick={() => selectTimeSlot(slot.id)}
								disabled={saving}
								class="p-3 rounded-lg border-2 text-left transition-all duration-200 {preferences.preferredTimeSlot ===
								slot.id
									? 'border-primary-500 bg-primary-50'
									: 'border-gray-200 bg-white hover:border-gray-300'}"
							>
								<p class="font-medium text-gray-900 text-sm">
									{$t(`settings.notifications.schedule.timeSlot.${slot.labelKey}`)}
								</p>
								{#if slot.timeKey}
									<p class="text-xs text-gray-500">
										{$t(`settings.notifications.schedule.timeSlot.${slot.timeKey}`)}
									</p>
								{/if}
							</button>
						{/each}
					</div>

					<!-- Custom time inputs -->
					{#if preferences.preferredTimeSlot === 'Custom'}
						<div class="mt-3 flex gap-3">
							<div class="flex-1">
								<label for="customTimeStart" class="block text-xs font-medium text-gray-700 mb-1">
									{$t('settings.notifications.schedule.timeSlot.customStart')}
								</label>
								<input
									id="customTimeStart"
									type="time"
									value={preferences.customTimeStart || ''}
									onchange={updateCustomTimeStart}
									disabled={saving}
									class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
								/>
							</div>
							<div class="flex-1">
								<label for="customTimeEnd" class="block text-xs font-medium text-gray-700 mb-1">
									{$t('settings.notifications.schedule.timeSlot.customEnd')}
								</label>
								<input
									id="customTimeEnd"
									type="time"
									value={preferences.customTimeEnd || ''}
									onchange={updateCustomTimeEnd}
									disabled={saving}
									class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
								/>
							</div>
						</div>
					{/if}
				</div>

				<!-- Timezone -->
				<div class="mb-4">
					<p class="text-xs font-medium text-gray-700 mb-1">
						{$t('settings.notifications.schedule.timezone.title')}
					</p>
					<div class="flex items-center gap-2 text-sm text-gray-600">
						<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
							/>
						</svg>
						<span>{$t('settings.notifications.schedule.timezone.detected')}: {preferences.timezoneId}</span>
					</div>
				</div>

				<!-- Estimated timing note -->
				<div class="p-3 bg-amber-50 border border-amber-200 rounded-lg">
					<p class="text-xs text-amber-800">
						{$t('settings.notifications.schedule.estimatedNote')}
					</p>
				</div>
			</div>
		{/if}

		{#if saving}
			<p class="text-sm text-gray-500 text-center">{$t('common.saving')}</p>
		{/if}
	{/if}
</div>
