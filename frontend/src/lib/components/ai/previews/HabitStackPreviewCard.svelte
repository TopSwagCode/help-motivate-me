<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { HabitStackPreviewData } from '$lib/api/aiGeneral';

	interface Props {
		data: HabitStackPreviewData;
	}

	let { data }: Props = $props();
</script>

<div class="bg-amber-50 border border-amber-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-amber-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
				/>
			</svg>
			{$t('ai.preview.habitStack')}
		</div>
	</div>

	<h3 class="text-lg font-semibold text-gray-900">{data.name}</h3>

	{#if data.description}
		<p class="text-gray-600 mt-1 text-sm">{data.description}</p>
	{/if}

	<!-- Trigger cue -->
	{#if data.triggerCue}
		<div class="mt-3 flex items-center gap-2 text-sm text-amber-700">
			<span class="font-medium">{$t('ai.preview.trigger')}:</span>
			<span class="italic">{data.triggerCue}</span>
		</div>
	{/if}

	<!-- Habits list -->
	{#if data.habits && data.habits.length > 0}
		<div class="mt-3 space-y-2">
			{#each data.habits as habit, index}
				<div class="flex items-start gap-2 text-sm">
					<div
						class="flex-shrink-0 w-5 h-5 rounded-full bg-amber-200 text-amber-700 flex items-center justify-center text-xs font-medium"
					>
						{index + 1}
					</div>
					<div>
						<span class="text-gray-500">{habit.cueDescription}</span>
						<span class="mx-1 text-gray-400">→</span>
						<span class="font-medium text-gray-900">{habit.habitDescription}</span>
					</div>
				</div>
			{/each}
		</div>
	{/if}

	<!-- Identity link -->
	{#if data.identityName}
		<div class="mt-3">
			<span class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-purple-700 bg-purple-100 rounded-full">
				{data.identityName}
			</span>
		</div>
	{/if}
</div>
