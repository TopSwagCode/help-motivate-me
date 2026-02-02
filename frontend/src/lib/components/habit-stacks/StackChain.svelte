<script lang="ts">
	import type { HabitStack } from '$lib/types';

	interface Props {
		stack: HabitStack;
		onclick?: () => void;
	}

	let { stack, onclick }: Props = $props();
</script>

<button type="button" class="card-hover p-4 text-left w-full" onclick={onclick}>
	<div class="flex items-center justify-between mb-3">
		<h3 class="font-medium text-cocoa-800">{stack.name}</h3>
		{#if !stack.isActive}
			<span class="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-cocoa-600">Inactive</span>
		{/if}
	</div>

	{#if stack.items.length > 0}
		<div class="space-y-2">
			{#each stack.items as item, i (item.id)}
				<div class="relative pl-4">
					{#if i > 0}
						<div
							class="absolute left-1.5 -top-2 w-0.5 h-4 bg-gray-200"
						></div>
					{/if}
					<div
						class="absolute left-0 top-1.5 w-3 h-3 rounded-full border-2 border-primary-500 bg-warm-paper"
					></div>
					<div class="bg-warm-cream rounded-2xl p-2 text-sm">
						<p class="text-cocoa-500">
							<span class="font-medium text-cocoa-700">After I</span>
							{item.cueDescription}
						</p>
						<p class="text-cocoa-800 mt-0.5">
							<span class="font-medium text-primary-600">I will</span>
							{item.habitDescription}
						</p>
					</div>
				</div>
			{/each}
		</div>
	{:else}
		<p class="text-sm text-cocoa-500 italic">No habits in this stack yet</p>
	{/if}

	<p class="text-xs text-gray-400 mt-3">{stack.items.length} habit{stack.items.length !== 1 ? 's' : ''} in chain</p>
</button>
