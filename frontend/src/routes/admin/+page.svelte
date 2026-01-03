<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getAdminStats, getAdminUsers, toggleUserActive, updateUserRole } from '$lib/api/admin';
	import type { AdminStats, AdminUser, UserRole } from '$lib/types';

	let stats = $state<AdminStats | null>(null);
	let users = $state<AdminUser[]>([]);
	let loading = $state(true);
	let error = $state('');

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
			const [statsData, usersData] = await Promise.all([getAdminStats(), loadUsers()]);
			stats = statsData;
			users = usersData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load admin data';
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
			<!-- Stats Cards -->
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
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

				<!-- Membership Breakdown -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Membership Tiers</p>
							<div class="flex gap-3 mt-1">
								<span class="text-sm">
									<span class="font-semibold text-gray-900">{stats.membershipStats.freeUsers}</span>
									<span class="text-gray-500">Free</span>
								</span>
								<span class="text-sm">
									<span class="font-semibold text-blue-600">{stats.membershipStats.plusUsers}</span>
									<span class="text-gray-500">Plus</span>
								</span>
								<span class="text-sm">
									<span class="font-semibold text-purple-600">{stats.membershipStats.proUsers}</span>
									<span class="text-gray-500">Pro</span>
								</span>
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

				<!-- Tasks Created Today -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Tasks Today</p>
							<p class="text-2xl font-bold text-gray-900">{stats.todayStats.tasksCreated}</p>
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
									d="M12 4v16m8-8H4"
								/>
							</svg>
						</div>
					</div>
					<p class="text-sm text-gray-500 mt-2">created today</p>
				</div>

				<!-- Tasks Completed Today -->
				<div class="card p-6">
					<div class="flex items-center justify-between">
						<div>
							<p class="text-sm text-gray-500">Completed Today</p>
							<p class="text-2xl font-bold text-green-600">{stats.todayStats.tasksCompleted}</p>
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
					<p class="text-sm text-gray-500 mt-2">
						{stats.todayStats.tasksUpdated} updated
					</p>
				</div>
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
											class="text-sm border border-gray-300 rounded px-2 py-1"
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
									<td colspan="7" class="px-6 py-8 text-center text-gray-500">
										No users found
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			</div>
		{/if}
	</main>
</div>
