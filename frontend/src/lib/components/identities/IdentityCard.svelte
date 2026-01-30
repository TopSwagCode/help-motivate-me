<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { Identity } from '$lib/types';

	interface Props {
		identity: Identity;
		onclick?: () => void;
	}

	let { identity, onclick }: Props = $props();
</script>

<button
	type="button"
	class="card-hover p-4 text-left w-full border-l-4"
	style="border-left-color: {identity.color || '#6366f1'}"
	onclick={onclick}
>
	<div class="flex items-start gap-3">
		{#if identity.icon}
			<span class="text-2xl">{identity.icon}</span>
		{:else}
			<div
				class="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-medium"
				style="background-color: {identity.color || '#6366f1'}"
			>
				{identity.name.charAt(0).toUpperCase()}
			</div>
		{/if}
		<div class="flex-1 min-w-0">
			<h3 class="font-medium text-gray-900 truncate">{identity.name}</h3>
			{#if identity.description}
				<p class="text-sm text-gray-500 line-clamp-2 mt-1">{identity.description}</p>
			{/if}
			<div class="flex flex-wrap items-center gap-x-3 gap-y-1 mt-2 text-xs text-gray-500">
				<span title={$t('identities.stats.goals')}>🎯 {identity.completedGoals}/{identity.totalGoals}</span>
				<span title={$t('identities.stats.tasks')}>✅ {identity.completedTasks}/{identity.totalTasks}</span>
				<span title={$t('identities.stats.proofs')}>🏆 {identity.totalProofs}</span>
				<span title={$t('identities.stats.commitments')}>📋 {identity.completedDailyCommitments}/{identity.totalDailyCommitments}</span>
			</div>
		</div>
	</div>
</button>
