<script lang="ts">
	import type { Identity } from '$lib/types';

	interface Props {
		identity: Identity;
		onclick?: () => void;
	}

	let { identity, onclick }: Props = $props();
</script>

<button
	type="button"
	class="card-hover p-4 text-left w-full"
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
			<div class="flex items-center gap-4 mt-2 text-xs text-gray-500">
				<span>{identity.completedTasks}/{identity.totalTasks} tasks</span>
				<span>{identity.tasksCompletedLast7Days} this week</span>
				<span>{identity.completionRate}%</span>
			</div>
		</div>
	</div>
</button>
