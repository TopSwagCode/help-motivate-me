<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import {
		getAdminStats,
		getAdminUsers,
		getDailyStats,
		toggleUserActive,
		updateUserRole,
		getAdminSettings,
		getWaitlist,
		removeFromWaitlist,
		approveWaitlistEntry,
		getWhitelist,
		addToWhitelist,
		removeFromWhitelist
	} from '$lib/api/admin';
	import type { AdminStats, AdminUser, DailyStats, UserRole } from '$lib/types';
	import type { WaitlistEntry, WhitelistEntry } from '$lib/types/waitlist';

	let stats = $state<AdminStats | null>(null);
	let dailyStats = $state<DailyStats | null>(null);
	let users = $state<AdminUser[]>([]);
	let waitlistEntries = $state<WaitlistEntry[]>([]);
	let whitelistEntries = $state<WhitelistEntry[]>([]);
	let allowSignups = $state(true);
	let loading = $state(true);
	let error = $state('');

	// Invite form
	let inviteEmail = $state('');
	let inviteLoading = $state(false);

	// Date picker state
	let selectedDate = $state(new Date().toISOString().split('T')[0]);

	// Filters
	let searchQuery = $state('');
	let tierFilter = $state('');
	let activeFilter = $state('');

	// Debounce search
	let searchTimeout: ReturnType<typeof setTimeout>;

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		// Check if user is admin
		if ($auth.user.role !== 'Admin') {
			goto('/today');
			return;
		}

		await loadData();
	});

	async function loadData() {
		loading = true;
		error = '';

		try {
			const [statsData, dailyStatsData, usersData, settingsData, waitlistData, whitelistData] = await Promise.all([
				getAdminStats(),
				getDailyStats(selectedDate),
				loadUsers(),
				getAdminSettings(),
				getWaitlist(),
				getWhitelist()
			]);
			stats = statsData;
			dailyStats = dailyStatsData;
			users = usersData;
			allowSignups = settingsData.allowSignups;
			waitlistEntries = waitlistData;
			whitelistEntries = whitelistData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load admin data';
		} finally {
			loading = false;
		}
	}

	async function loadDailyStats() {
		try {
			dailyStats = await getDailyStats(selectedDate);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load daily stats';
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
			// Refresh stats
			stats = await getAdminStats();
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

	function navigateDate(direction: 'prev' | 'next') {
		const date = new Date(selectedDate);
		date.setDate(date.getDate() + (direction === 'next' ? 1 : -1));
		selectedDate = date.toISOString().split('T')[0];
		loadDailyStats();
	}

	function formatDisplayDate(dateStr: string): string {
		const date = new Date(dateStr);
		return date.toLocaleDateString('en-US', {
			weekday: 'short',
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	function formatDate(dateStr: string): string {
		return new Date(dateStr).toLocaleDateString('en-US', {
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

	function isToday(dateStr: string): boolean {
		return dateStr === new Date().toISOString().split('T')[0];
	}

	// Waitlist/Whitelist handlers
	async function handleApproveWaitlist(entry: WaitlistEntry) {
		try {
			const newWhitelistEntry = await approveWaitlistEntry(entry.id);
			waitlistEntries = waitlistEntries.filter((w) => w.id !== entry.id);
			whitelistEntries = [newWhitelistEntry, ...whitelistEntries];
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to approve waitlist entry';
		}
	}

	async function handleRemoveFromWaitlist(entry: WaitlistEntry) {
		try {
			await removeFromWaitlist(entry.id);
			waitlistEntries = waitlistEntries.filter((w) => w.id !== entry.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to remove from waitlist';
		}
	}

	async function handleRemoveFromWhitelist(entry: WhitelistEntry) {
		try {
			await removeFromWhitelist(entry.id);
			whitelistEntries = whitelistEntries.filter((w) => w.id !== entry.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to remove from whitelist';
		}
	}

	async function handleInviteUser(e: Event) {
		e.preventDefault();
		if (!inviteEmail.trim()) return;

		inviteLoading = true;
		try {
			const newEntry = await addToWhitelist(inviteEmail.trim());
			whitelistEntries = [newEntry, ...whitelistEntries];
			inviteEmail = '';
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to invite user';
		} finally {
			inviteLoading = false;
		}
	}
</script>

<svelte:head>
	<title>Admin Dashboard - HelpMotivateMe</title>
</svelte:head>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="mb-8">
			<h1 class="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
			<p class="text-gray-500 mt-1">System overview and user management</p>
		</div>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
				<button onclick={() => (error = '')} class="float-right text-red-500 hover:text-red-700"
					>&times;</button
				>
			</div>
		{:else if stats}
			<!-- Row 1: Total Users - Membership Tiers - Users Logged In Today -->
			<div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
				<!-- Total Users -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Total Users</p>
							<p class="text-2xl font-bold text-gray-900">{stats.totalUsers}</p>
						</div>
						<div class="w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
							<svg
								class="w-6 h-6 text-primary-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"
								/>
							</svg>
						</div>
					</div>
					<p class="text-sm text-gray-500 mt-2">
						{stats.activeUsers} active
					</p>
				</div>

				<!-- Membership Tiers -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Membership Tiers</p>
							<div class="flex gap-4 mt-2">
								<div class="text-center">
									<p class="text-xl font-bold text-gray-900">{stats.membershipStats.freeUsers}</p>
									<p class="text-xs text-gray-500">Free</p>
								</div>
								<div class="text-center">
									<p class="text-xl font-bold text-blue-600">{stats.membershipStats.plusUsers}</p>
									<p class="text-xs text-gray-500">Plus</p>
								</div>
								<div class="text-center">
									<p class="text-xl font-bold text-purple-600">{stats.membershipStats.proUsers}</p>
									<p class="text-xs text-gray-500">Pro</p>
								</div>
							</div>
						</div>
						<div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
							<svg
								class="w-6 h-6 text-green-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z"
								/>
							</svg>
						</div>
					</div>
				</div>

				<!-- Users Logged In Today -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Users Logged In Today</p>
							<p class="text-2xl font-bold text-gray-900">{stats.usersLoggedInToday}</p>
						</div>
						<div class="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
							<svg
								class="w-6 h-6 text-blue-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"
								/>
							</svg>
						</div>
					</div>
					<p class="text-sm text-gray-500 mt-2">active today</p>
				</div>
			</div>

			<!-- Row 2: Total Tasks Created - Total Tasks Completed -->
			<div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
				<!-- Total Tasks Created -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Total Tasks Created</p>
							<p class="text-2xl font-bold text-gray-900">{stats.taskTotals.totalTasksCreated}</p>
						</div>
						<div class="w-12 h-12 bg-indigo-100 rounded-full flex items-center justify-center">
							<svg
								class="w-6 h-6 text-indigo-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M12 4v16m8-8H4"
								/>
							</svg>
						</div>
					</div>
					<p class="text-sm text-gray-500 mt-2">all time</p>
				</div>

				<!-- Total Tasks Completed -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Total Tasks Completed</p>
							<p class="text-2xl font-bold text-green-600">
								{stats.taskTotals.totalTasksCompleted}
							</p>
						</div>
						<div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
							<svg
								class="w-6 h-6 text-green-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M5 13l4 4L19 7"
								/>
							</svg>
						</div>
					</div>
					<p class="text-sm text-gray-500 mt-2">all time</p>
				</div>
			</div>

			<!-- Row 3: Daily Stats with Date Picker -->
			<div class="card p-6 mb-8">
				<div class="flex items-center justify-between mb-6">
					<h3 class="text-lg font-semibold text-gray-900">Daily Statistics</h3>
					<div class="flex items-center gap-2">
						<button
							onclick={() => navigateDate('prev')}
							class="p-2 hover:bg-gray-100 rounded-lg transition-colors"
							aria-label="Previous day"
						>
							<svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
							</svg>
						</button>
						<input
							type="date"
							bind:value={selectedDate}
							onchange={loadDailyStats}
							class="input text-center"
						/>
						<button
							onclick={() => navigateDate('next')}
							class="p-2 hover:bg-gray-100 rounded-lg transition-colors"
							disabled={isToday(selectedDate)}
							class:opacity-50={isToday(selectedDate)}
							class:cursor-not-allowed={isToday(selectedDate)}
							aria-label="Next day"
						>
							<svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
							</svg>
						</button>
					</div>
				</div>

				<p class="text-sm text-gray-500 mb-4">{formatDisplayDate(selectedDate)}</p>

				{#if dailyStats}
					<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
						<!-- Tasks Created on Date -->
						<div class="bg-gray-50 rounded-lg p-4">
							<div class="flex items-center gap-3">
								<div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
									<svg
										class="w-5 h-5 text-blue-600"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M12 4v16m8-8H4"
										/>
									</svg>
								</div>
								<div>
									<p class="text-2xl font-bold text-gray-900">{dailyStats.tasksCreated}</p>
									<p class="text-sm text-gray-500">Tasks Created</p>
								</div>
							</div>
						</div>

						<!-- Tasks Completed on Date -->
						<div class="bg-gray-50 rounded-lg p-4">
							<div class="flex items-center gap-3">
								<div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
									<svg
										class="w-5 h-5 text-green-600"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M5 13l4 4L19 7"
										/>
									</svg>
								</div>
								<div>
									<p class="text-2xl font-bold text-green-600">{dailyStats.tasksCompleted}</p>
									<p class="text-sm text-gray-500">Tasks Completed</p>
								</div>
							</div>
						</div>

						<!-- Tasks Due on Date -->
						<div class="bg-gray-50 rounded-lg p-4">
							<div class="flex items-center gap-3">
								<div class="w-10 h-10 bg-orange-100 rounded-full flex items-center justify-center">
									<svg
										class="w-5 h-5 text-orange-600"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
										/>
									</svg>
								</div>
								<div>
									<p class="text-2xl font-bold text-orange-600">{dailyStats.tasksDue}</p>
									<p class="text-sm text-gray-500">Tasks Due</p>
								</div>
							</div>
						</div>
					</div>
				{/if}
			</div>

			<!-- Users Table -->
			<div class="card">
				<div class="p-6 border-b border-gray-200">
					<h2 class="text-lg font-semibold text-gray-900">Users</h2>

					<!-- Filters -->
					<div class="mt-4 flex flex-wrap gap-4">
						<div class="flex-1 min-w-[200px]">
							<input
								type="text"
								placeholder="Search users..."
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
							<option value="">All Tiers</option>
							<option value="Free">Free</option>
							<option value="Plus">Plus</option>
							<option value="Pro">Pro</option>
						</select>
						<select
							bind:value={activeFilter}
							onchange={handleFilterChange}
							class="input w-auto"
						>
							<option value="">All Status</option>
							<option value="active">Active</option>
							<option value="inactive">Inactive</option>
						</select>
					</div>
				</div>

				<div class="overflow-x-auto">
					<table class="w-full">
						<thead class="bg-gray-50">
							<tr>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>User</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Email</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Tier</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Role</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Status</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Joined</th
								>
								<th
									class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
									>AI Calls</th
								>
								<th
									class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
									>AI Cost</th
								>
								<th
									class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
									>Actions</th
								>
							</tr>
						</thead>
						<tbody class="bg-white divide-y divide-gray-200">
							{#each users as user (user.id)}
								<tr class="hover:bg-gray-50">
									<td class="px-6 py-4 whitespace-nowrap">
										<div class="flex items-center">
											<div
												class="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-600 font-medium text-sm"
											>
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
									<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
										{user.email}
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										<span
											class="px-2 py-1 text-xs font-medium rounded-full {getTierBadgeClass(
												user.membershipTier
											)}"
										>
											{user.membershipTier}
										</span>
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										<select
											value={user.role}
											onchange={(e) =>
												handleRoleChange(user, (e.target as HTMLSelectElement).value as UserRole)}
											class="text-sm border border-gray-300 rounded py-1"
										>
											<option value="User">User</option>
											<option value="Admin">Admin</option>
										</select>
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										{#if user.isActive}
											<span
												class="px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700"
												>Active</span
											>
										{:else}
											<span
												class="px-2 py-1 text-xs font-medium rounded-full bg-red-100 text-red-700"
												>Inactive</span
											>
										{/if}
									</td>
									<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
										{formatDate(user.createdAt)}
									</td>
									<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-right">
										{user.aiCallsCount}
									</td>
									<td class="px-6 py-4 whitespace-nowrap text-sm text-right">
										{#if user.aiTotalCostUsd > 0}
											<span class="text-amber-600 font-medium">${user.aiTotalCostUsd.toFixed(4)}</span>
										{:else}
											<span class="text-gray-400">-</span>
										{/if}
									</td>
									<td class="px-6 py-4 whitespace-nowrap text-sm">
										<button
											onclick={() => handleToggleActive(user)}
											class="text-primary-600 hover:text-primary-800 font-medium"
										>
											{user.isActive ? 'Deactivate' : 'Activate'}
										</button>
									</td>
								</tr>
							{:else}
								<tr>
									<td colspan="9" class="px-6 py-8 text-center text-gray-500">
										No users found
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			</div>

			<!-- Access Control Section Header -->
			<div class="mt-8 mb-4">
				<h2 class="text-xl font-bold text-gray-900">Access Control</h2>
				<p class="text-gray-500 mt-1">
					{#if allowSignups}
						<span class="inline-flex items-center gap-1.5">
							<span class="w-2 h-2 bg-green-500 rounded-full"></span>
							Open signups enabled
						</span>
					{:else}
						<span class="inline-flex items-center gap-1.5">
							<span class="w-2 h-2 bg-yellow-500 rounded-full"></span>
							Closed beta - whitelist required
						</span>
					{/if}
				</p>
			</div>

			<!-- Invite User Section -->
			<div class="card p-6 mb-4">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">Invite User</h3>
				<form onsubmit={handleInviteUser} class="flex gap-4">
					<input
						type="email"
						placeholder="Enter email address..."
						bind:value={inviteEmail}
						required
						class="input flex-1"
					/>
					<button
						type="submit"
						disabled={inviteLoading || !inviteEmail.trim()}
						class="btn-primary whitespace-nowrap"
					>
						{inviteLoading ? 'Sending...' : 'Send Invite'}
					</button>
				</form>
				<p class="text-sm text-gray-500 mt-2">
					Adds the email to the whitelist and sends an invite email.
				</p>
			</div>

			<!-- Waitlist and Whitelist Tables -->
			<div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
				<!-- Waitlist Table -->
				<div class="card">
					<div class="p-6 border-b border-gray-200">
						<div class="flex items-center justify-between">
							<h3 class="text-lg font-semibold text-gray-900">Waitlist</h3>
							<span class="px-2 py-1 text-xs font-medium rounded-full bg-yellow-100 text-yellow-700">
								{waitlistEntries.length} pending
							</span>
						</div>
						<p class="text-sm text-gray-500 mt-1">Users waiting for access</p>
					</div>

					<div class="overflow-x-auto max-h-96 overflow-y-auto">
						<table class="w-full">
							<thead class="bg-gray-50 sticky top-0">
								<tr>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Email
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Name
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Date
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Actions
									</th>
								</tr>
							</thead>
							<tbody class="bg-white divide-y divide-gray-200">
								{#each waitlistEntries as entry (entry.id)}
									<tr class="hover:bg-gray-50">
										<td class="px-4 py-3 text-sm text-gray-900 max-w-[150px] truncate" title={entry.email}>
											{entry.email}
										</td>
										<td class="px-4 py-3 text-sm text-gray-500 max-w-[100px] truncate" title={entry.name}>
											{entry.name}
										</td>
										<td class="px-4 py-3 text-sm text-gray-500 whitespace-nowrap">
											{formatDate(entry.createdAt)}
										</td>
										<td class="px-4 py-3 text-sm whitespace-nowrap">
											<div class="flex gap-2">
												<button
													onclick={() => handleApproveWaitlist(entry)}
													class="text-green-600 hover:text-green-800 font-medium"
													title="Approve and send invite"
												>
													Approve
												</button>
												<button
													onclick={() => handleRemoveFromWaitlist(entry)}
													class="text-red-600 hover:text-red-800 font-medium"
													title="Remove from waitlist"
												>
													Remove
												</button>
											</div>
										</td>
									</tr>
								{:else}
									<tr>
										<td colspan="4" class="px-4 py-8 text-center text-gray-500">
											No waitlist entries
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>

				<!-- Whitelist Table -->
				<div class="card">
					<div class="p-6 border-b border-gray-200">
						<div class="flex items-center justify-between">
							<h3 class="text-lg font-semibold text-gray-900">Whitelist</h3>
							<span class="px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700">
								{whitelistEntries.length} approved
							</span>
						</div>
						<p class="text-sm text-gray-500 mt-1">Emails allowed to sign up</p>
					</div>

					<div class="overflow-x-auto max-h-96 overflow-y-auto">
						<table class="w-full">
							<thead class="bg-gray-50 sticky top-0">
								<tr>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Email
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Added
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Invited
									</th>
									<th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
										Actions
									</th>
								</tr>
							</thead>
							<tbody class="bg-white divide-y divide-gray-200">
								{#each whitelistEntries as entry (entry.id)}
									<tr class="hover:bg-gray-50">
										<td class="px-4 py-3 text-sm text-gray-900 max-w-[150px] truncate" title={entry.email}>
											{entry.email}
										</td>
										<td class="px-4 py-3 text-sm text-gray-500 whitespace-nowrap">
											<div>
												<p>{formatDate(entry.addedAt)}</p>
												{#if entry.addedByUsername}
													<p class="text-xs text-gray-400">by {entry.addedByUsername}</p>
												{/if}
											</div>
										</td>
										<td class="px-4 py-3 text-sm whitespace-nowrap">
											{#if entry.invitedAt}
												<span class="text-green-600" title={formatDate(entry.invitedAt)}>Sent</span>
											{:else}
												<span class="text-gray-400">-</span>
											{/if}
										</td>
										<td class="px-4 py-3 text-sm whitespace-nowrap">
											<button
												onclick={() => handleRemoveFromWhitelist(entry)}
												class="text-red-600 hover:text-red-800 font-medium"
												title="Remove from whitelist"
											>
												Remove
											</button>
										</td>
									</tr>
								{:else}
									<tr>
										<td colspan="4" class="px-4 py-8 text-center text-gray-500">
											No whitelist entries
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>
			</div>
		{/if}
	</main>
</div>
