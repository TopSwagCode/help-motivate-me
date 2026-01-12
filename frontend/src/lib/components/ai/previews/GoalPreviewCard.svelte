<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { GoalPreviewData } from '$lib/api/aiGeneral';

	interface Props {
		data: GoalPreviewData;
	}

	let { data }: Props = $props();

	function formatDate(dateStr?: string | null): string {
		if (!dateStr) return '';
		try {
			const date = new Date(dateStr);
			return date.toLocaleDateString(undefined, {
				month: 'long',
				day: 'numeric',
				year: 'numeric'
			});
		} catch {
			return dateStr;
		}
	}
</script>

<div class="bg-green-50 border border-green-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-green-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
				/>
			</svg>
			{$t('ai.preview.goal')}
		</div>
	</div>

	<h3 class="text-lg font-semibold text-gray-900">{data.title}</h3>

	{#if data.description}
		<p class="text-gray-600 mt-1 text-sm">{data.description}</p>
	{/if}

	<div class="flex flex-wrap gap-2 mt-3">
		{#if data.targetDate}
			<span class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-green-700 bg-green-100 rounded-full">
				<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
					/>
				</svg>
				{$t('ai.preview.targetDate')}: {formatDate(data.targetDate)}
			</span>
		{/if}
	</div>
</div>
