<script lang="ts">
	import ChatOnboarding from './ChatOnboarding.svelte';
	import { createHabitStack } from '$lib/api/habitStacks';
	import type { ExtractedData } from '$lib/api/ai';

	interface Props {
		onnext: () => void;
		onskip: () => void;
		onback: () => void;
	}

	let { onnext, onskip, onback }: Props = $props();

	let createdCount = $state(0);

	const initialMessage = `Great! Now let's build some habit stacks.

Habit stacking is one of the most effective ways to build new habits. The idea is simple: link a new habit to an existing one using this formula:

"After I [CURRENT HABIT], I will [NEW HABIT]"

For example, a morning routine might look like:
• After I wake up, I will make my bed
• After I make my bed, I will do 5 minutes of stretching
• After I stretch, I will drink a glass of water

You can chain multiple habits together to create powerful routines!

What daily routines do you have? What new habits would you like to build into them?`;

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'habitStack') {
			const stackData = data.data as Record<string, unknown>;
			const items = (stackData.items as Array<{ cueDescription: string; habitDescription: string }>) || [];

			await createHabitStack({
				name: String(stackData.name || ''),
				description: String(stackData.description || ''),
				triggerCue: String(stackData.triggerCue || ''),
				items: items.map((item) => ({
					cueDescription: item.cueDescription,
					habitDescription: item.habitDescription
				}))
			});
			createdCount++;
		}
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-4 border-b bg-white">
		<div class="flex items-center gap-3">
			<div class="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
				<span class="text-xl">🔗</span>
			</div>
			<div>
				<h2 class="font-semibold text-gray-900">Build Habit Stacks</h2>
				<p class="text-sm text-gray-500">
					{#if createdCount > 0}
						{createdCount} habit {createdCount === 1 ? 'stack' : 'stacks'} created
					{:else}
						Chain habits together for powerful routines
					{/if}
				</p>
			</div>
		</div>
	</div>

	<div class="flex-1 overflow-hidden">
		<ChatOnboarding
			step="habitStack"
			{initialMessage}
			onExtractedData={handleExtractedData}
			onSkip={onskip}
			onNext={onnext}
			onBack={onback}
			showBack={true}
		/>
	</div>
</div>
