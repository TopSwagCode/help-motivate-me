<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { IdentityFeedback } from '$lib/types';

	interface Props {
		feedback: IdentityFeedback;
	}

	let { feedback }: Props = $props();
</script>

<div
	class="absolute z-50 mt-2 left-1/2 -translate-x-1/2 min-w-[200px] bg-gray-900 text-white rounded-xl shadow-xl p-3 text-sm"
	role="tooltip"
>
	<!-- Arrow -->
	<div class="absolute -top-2 left-1/2 -translate-x-1/2 w-0 h-0 border-l-8 border-r-8 border-b-8 border-l-transparent border-r-transparent border-b-gray-900"></div>

	<!-- Header -->
	<div class="flex items-center gap-2 mb-3 pb-2 border-b border-gray-700">
		{#if feedback.icon}
			<span class="text-lg">{feedback.icon}</span>
		{/if}
		<span class="font-semibold">{feedback.totalVotes}</span>
		<span class="text-gray-400">{$t('today.voteBreakdown.votes')}</span>
	</div>

	<!-- Breakdown List -->
	<div class="space-y-2">
		{#if feedback.habitVotes > 0}
			<div class="flex items-center justify-between gap-3">
				<div class="flex items-center gap-2">
					<svg class="w-4 h-4 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
					</svg>
					<span class="text-gray-300">{$t('today.voteBreakdown.habits')}</span>
				</div>
				<span class="font-medium text-blue-400">+{feedback.habitVotes}</span>
			</div>
		{/if}

		{#if feedback.stackBonusVotes > 0}
			<div class="flex items-center justify-between gap-3">
				<div class="flex items-center gap-2">
					<svg class="w-4 h-4 text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
					</svg>
					<span class="text-gray-300">{$t('today.voteBreakdown.stackBonus')}</span>
				</div>
				<span class="font-medium text-yellow-400">+{feedback.stackBonusVotes}</span>
			</div>
		{/if}

		{#if feedback.taskVotes > 0}
			<div class="flex items-center justify-between gap-3">
				<div class="flex items-center gap-2">
					<svg class="w-4 h-4 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
					<span class="text-gray-300">{$t('today.voteBreakdown.tasks')}</span>
				</div>
				<span class="font-medium text-green-400">+{feedback.taskVotes}</span>
			</div>
		{/if}

		{#if feedback.proofVotes > 0}
			<div class="flex items-center justify-between gap-3">
				<div class="flex items-center gap-2">
					<svg class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
					</svg>
					<span class="text-gray-300">{$t('today.voteBreakdown.wins')}</span>
				</div>
				<span class="font-medium text-amber-400">+{feedback.proofVotes}</span>
			</div>
		{/if}
	</div>
</div>
