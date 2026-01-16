<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { IdentityPreviewData } from '$lib/api/aiGeneral';

	interface Props {
		data: IdentityPreviewData;
	}

	let { data }: Props = $props();
</script>

<div class="bg-purple-50 border border-purple-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-purple-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
				/>
			</svg>
			{$t('ai.preview.identity')}
		</div>
	</div>

	<div class="flex items-center gap-3">
		{#if data.icon}
			<div class="text-3xl">{data.icon}</div>
		{/if}
		<div class="flex-1">
			<h3 class="text-lg font-semibold text-gray-900">{data.name}</h3>
			{#if data.description}
				<p class="text-gray-600 mt-1 text-sm">{data.description}</p>
			{/if}
		</div>
		{#if data.color}
			<div
				class="w-8 h-8 rounded-full border-2 border-white shadow-sm"
				style="background-color: {data.color}"
			></div>
		{/if}
	</div>

	{#if data.reasoning}
		<div class="mt-3 p-2 bg-purple-100 rounded-lg">
			<p class="text-xs text-purple-800">
				<span class="font-medium">Why this identity?</span>
				{data.reasoning}
			</p>
		</div>
	{/if}
</div>
