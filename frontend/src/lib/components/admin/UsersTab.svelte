<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import type { AdminUser, UserActivity, UserRole } from '$lib/types';
	import {
		getAdminUsers,
		toggleUserActive,
		updateUserRole,
		getUserActivity,
		getUsersWithPushStatus,
		sendPushToUser,
		type UserPushStatus,
		type PushNotificationResult
	} from '$lib/api/admin';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let users = $state<AdminUser[]>([]);
	let loading = $state(true);
	let error = $state('');

	// Filters
	let searchQuery = $state('');
	let tierFilter = $state('');
	let activeFilter = $state('');

	// User activity modal
	let activityModalOpen = $state(false);
	let activityModalLoading = $state(false);
	let selectedUserActivity = $state<UserActivity | null>(null);

	// Push notification modal
	let pushModalOpen = $state(false);
	let pushModalUser = $state<{ id: string; username: string } | null>(null);
	let pushModalTitle = $state('');
	let pushModalBody = $state('');
	let pushModalUrl = $state('');
	let pushModalSending = $state(false);
	let pushModalResult = $state<{ success: boolean; message: string } | null>(null);

	// User push status map
	let userPushStatuses = $state<Map<string, UserPushStatus>>(new Map());

	// Debounce search
	let searchTimeout: ReturnType<typeof setTimeout>;

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			const [usersData, pushUsersData] = await Promise.all([
				loadUsers(),
				getUsersWithPushStatus().catch(() => [])
			]);
			users = usersData;
			userPushStatuses = new Map(pushUsersData.map((u: UserPushStatus) => [u.userId, u]));
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load users';
		} finally {
			loading = false;
		}
	}

	async function loadUsers(): Promise<AdminUser[]> {
		const params: {
			search?: string;
			tier?: string;
			isActive?: boolean;
		} = {};

		if (searchQuery) params.search = searchQuery;
		if (tierFilter) params.tier = tierFilter;
		if (activeFilter === 'active') params.isActive = true;
		if (activeFilter === 'inactive') params.isActive = false;

		return getAdminUsers(params);
	}

	async function handleSearch() {
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(async () => {
			try {
				users = await loadUsers();
			} catch (e) {
				error = e instanceof Error ? e.message : 'Failed to search users';
			}
		}, 300);
	}

	async function handleFilterChange() {
		try {
			users = await loadUsers();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to filter users';
		}
	}

	async function handleToggleActive(user: AdminUser) {
		try {
			const updated = await toggleUserActive(user.id);
			users = users.map((u) => (u.id === updated.id ? updated : u));
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update user';
		}
	}

	async function handleRoleChange(user: AdminUser, newRole: UserRole) {
		try {
			const updated = await updateUserRole(user.id, { role: newRole });
			users = users.map((u) => (u.id === updated.id ? updated : u));
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update user role';
		}
	}

	async function showUserActivity(user: AdminUser) {
		activityModalOpen = true;
		activityModalLoading = true;
		selectedUserActivity = null;

		try {
			selectedUserActivity = await getUserActivity(user.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load user activity';
		} finally {
			activityModalLoading = false;
		}
	}

	function closeActivityModal() {
		activityModalOpen = false;
		selectedUserActivity = null;
	}

	function openPushModal(user: { id: string; username: string }) {
		pushModalUser = user;
		pushModalTitle = '';
		pushModalBody = '';
		pushModalUrl = '';
		pushModalResult = null;
		pushModalOpen = true;
	}

	function closePushModal() {
		pushModalOpen = false;
		pushModalUser = null;
	}

	async function handleSendPushToUser(e: Event) {
		e.preventDefault();
		if (!pushModalUser || !pushModalTitle.trim() || !pushModalBody.trim()) return;

		pushModalSending = true;
		pushModalResult = null;
		try {
			const result = await sendPushToUser(
				pushModalUser.id,
				pushModalTitle.trim(),
				pushModalBody.trim(),
				pushModalUrl.trim() || undefined
			);
			pushModalResult = {
				success: true,
				message: `Sent to ${result.successCount} device(s)`
			};
			pushModalTitle = '';
			pushModalBody = '';
			pushModalUrl = '';
		} catch (err) {
			pushModalResult = {
				success: false,
				message: err instanceof Error ? err.message : 'Failed to send push notification'
			};
		} finally {
			pushModalSending = false;
		}
	}

	function getUserPushStatus(userId: string): UserPushStatus | undefined {
		return userPushStatuses.get(userId);
	}

	function formatDate(dateStr: string): string {
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return new Date(dateStr).toLocaleDateString(currentLocale, {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	function getTierBadgeClass(tier: string): string {
		switch (tier) {
			case 'Pro':
				return 'bg-purple-100 text-purple-700';
			case 'Plus':
				return 'bg-blue-100 text-blue-700';
			default:
				return 'bg-gray-100 text-gray-700';
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
	<div class="card">
		<div class="p-6 border-b border-gray-200">
			<h2 class="text-lg font-semibold text-gray-900">{$t('admin.users.title')}</h2>

			<!-- Filters -->
			<div class="mt-4 flex flex-wrap gap-4">
				<div class="flex-1 min-w-[200px]">
					<input
						type="text"
						placeholder={$t('admin.users.searchPlaceholder')}
						bind:value={searchQuery}
						oninput={handleSearch}
						class="input w-full"
					/>
				</div>
				<select
					bind:value={tierFilter}
					onchange={handleFilterChange}
					class="input w-auto"
				>
					<option value="">{$t('admin.users.allTiers')}</option>
					<option value="Free">{$t('admin.tiers.free')}</option>
					<option value="Plus">{$t('admin.tiers.plus')}</option>
					<option value="Pro">{$t('admin.tiers.pro')}</option>
				</select>
				<select
					bind:value={activeFilter}
					onchange={handleFilterChange}
					class="input w-auto"
				>
					<option value="">{$t('admin.users.allStatus')}</option>
					<option value="active">{$t('admin.users.active')}</option>
					<option value="inactive">{$t('admin.users.inactive')}</option>
				</select>
			</div>
		</div>

		<div class="overflow-x-auto">
			<table class="w-full">
				<thead class="bg-gray-50">
					<tr>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.user')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.email')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.tier')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.role')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.status')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.joined')}</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.aiCalls')}</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.aiCost')}</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{$t('admin.users.actions')}</th>
					</tr>
				</thead>
				<tbody class="bg-white divide-y divide-gray-200">
					{#each users as user (user.id)}
						<tr class="hover:bg-gray-50 cursor-pointer" onclick={() => showUserActivity(user)}>
							<td class="px-6 py-4 whitespace-nowrap">
								<div class="flex items-center">
									<div class="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-600 font-medium text-sm">
										{user.username.charAt(0).toUpperCase()}
									</div>
									<div class="ml-3">
										<p class="text-sm font-medium text-gray-900">{user.username}</p>
										{#if user.displayName}
											<p class="text-sm text-gray-500">{user.displayName}</p>
										{/if}
									</div>
								</div>
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{user.email}</td>
							<td class="px-6 py-4 whitespace-nowrap">
								<span class="px-2 py-1 text-xs font-medium rounded-full {getTierBadgeClass(user.membershipTier)}">
									{user.membershipTier}
								</span>
							</td>
							<td class="px-6 py-4 whitespace-nowrap">
								<select
									value={user.role}
									onchange={(e) => handleRoleChange(user, (e.target as HTMLSelectElement).value as UserRole)}
									onclick={(e) => e.stopPropagation()}
									class="text-sm border border-gray-300 rounded py-1"
								>
									<option value="User">{$t('admin.users.roleUser')}</option>
									<option value="Admin">{$t('admin.users.roleAdmin')}</option>
								</select>
							</td>
							<td class="px-6 py-4 whitespace-nowrap">
								{#if user.isActive}
									<span class="px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700">{$t('admin.users.active')}</span>
								{:else}
									<span class="px-2 py-1 text-xs font-medium rounded-full bg-red-100 text-red-700">{$t('admin.users.inactive')}</span>
								{/if}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{formatDate(user.createdAt)}</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-right">{user.aiCallsCount}</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-right">
								{#if user.aiTotalCostUsd > 0}
									<span class="text-amber-600 font-medium">${user.aiTotalCostUsd.toFixed(4)}</span>
								{:else}
									<span class="text-gray-400">-</span>
								{/if}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm">
								<div class="flex items-center gap-2">
									{#if getUserPushStatus(user.id)?.hasPushEnabled}
										<button
											onclick={(e) => { e.stopPropagation(); openPushModal({ id: user.id, username: user.username }); }}
											class="text-blue-600 hover:text-blue-800"
											title="Send push notification"
										>
											<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
											</svg>
										</button>
									{/if}
									<button
										onclick={(e) => { e.stopPropagation(); handleToggleActive(user); }}
										class="text-primary-600 hover:text-primary-800 font-medium"
									>
										{user.isActive ? $t('admin.users.deactivate') : $t('admin.users.activate')}
									</button>
								</div>
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="9" class="px-6 py-8 text-center text-gray-500">{$t('admin.users.noUsers')}</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
{/if}

<!-- User Activity Modal -->
{#if activityModalOpen}
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
		onclick={closeActivityModal}
		role="presentation"
	>
		<div
			class="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-auto"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.key === 'Escape' && closeActivityModal()}
			role="dialog"
			aria-modal="true"
			tabindex="-1"
		>
			<div class="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
				<div>
					<h2 class="text-xl font-semibold text-gray-900">{$t('admin.users.activity.title')}</h2>
					{#if selectedUserActivity}
						<p class="text-sm text-gray-500 mt-1">
							{selectedUserActivity.username} ({selectedUserActivity.email})
						</p>
					{/if}
				</div>
				<button
					onclick={closeActivityModal}
					class="text-gray-400 hover:text-gray-600 transition-colors"
					aria-label={$t('admin.users.activity.close')}
				>
					<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<div class="p-6">
				{#if activityModalLoading}
					<div class="flex items-center justify-center py-12">
						<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
						<span class="ml-3 text-gray-600">{$t('admin.users.activity.loading')}</span>
					</div>
				{:else if selectedUserActivity}
					<div class="grid grid-cols-1 md:grid-cols-2 gap-6">
						<!-- Last Week Column -->
						<div class="space-y-4">
							<h3 class="text-lg font-semibold text-gray-900 border-b pb-2">{$t('admin.users.activity.lastWeek')}</h3>
							<div class="space-y-3">
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.tasksCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.lastWeek.tasksCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.tasksCompleted')}</span>
									<span class="font-semibold text-green-600">{selectedUserActivity.lastWeek.tasksCompleted}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.goalsCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.lastWeek.goalsCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.identitiesCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.lastWeek.identitiesCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.habitStacksCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.lastWeek.habitStacksCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.habitCompletions')}</span>
									<span class="font-semibold text-green-600">{selectedUserActivity.lastWeek.habitCompletions}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.journalEntries')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.lastWeek.journalEntries}</span>
								</div>
								<div class="border-t pt-3 mt-3">
									<div class="flex justify-between items-center">
										<span class="text-gray-600">{$t('admin.users.activity.aiCalls')}</span>
										<span class="font-semibold text-primary-600">{selectedUserActivity.lastWeek.aiCalls}</span>
									</div>
									<div class="flex justify-between items-center mt-2">
										<span class="text-gray-600">{$t('admin.users.activity.aiCost')}</span>
										<span class="font-semibold text-amber-600">${selectedUserActivity.lastWeek.aiCostUsd.toFixed(4)}</span>
									</div>
								</div>
							</div>
						</div>

						<!-- Total Column -->
						<div class="space-y-4">
							<h3 class="text-lg font-semibold text-gray-900 border-b pb-2">{$t('admin.users.activity.total')}</h3>
							<div class="space-y-3">
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.tasksCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.total.tasksCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.tasksCompleted')}</span>
									<span class="font-semibold text-green-600">{selectedUserActivity.total.tasksCompleted}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.goalsCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.total.goalsCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.identitiesCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.total.identitiesCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.habitStacksCreated')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.total.habitStacksCreated}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.habitCompletions')}</span>
									<span class="font-semibold text-green-600">{selectedUserActivity.total.habitCompletions}</span>
								</div>
								<div class="flex justify-between items-center">
									<span class="text-gray-600">{$t('admin.users.activity.journalEntries')}</span>
									<span class="font-semibold text-gray-900">{selectedUserActivity.total.journalEntries}</span>
								</div>
								<div class="border-t pt-3 mt-3">
									<div class="flex justify-between items-center">
										<span class="text-gray-600">{$t('admin.users.activity.aiCalls')}</span>
										<span class="font-semibold text-primary-600">{selectedUserActivity.total.aiCalls}</span>
									</div>
									<div class="flex justify-between items-center mt-2">
										<span class="text-gray-600">{$t('admin.users.activity.aiCost')}</span>
										<span class="font-semibold text-amber-600">${selectedUserActivity.total.aiCostUsd.toFixed(4)}</span>
									</div>
								</div>
							</div>
						</div>
					</div>

					<div class="mt-6 flex justify-end">
						<button
							onclick={closeActivityModal}
							class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors"
						>
							{$t('admin.users.activity.close')}
						</button>
					</div>
				{/if}
			</div>
		</div>
	</div>
{/if}

<!-- Push Notification Modal -->
{#if pushModalOpen && pushModalUser}
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
		onclick={closePushModal}
		role="presentation"
	>
		<div
			class="bg-white rounded-lg shadow-xl max-w-md w-full"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.key === 'Escape' && closePushModal()}
			role="dialog"
			aria-modal="true"
			tabindex="-1"
		>
			<div class="border-b border-gray-200 px-6 py-4 flex items-center justify-between">
				<div>
					<h2 class="text-lg font-semibold text-gray-900">Send Push Notification</h2>
					<p class="text-sm text-gray-500 mt-1">To: {pushModalUser.username}</p>
				</div>
				<button
					onclick={closePushModal}
					class="text-gray-400 hover:text-gray-600 transition-colors"
					aria-label="Close"
				>
					<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<form onsubmit={handleSendPushToUser} class="p-6 space-y-4">
				<div>
					<label for="push-modal-title" class="block text-sm font-medium text-gray-700 mb-1">
						Title <span class="text-red-500">*</span>
					</label>
					<input
						id="push-modal-title"
						type="text"
						bind:value={pushModalTitle}
						placeholder="Notification title"
						class="input w-full"
						required
						maxlength="100"
					/>
				</div>

				<div>
					<label for="push-modal-body" class="block text-sm font-medium text-gray-700 mb-1">
						Message <span class="text-red-500">*</span>
					</label>
					<textarea
						id="push-modal-body"
						bind:value={pushModalBody}
						placeholder="Notification message"
						class="input w-full h-24 resize-none"
						required
						maxlength="500"
					></textarea>
				</div>

				<div>
					<label for="push-modal-url" class="block text-sm font-medium text-gray-700 mb-1">
						Click URL (optional)
					</label>
					<input
						id="push-modal-url"
						type="text"
						bind:value={pushModalUrl}
						placeholder="/today or https://example.com"
						class="input w-full"
					/>
				</div>

				{#if pushModalResult}
					<div class="p-3 rounded-lg {pushModalResult.success ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}">
						{pushModalResult.message}
					</div>
				{/if}

				<div class="flex gap-3 pt-2">
					<button
						type="button"
						onclick={closePushModal}
						class="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors"
					>
						Cancel
					</button>
					<button
						type="submit"
						disabled={pushModalSending || !pushModalTitle.trim() || !pushModalBody.trim()}
						class="flex-1 btn btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
					>
						{#if pushModalSending}
							Sending...
						{:else}
							Send
						{/if}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
