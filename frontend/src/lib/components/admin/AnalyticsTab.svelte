<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import {
		getAnalyticsOverview,
		type AnalyticsOverviewResponse,
		type SessionSummary
	} from '$lib/api/admin';

	let analytics = $state<AnalyticsOverviewResponse | null>(null);
	let loading = $state(true);
	let error = $state('');
	let selectedDays = $state(30);

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			analytics = await getAnalyticsOverview(selectedDays);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load analytics data';
		} finally {
			loading = false;
		}
	}

	function handleDaysChange() {
		loadData();
	}

	function formatDateTime(dateStr: string): string {
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return new Date(dateStr).toLocaleString(currentLocale, {
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}

	function formatDuration(minutes: number): string {
		if (minutes < 1) return '< 1 min';
		if (minutes < 60) return `${Math.round(minutes)} min`;
		const hours = Math.floor(minutes / 60);
		const remainingMins = Math.round(minutes % 60);
		return `${hours}h ${remainingMins}m`;
	}

	function getMaxDailyCount(): number {
		if (!analytics?.dailyEventCounts.length) return 1;
		return Math.max(...analytics.dailyEventCounts.map(d => d.count));
	}

	function getBarHeight(count: number): number {
		const max = getMaxDailyCount();
		return max > 0 ? (count / max) * 100 : 0;
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
{:else if analytics}
	<!-- Time Period Selector -->
	<div class="mb-6 flex items-center justify-between">
		<div>
			<h2 class="text-lg font-semibold text-gray-900">Analytics Events Overview</h2>
			<p class="text-sm text-gray-500 mt-1">Privacy-focused usage analytics (no IP/user agent tracking)</p>
		</div>
		<div class="flex items-center gap-2">
			<label for="days-select" class="text-sm text-gray-600">Time period:</label>
			<select
				id="days-select"
				bind:value={selectedDays}
				onchange={handleDaysChange}
				class="input w-auto"
			>
				<option value={7}>Last 7 days</option>
				<option value={30}>Last 30 days</option>
				<option value={90}>Last 90 days</option>
			</select>
		</div>
	</div>

	<!-- Overview Stats Cards -->
	<div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
		<!-- Total Events -->
		<div class="card p-4">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">Total Events</p>
					<p class="text-2xl font-bold text-gray-900">{analytics.totalEvents.toLocaleString()}</p>
				</div>
				<div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
					</svg>
				</div>
			</div>
		</div>

		<!-- Unique Users -->
		<div class="card p-4">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">Active Users</p>
					<p class="text-2xl font-bold text-gray-900">{analytics.uniqueUsers}</p>
				</div>
				<div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
					</svg>
				</div>
			</div>
		</div>

		<!-- Unique Sessions -->
		<div class="card p-4">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">Total Sessions</p>
					<p class="text-2xl font-bold text-gray-900">{analytics.uniqueSessions}</p>
				</div>
				<div class="w-10 h-10 bg-purple-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
				</div>
			</div>
		</div>

		<!-- Avg Events Per Session -->
		<div class="card p-4">
			<div class="flex items-center justify-between">
				<div>
					<p class="text-sm text-gray-500">Avg Events/Session</p>
					<p class="text-2xl font-bold text-gray-900">{analytics.avgEventsPerSession}</p>
				</div>
				<div class="w-10 h-10 bg-orange-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
					</svg>
				</div>
			</div>
		</div>
	</div>

	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
		<!-- Daily Events Chart -->
		<div class="card p-6">
			<h3 class="text-lg font-semibold text-gray-900 mb-4">Daily Events</h3>
			{#if analytics.dailyEventCounts.length > 0}
				<div class="h-48 flex items-end gap-1">
					{#each analytics.dailyEventCounts as day}
						<div
							class="flex-1 bg-primary-500 rounded-t hover:bg-primary-600 transition-colors cursor-pointer group relative"
							style="height: {getBarHeight(day.count)}%"
							title="{day.date}: {day.count} events"
						>
							<div class="hidden group-hover:block absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap">
								{day.date}: {day.count}
							</div>
						</div>
					{/each}
				</div>
				<div class="flex justify-between mt-2 text-xs text-gray-500">
					<span>{analytics.dailyEventCounts[0]?.date}</span>
					<span>{analytics.dailyEventCounts[analytics.dailyEventCounts.length - 1]?.date}</span>
				</div>
			{:else}
				<div class="h-48 flex items-center justify-center text-gray-400">
					No events recorded in this period
				</div>
			{/if}
		</div>

		<!-- Top Event Types -->
		<div class="card p-6">
			<h3 class="text-lg font-semibold text-gray-900 mb-4">Top Event Types</h3>
			{#if analytics.topEventTypes.length > 0}
				<div class="space-y-3">
					{#each analytics.topEventTypes as eventType, i}
						{@const maxCount = analytics.topEventTypes[0]?.count || 1}
						{@const percentage = (eventType.count / maxCount) * 100}
						<div class="flex items-center gap-3">
							<span class="text-sm font-medium text-gray-700 w-48 truncate" title={eventType.eventType}>
								{eventType.eventType}
							</span>
							<div class="flex-1 h-4 bg-gray-100 rounded-full overflow-hidden">
								<div
									class="h-full bg-primary-500 rounded-full transition-all"
									style="width: {percentage}%"
								></div>
							</div>
							<span class="text-sm text-gray-500 w-16 text-right">{eventType.count}</span>
						</div>
					{/each}
				</div>
			{:else}
				<div class="py-8 text-center text-gray-400">
					No events recorded
				</div>
			{/if}
		</div>
	</div>

	<!-- Recent Sessions Table -->
	<div class="card">
		<div class="p-6 border-b border-gray-200">
			<h3 class="text-lg font-semibold text-gray-900">Recent Sessions</h3>
			<p class="text-sm text-gray-500 mt-1">Last 20 sessions with activity details</p>
		</div>

		<div class="overflow-x-auto">
			<table class="w-full">
				<thead class="bg-gray-50">
					<tr>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Session Start</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Last Activity</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Events</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Duration</th>
					</tr>
				</thead>
				<tbody class="bg-white divide-y divide-gray-200">
					{#each analytics.recentSessions as session (session.sessionId)}
						<tr class="hover:bg-gray-50">
							<td class="px-6 py-4 whitespace-nowrap">
								<div class="flex items-center">
									<div class="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-600 font-medium text-sm">
										{session.username.charAt(0).toUpperCase()}
									</div>
									<div class="ml-3">
										<p class="text-sm font-medium text-gray-900">{session.username}</p>
										<p class="text-xs text-gray-400 font-mono">{session.sessionId.slice(0, 8)}...</p>
									</div>
								</div>
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
								{formatDateTime(session.firstEvent)}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
								{formatDateTime(session.lastEvent)}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right font-medium">
								{session.eventCount}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-right">
								<span class="px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-700">
									{formatDuration(session.durationMinutes)}
								</span>
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="5" class="px-6 py-8 text-center text-gray-500">
								No sessions recorded in this period
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
{/if}
