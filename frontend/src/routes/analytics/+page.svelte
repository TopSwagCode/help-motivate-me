<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getStreakSummary, getCompletionRates, getHeatmapData } from '$lib/api/analytics';
	import StreakBadge from '$lib/components/analytics/StreakBadge.svelte';
	import type { StreakSummary, CompletionRate, HeatmapData } from '$lib/types';

	let streakSummary = $state<StreakSummary | null>(null);
	let completionRates = $state<CompletionRate | null>(null);
	let heatmapData = $state<HeatmapData[]>([]);
	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadAnalytics();
	});

	async function loadAnalytics() {
		try {
			const [streaks, rates, heatmap] = await Promise.all([
				getStreakSummary(),
				getCompletionRates(),
				getHeatmapData(90)
			]);
			streakSummary = streaks;
			completionRates = rates;
			heatmapData = heatmap;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load analytics';
		} finally {
			loading = false;
		}
	}

	function getHeatmapColor(count: number): string {
		if (count === 0) return 'bg-gray-100';
		if (count === 1) return 'bg-green-200';
		if (count <= 3) return 'bg-green-300';
		if (count <= 5) return 'bg-green-400';
		return 'bg-green-500';
	}

	function formatDate(dateStr: string): string {
		return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
	}

	function getWeeksData(): HeatmapData[][] {
		const weeks: HeatmapData[][] = [];
		for (let i = 0; i < heatmapData.length; i += 7) {
			weeks.push(heatmapData.slice(i, i + 7));
		}
		return weeks;
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<!-- Page Header -->
		<h1 class="text-2xl font-bold text-gray-900 mb-6">Analytics</h1>
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
			</div>
		{:else}
			<!-- Summary Cards -->
			<div class="grid gap-4 sm:grid-cols-3 mb-8">
				<div class="card p-4">
					<p class="text-sm text-gray-500">Total Habits</p>
					<p class="text-2xl font-bold text-gray-900">{streakSummary?.totalHabits || 0}</p>
				</div>
				<div class="card p-4">
					<p class="text-sm text-gray-500">Active Streaks</p>
					<p class="text-2xl font-bold text-green-600">{streakSummary?.activeStreaks || 0}</p>
				</div>
				<div class="card p-4">
					<p class="text-sm text-gray-500">Longest Streak</p>
					<p class="text-2xl font-bold text-orange-600">{streakSummary?.longestActiveStreak || 0} days</p>
				</div>
			</div>

			<!-- Completion Rates -->
			{#if completionRates}
				<div class="card p-6 mb-8">
					<h2 class="font-semibold text-gray-900 mb-4">Completion Rates</h2>
					<div class="grid gap-6 sm:grid-cols-3">
						<div>
							<div class="flex items-center justify-between mb-2">
								<span class="text-sm text-gray-600">Daily</span>
								<span class="text-sm font-medium text-gray-900">{Math.round(completionRates.dailyRate)}%</span>
							</div>
							<div class="h-2 bg-gray-200 rounded-full overflow-hidden">
								<div
									class="h-full bg-blue-500 rounded-full transition-all duration-300"
									style="width: {completionRates.dailyRate}%"
								></div>
							</div>
						</div>
						<div>
							<div class="flex items-center justify-between mb-2">
								<span class="text-sm text-gray-600">Weekly</span>
								<span class="text-sm font-medium text-gray-900">{Math.round(completionRates.weeklyRate)}%</span>
							</div>
							<div class="h-2 bg-gray-200 rounded-full overflow-hidden">
								<div
									class="h-full bg-green-500 rounded-full transition-all duration-300"
									style="width: {completionRates.weeklyRate}%"
								></div>
							</div>
						</div>
						<div>
							<div class="flex items-center justify-between mb-2">
								<span class="text-sm text-gray-600">Monthly</span>
								<span class="text-sm font-medium text-gray-900">{Math.round(completionRates.monthlyRate)}%</span>
							</div>
							<div class="h-2 bg-gray-200 rounded-full overflow-hidden">
								<div
									class="h-full bg-purple-500 rounded-full transition-all duration-300"
									style="width: {completionRates.monthlyRate}%"
								></div>
							</div>
						</div>
					</div>
					<div class="mt-4 pt-4 border-t border-gray-100 flex justify-between text-sm text-gray-500">
						<span>Total Completions: {completionRates.totalCompletions}</span>
						<span>Missed Days: {completionRates.missedDays}</span>
					</div>
				</div>
			{/if}

			<!-- Heatmap -->
			{#if heatmapData.length > 0}
				<div class="card p-6 mb-8">
					<h2 class="font-semibold text-gray-900 mb-4">Activity (Last 90 Days)</h2>
					<div class="overflow-x-auto">
						<div class="flex gap-1">
							{#each getWeeksData() as week}
								<div class="flex flex-col gap-1">
									{#each week as day}
										<div
											class="w-3 h-3 rounded-sm {getHeatmapColor(day.count)}"
											title="{formatDate(day.date)}: {day.count} completion{day.count !== 1 ? 's' : ''}"
										></div>
									{/each}
								</div>
							{/each}
						</div>
					</div>
					<div class="flex items-center gap-2 mt-4 text-xs text-gray-500">
						<span>Less</span>
						<div class="w-3 h-3 rounded-sm bg-gray-100"></div>
						<div class="w-3 h-3 rounded-sm bg-green-200"></div>
						<div class="w-3 h-3 rounded-sm bg-green-300"></div>
						<div class="w-3 h-3 rounded-sm bg-green-400"></div>
						<div class="w-3 h-3 rounded-sm bg-green-500"></div>
						<span>More</span>
					</div>
				</div>
			{/if}

			<!-- Streak Details -->
			{#if streakSummary && streakSummary.streaks.length > 0}
				<div class="card p-6">
					<h2 class="font-semibold text-gray-900 mb-4">Habit Streaks</h2>
					<div class="space-y-3">
						{#each streakSummary.streaks as streak (streak.taskId)}
							<div class="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
								<div>
									<p class="font-medium text-gray-900">{streak.taskTitle}</p>
									<p class="text-xs text-gray-500">
										Longest: {streak.longestStreak} days
										{#if streak.lastCompletedDate}
											<span class="mx-1">•</span>
											Last: {formatDate(streak.lastCompletedDate)}
										{/if}
									</p>
								</div>
								<StreakBadge
									currentStreak={streak.currentStreak}
									isOnGracePeriod={streak.isOnGracePeriod}
									daysUntilStreakBreaks={streak.daysUntilStreakBreaks}
								/>
							</div>
						{/each}
					</div>
				</div>
			{:else}
				<div class="card p-12 text-center">
					<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
						<span class="text-3xl">📊</span>
					</div>
					<h3 class="text-lg font-medium text-gray-900 mb-2">No streak data yet</h3>
					<p class="text-gray-500 mb-6">Complete repeating tasks to start building streaks.</p>
					<a href="/dashboard" class="btn-primary inline-block">Go to Dashboard</a>
				</div>
			{/if}
		{/if}
	</main>
</div>
