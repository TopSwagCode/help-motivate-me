<script lang="ts">
	import { getAiUsageStats, getAiUsageLogs } from '$lib/api/admin';
	import type { AiUsageStats, AiUsageLog, PaginatedResponse } from '$lib/types';
	import { locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let stats = $state<AiUsageStats | null>(null);
	let logs = $state<PaginatedResponse<AiUsageLog> | null>(null);
	let loading = $state(true);
	let error = $state('');
	let currentPage = $state(1);
	const pageSize = 50;

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			const [statsData, logsData] = await Promise.all([
				getAiUsageStats(),
				getAiUsageLogs({ page: currentPage, pageSize })
			]);
			stats = statsData;
			logs = logsData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load AI usage data';
		} finally {
			loading = false;
		}
	}

	function formatCurrency(value: number): string {
		return `$${value.toFixed(4)}`;
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

	function getUsagePercentage(current: number, limit: number): number {
		if (limit <= 0) return 0;
		return Math.min((current / limit) * 100, 100);
	}

	function getProgressColor(percentage: number): string {
		if (percentage >= 90) return 'bg-red-500';
		if (percentage >= 70) return 'bg-yellow-500';
		return 'bg-green-500';
	}

	async function changePage(newPage: number) {
		if (newPage < 1 || (logs && newPage > logs.totalPages)) return;
		currentPage = newPage;
		try {
			logs = await getAiUsageLogs({ page: currentPage, pageSize });
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load page';
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
{:else if stats && logs}
	<!-- Header -->
	<div class="mb-6">
		<h2 class="text-lg font-semibold text-gray-900">AI Usage & Budget</h2>
		<p class="text-sm text-gray-500 mt-1">Monitor AI API costs and enforce usage limits</p>
	</div>

	<!-- Stats Cards -->
	<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
		<!-- Total Estimated (All Time) -->
		<div class="card p-4">
			<div class="flex items-center justify-between mb-2">
				<p class="text-sm text-gray-500">Total Estimated (All Time)</p>
				<div class="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
					<svg class="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
					</svg>
				</div>
			</div>
			<p class="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalEstimatedAllTime)}</p>
		</div>

		<!-- Total Actual (All Time) -->
		<div class="card p-4">
			<div class="flex items-center justify-between mb-2">
				<p class="text-sm text-gray-500">Total Actual (All Time)</p>
				<div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
				</div>
			</div>
			<p class="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalActualAllTime)}</p>
		</div>

		<!-- Last 30 Days Estimated (with progress) -->
		<div class="card p-4">
			<div class="flex items-center justify-between mb-2">
				<p class="text-sm text-gray-500">Last 30 Days (Estimated)</p>
				<div class="w-8 h-8 bg-purple-100 rounded-full flex items-center justify-center">
					<svg class="w-4 h-4 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
					</svg>
				</div>
			</div>
			<p class="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalEstimatedLast30Days)}</p>
			<div class="mt-2">
				<div class="flex justify-between text-xs text-gray-500 mb-1">
					<span>{getUsagePercentage(stats.totalEstimatedLast30Days, stats.globalLimitLast30DaysUsd).toFixed(1)}% of limit</span>
					<span>{formatCurrency(stats.globalLimitLast30DaysUsd)}</span>
				</div>
				<div class="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
					<div class="{getProgressColor(getUsagePercentage(stats.totalEstimatedLast30Days, stats.globalLimitLast30DaysUsd))} h-full rounded-full transition-all" style="width: {getUsagePercentage(stats.totalEstimatedLast30Days, stats.globalLimitLast30DaysUsd)}%"></div>
				</div>
			</div>
		</div>

		<!-- Last 30 Days Actual (with progress) -->
		<div class="card p-4">
			<div class="flex items-center justify-between mb-2">
				<p class="text-sm text-gray-500">Last 30 Days (Actual)</p>
				<div class="w-8 h-8 bg-orange-100 rounded-full flex items-center justify-center">
					<svg class="w-4 h-4 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
					</svg>
				</div>
			</div>
			<p class="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalActualLast30Days)}</p>
			<div class="mt-2">
				<div class="flex justify-between text-xs text-gray-500 mb-1">
					<span>{getUsagePercentage(stats.totalActualLast30Days, stats.globalLimitLast30DaysUsd).toFixed(1)}% of limit</span>
					<span>{formatCurrency(stats.globalLimitLast30DaysUsd)}</span>
				</div>
				<div class="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
					<div class="{getProgressColor(getUsagePercentage(stats.totalActualLast30Days, stats.globalLimitLast30DaysUsd))} h-full rounded-full transition-all" style="width: {getUsagePercentage(stats.totalActualLast30Days, stats.globalLimitLast30DaysUsd)}%"></div>
				</div>
			</div>
		</div>
	</div>

	<!-- Budget Limits Info -->
	<div class="card p-4 mb-6 bg-gray-50">
		<h3 class="text-sm font-medium text-gray-700 mb-2">Budget Configuration</h3>
		<div class="flex flex-wrap gap-6 text-sm text-gray-600">
			<div>
				<span class="font-medium">Global Limit (30 days):</span> {formatCurrency(stats.globalLimitLast30DaysUsd)}
			</div>
			<div>
				<span class="font-medium">Per-User Limit (30 days):</span> {formatCurrency(stats.perUserLimitLast30DaysUsd)}
			</div>
		</div>
	</div>

	<!-- Usage Logs Table -->
	<div class="card">
		<div class="p-6 border-b border-gray-200">
			<h3 class="text-lg font-semibold text-gray-900">Usage Logs</h3>
			<p class="text-sm text-gray-500 mt-1">All AI API calls with cost breakdown</p>
		</div>

		<div class="overflow-x-auto">
			<table class="w-full">
				<thead class="bg-gray-50">
					<tr>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
						<th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Estimated</th>
						<th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actual</th>
					</tr>
				</thead>
				<tbody class="bg-white divide-y divide-gray-200">
					{#each logs.items as log (log.id)}
						<tr class="hover:bg-gray-50">
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
								{formatDateTime(log.createdAt)}
							</td>
							<td class="px-6 py-4 whitespace-nowrap">
								<div class="flex items-center">
									<div class="w-7 h-7 rounded-full bg-primary-100 flex items-center justify-center text-primary-600 font-medium text-xs">
										{log.username.charAt(0).toUpperCase()}
									</div>
									<span class="ml-2 text-sm text-gray-900">{log.username}</span>
								</div>
							</td>
							<td class="px-6 py-4 whitespace-nowrap">
								{#if log.requestType === 'transcription'}
									<span class="px-2 py-1 text-xs font-medium rounded-full bg-purple-100 text-purple-700">
										Voice
									</span>
								{:else}
									<span class="px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-700">
										Text
									</span>
								{/if}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-right font-mono">
								{formatCurrency(log.estimatedCostUsd)}
							</td>
							<td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right font-mono font-medium">
								{formatCurrency(log.actualCostUsd)}
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="5" class="px-6 py-8 text-center text-gray-500">
								No AI usage logs found
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<!-- Pagination -->
		{#if logs.totalPages > 1}
			<div class="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
				<div class="text-sm text-gray-500">
					Showing {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, logs.totalCount)} of {logs.totalCount}
				</div>
				<div class="flex gap-2">
					<button
						onclick={() => changePage(currentPage - 1)}
						disabled={currentPage === 1}
						class="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
					>
						Previous
					</button>
					<span class="px-3 py-1 text-sm text-gray-600">
						Page {currentPage} of {logs.totalPages}
					</span>
					<button
						onclick={() => changePage(currentPage + 1)}
						disabled={currentPage >= logs.totalPages}
						class="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
					>
						Next
					</button>
				</div>
			</div>
		{/if}
	</div>
{/if}
