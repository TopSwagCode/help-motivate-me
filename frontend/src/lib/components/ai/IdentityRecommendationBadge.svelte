<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		identityName: string;
		reasoning?: string | null;
	}

	let { identityName, reasoning }: Props = $props();
</script>

<div
	class="mt-3 p-3 bg-gradient-to-r from-purple-50 to-pink-50 border border-purple-200 rounded-lg"
>
	<div class="flex items-start gap-2">
		<svg
			class="w-5 h-5 text-purple-600 flex-shrink-0 mt-0.5"
			fill="none"
			stroke="currentColor"
			viewBox="0 0 24 24"
		>
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2"
				d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
			/>
		</svg>
		<div class="flex-1">
			<p class="text-sm font-medium text-purple-900">
				{$t('ai.preview.identityRecommendation')}: <span class="text-purple-700"
					>{identityName}</span
				>
			</p>
			{#if reasoning}
				<p class="text-xs text-purple-600 mt-1">{reasoning}</p>
			{/if}
		</div>
	</div>
</div>
