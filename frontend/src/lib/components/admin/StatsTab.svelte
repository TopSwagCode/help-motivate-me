<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import type { AdminStats, DailyStats } from '$lib/types';
	import { getAdminStats, getDailyStats } from '$lib/api/admin';

	let stats = $state<AdminStats | null>(null);
	let dailyStats = $state<DailyStats | null>(null);
	let loading = $state(true);
	let error = $state('');
	let selectedDate = $state(new Date().toISOString().split('T')[0]);

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			const [statsData, dailyStatsData] = await Promise.all([
				getAdminStats(),
				getDailyStats(selectedDate)
			]);
			stats = statsData;
			dailyStats = dailyStatsData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load stats';
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

	function navigateDate(direction: 'prev' | 'next') {
		const date = new Date(selectedDate);
		date.setDate(date.getDate() + (direction === 'next' ? 1 : -1));
		selectedDate = date.toISOString().split('T')[0];
		loadDailyStats();
	}

	function formatDisplayDate(dateStr: string): string {
		const date = new Date(dateStr);
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return date.toLocaleDateString(currentLocale, {
			weekday: 'short',
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	function isToday(dateStr: string): boolean {
		return dateStr === new Date().toISOString().split('T')[0];
	}
</script>

{#if loading}
	<div class="flex justify-center py-12">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{:else if error}
	<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
		{error}
		<button onclick={() => (error = '')} class="float-right text-red-500 hover:text-red-700">&times;</button>
	</div>
{:else if stats}
	<!-- Row 1: Total Users - Membership Tiers - Users Logged In Today -->
	<div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
		<!-- Total Users -->
		<div class="card p-6">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">{$t('admin.stats.totalUsers')}</p>
					<p class="text-2xl font-bold text-gray-900">{stats.totalUsers}</p>
				</div>
				<div class="w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
					<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
					</svg>
				</div>
			</div>
			<p class="text-sm text-gray-500 mt-2">
				{stats.activeUsers} {$t('admin.stats.activeUsers')}
			</p>
		</div>

		<!-- Membership Tiers -->
		<div class="card p-6">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">{$t('admin.stats.membershipTiers')}</p>
					<div class="flex gap-4 mt-2">
						<div class="text-center">
							<p class="text-xl font-bold text-gray-900">{stats.membershipStats.freeUsers}</p>
							<p class="text-xs text-gray-500">{$t('admin.tiers.free')}</p>
						</div>
						<div class="text-center">
							<p class="text-xl font-bold text-blue-600">{stats.membershipStats.plusUsers}</p>
							<p class="text-xs text-gray-500">{$t('admin.tiers.plus')}</p>
						</div>
						<div class="text-center">
							<p class="text-xl font-bold text-purple-600">{stats.membershipStats.proUsers}</p>
							<p class="text-xs text-gray-500">{$t('admin.tiers.pro')}</p>
						</div>
					</div>
				</div>
				<div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z" />
					</svg>
				</div>
			</div>
		</div>

		<!-- Users Logged In Today -->
		<div class="card p-6">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">{$t('admin.stats.usersLoggedInToday')}</p>
					<p class="text-2xl font-bold text-gray-900">{stats.usersLoggedInToday}</p>
				</div>
				<div class="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
					<svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
					</svg>
				</div>
			</div>
			<p class="text-sm text-gray-500 mt-2">{$t('admin.stats.activeToday')}</p>
		</div>
	</div>

	<!-- Row 2: Total Tasks Created - Total Tasks Completed -->
	<div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
		<!-- Total Tasks Created -->
		<div class="card p-6">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">{$t('admin.stats.totalTasksCreated')}</p>
					<p class="text-2xl font-bold text-gray-900">{stats.taskTotals.totalTasksCreated}</p>
				</div>
				<div class="w-12 h-12 bg-indigo-100 rounded-full flex items-center justify-center">
					<svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
					</svg>
				</div>
			</div>
			<p class="text-sm text-gray-500 mt-2">{$t('admin.stats.allTime')}</p>
		</div>

		<!-- Total Tasks Completed -->
		<div class="card p-6">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">{$t('admin.stats.totalTasksCompleted')}</p>
					<p class="text-2xl font-bold text-green-600">{stats.taskTotals.totalTasksCompleted}</p>
				</div>
				<div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
					</svg>
				</div>
			</div>
			<p class="text-sm text-gray-500 mt-2">{$t('admin.stats.allTime')}</p>
		</div>
	</div>

	<!-- Row 3: Daily Stats with Date Picker -->
	<div class="card p-6">
		<div class="flex items-center justify-between mb-6">
			<h3 class="text-lg font-semibold text-gray-900">{$t('admin.stats.daily.title')}</h3>
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
							<svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
							</svg>
						</div>
						<div>
							<p class="text-2xl font-bold text-gray-900">{dailyStats.tasksCreated}</p>
							<p class="text-sm text-gray-500">{$t('admin.stats.daily.tasksCreated')}</p>
						</div>
					</div>
				</div>

				<!-- Tasks Completed on Date -->
				<div class="bg-gray-50 rounded-lg p-4">
					<div class="flex items-center gap-3">
						<div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
							<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
							</svg>
						</div>
						<div>
							<p class="text-2xl font-bold text-green-600">{dailyStats.tasksCompleted}</p>
							<p class="text-sm text-gray-500">{$t('admin.stats.daily.tasksCompleted')}</p>
						</div>
					</div>
				</div>

				<!-- Tasks Due on Date -->
				<div class="bg-gray-50 rounded-lg p-4">
					<div class="flex items-center gap-3">
						<div class="w-10 h-10 bg-orange-100 rounded-full flex items-center justify-center">
							<svg class="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
							</svg>
						</div>
						<div>
							<p class="text-2xl font-bold text-orange-600">{dailyStats.tasksDue}</p>
							<p class="text-sm text-gray-500">{$t('admin.stats.daily.tasksDue')}</p>
						</div>
					</div>
				</div>
			</div>
		{/if}
	</div>
{/if}
