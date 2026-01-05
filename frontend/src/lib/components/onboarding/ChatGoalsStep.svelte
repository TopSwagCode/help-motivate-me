<script lang="ts">
	import ChatOnboarding from './ChatOnboarding.svelte';
	import { createGoal } from '$lib/api/goals';
	import type { ExtractedData } from '$lib/api/ai';

	interface Props {
		oncomplete: () => void;
		onskip: () => void;
		onback: () => void;
	}

	let { oncomplete, onskip, onback }: Props = $props();

	let createdCount = $state(0);

	const initialMessage = `Excellent! Finally, let's set some meaningful goals.

Goals give direction to your efforts and help you track progress over time. Great goals are:
• Specific - clear and well-defined
• Meaningful - connected to your identity
• Actionable - can be broken into smaller tasks

For example:
• "Run a marathon by December 2025"
• "Write and publish my first book"
• "Learn conversational Spanish"

You can optionally set target dates and later break goals into smaller tasks.

What goals would you like to work towards? What do you want to achieve?`;

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'goal') {
			const goalData = data.data as Record<string, unknown>;

			await createGoal({
				title: String(goalData.title || ''),
				description: String(goalData.description || ''),
				targetDate: goalData.targetDate ? String(goalData.targetDate) : undefined
			});
			createdCount++;
		}
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-4 border-b bg-white">
		<div class="flex items-center gap-3">
			<div class="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
				<span class="text-xl">🎯</span>
			</div>
			<div>
				<h2 class="font-semibold text-gray-900">Set Your Goals</h2>
				<p class="text-sm text-gray-500">
					{#if createdCount > 0}
						{createdCount} {createdCount === 1 ? 'goal' : 'goals'} created
					{:else}
						What do you want to achieve?
					{/if}
				</p>
			</div>
		</div>
	</div>

	<div class="flex-1 overflow-hidden">
		<ChatOnboarding
			step="goal"
			{initialMessage}
			onExtractedData={handleExtractedData}
			onSkip={onskip}
			onNext={oncomplete}
			onBack={onback}
			showBack={true}
		/>
	</div>
</div>
