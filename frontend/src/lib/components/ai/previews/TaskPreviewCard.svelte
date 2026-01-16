<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { TaskPreviewData } from '$lib/api/aiGeneral';
	import IdentityRecommendationBadge from '../IdentityRecommendationBadge.svelte';

	interface Props {
		data: TaskPreviewData;
	}

	let { data }: Props = $props();

	function formatDate(dateStr?: string | null): string {
		if (!dateStr) return '';
		try {
			const date = new Date(dateStr);
			return date.toLocaleDateString(undefined, {
				weekday: 'short',
				month: 'short',
				day: 'numeric'
			});
		} catch {
			return dateStr;
		}
	}
</script>

<div class="bg-blue-50 border border-blue-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-blue-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"
				/>
			</svg>
			{$t('ai.preview.task')}
		</div>
	</div>

	<h3 class="text-lg font-semibold text-gray-900">{data.title}</h3>

	{#if data.description}
		<p class="text-gray-600 mt-1 text-sm">{data.description}</p>
	{/if}

	<div class="flex flex-wrap gap-2 mt-3">
		{#if data.dueDate}
			<span class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-blue-700 bg-blue-100 rounded-full">
				<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
					/>
				</svg>
				{formatDate(data.dueDate)}
			</span>
		{/if}
		{#if data.identityName}
			<span class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-purple-700 bg-purple-100 rounded-full">
				{data.identityName}
			</span>
		{/if}
	</div>

	{#if data.identityName && data.reasoning}
		<IdentityRecommendationBadge identityName={data.identityName} reasoning={data.reasoning} />
	{/if}
</div>
