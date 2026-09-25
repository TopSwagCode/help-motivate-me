<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getCompletionRates, getHeatmapData } from '$lib/api/analytics';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';
	import type { CompletionRate, HeatmapData } from '$lib/types';

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
		loading = true;
		error = '';
		try {
			const [rates, heatmap] = await Promise.all([
				getCompletionRates(),
				getHeatmapData(90)
			]);
			completionRates = rates;
			heatmapData = heatmap;
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('analytics.errors.loadFailed');
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
		return new Date(dateStr).toLocaleDateString(get(locale) === 'da' ? 'da-DK' : 'en-US', { month: 'short', day: 'numeric' });
	}

	function getWeeksData(): HeatmapData[][] {
		const weeks: HeatmapData[][] = [];
		for (let i = 0; i < heatmapData.length; i += 7) {
			weeks.push(heatmapData.slice(i, i + 7));
		}
		return weeks;
	}
</script>

<div class="bg-warm-cream">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<!-- Page Header -->
		<h1 class="text-2xl font-bold text-cocoa-800 mb-6">{$t('analytics.pageTitle')}</h1>
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="card">
				<ErrorState message={error} onRetry={loadAnalytics} size="md" />
			</div>
		{:else}
			<!-- Completion Rates -->
			{#if completionRates}
				<div class="card p-6 mb-8">
					<h2 class="font-semibold text-cocoa-800 mb-4">{$t('analytics.rates.title')}</h2>
					<div class="grid gap-6 sm:grid-cols-3">
						<div>
							<div class="flex items-center justify-between mb-2">
								<span class="text-sm text-cocoa-600">{$t('analytics.rates.daily')}</span>
								<span class="text-sm font-medium text-cocoa-800">{Math.round(completionRates.dailyRate)}%</span>
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
								<span class="text-sm text-cocoa-600">{$t('analytics.rates.weekly')}</span>
								<span class="text-sm font-medium text-cocoa-800">{Math.round(completionRates.weeklyRate)}%</span>
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
								<span class="text-sm text-cocoa-600">{$t('analytics.rates.monthly')}</span>
								<span class="text-sm font-medium text-cocoa-800">{Math.round(completionRates.monthlyRate)}%</span>
							</div>
							<div class="h-2 bg-gray-200 rounded-full overflow-hidden">
								<div
									class="h-full bg-purple-500 rounded-full transition-all duration-300"
									style="width: {completionRates.monthlyRate}%"
								></div>
							</div>
						</div>
					</div>
					<div class="mt-4 pt-4 border-t border-gray-100 flex justify-between text-sm text-cocoa-500">
						<span>{$t('analytics.rates.totalCompletions')}: {completionRates.totalCompletions}</span>
						<span>{$t('analytics.rates.missedDays')}: {completionRates.missedDays}</span>
					</div>
				</div>
			{/if}

			<!-- Heatmap -->
			{#if heatmapData.length > 0}
				<div class="card p-6 mb-8">
					<h2 class="font-semibold text-cocoa-800 mb-4">{$t('analytics.heatmap.title')}</h2>
					<div class="overflow-x-auto">
						<div class="flex gap-1">
							{#each getWeeksData() as week}
								<div class="flex flex-col gap-1">
									{#each week as day}
										<div
											class="w-3 h-3 rounded-sm {getHeatmapColor(day.count)}"
											title="{formatDate(day.date)}: {day.count} {day.count !== 1 ? $t('analytics.heatmap.completions') : $t('analytics.heatmap.completion')}"
										></div>
									{/each}
								</div>
							{/each}
						</div>
					</div>
					<div class="flex items-center gap-2 mt-4 text-xs text-cocoa-500">
						<span>{$t('analytics.heatmap.less')}</span>
						<div class="w-3 h-3 rounded-sm bg-gray-100"></div>
						<div class="w-3 h-3 rounded-sm bg-green-200"></div>
						<div class="w-3 h-3 rounded-sm bg-green-300"></div>
						<div class="w-3 h-3 rounded-sm bg-green-400"></div>
						<div class="w-3 h-3 rounded-sm bg-green-500"></div>
						<span>{$t('analytics.heatmap.more')}</span>
					</div>
				</div>
			{/if}
		{/if}
	</main>
</div>
