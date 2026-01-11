<script lang="ts">
	import { t } from 'svelte-i18n';
	import ChatOnboarding from './ChatOnboarding.svelte';
	import { createGoal } from '$lib/api/goals';
	import type { ExtractedData } from '$lib/api/ai';

	interface Props {
		oncomplete: () => void;
		onskip: () => void;
		onback: () => void;
	}

	let { oncomplete, onskip, onback }: Props = $props();

	interface CreatedItem {
		title: string;
		targetDate?: string;
	}

	let createdItems = $state<CreatedItem[]>([]);

	const initialMessage = $derived($t('onboarding.goals.initialMessage'));

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'goal') {
			const goalData = data.data as Record<string, unknown>;
			
			// Support both array format (items) and legacy single item format
			const items = goalData.items as Array<Record<string, unknown>> | undefined;
			
			if (items && Array.isArray(items)) {
				// New format: multiple items in array
				for (const item of items) {
					await createGoal({
						title: String(item.title || ''),
						description: String(item.description || ''),
						targetDate: item.targetDate ? String(item.targetDate) : undefined
					});
					createdItems = [...createdItems, {
						title: String(item.title || 'Goal'),
						targetDate: item.targetDate ? String(item.targetDate) : undefined
					}];
				}
			} else {
				// Legacy format: single item at root level
				await createGoal({
					title: String(goalData.title || ''),
					description: String(goalData.description || ''),
					targetDate: goalData.targetDate ? String(goalData.targetDate) : undefined
				});
				createdItems = [...createdItems, {
					title: String(goalData.title || 'Goal'),
					targetDate: goalData.targetDate ? String(goalData.targetDate) : undefined
				}];
			}
		}
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-3 sm:p-4 border-b bg-white flex-shrink-0">
		<div class="flex items-center gap-2 sm:gap-3">
			<div class="w-8 h-8 sm:w-10 sm:h-10 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
				<span class="text-lg sm:text-xl">🎯</span>
			</div>
			<div class="flex-1 min-w-0">
				<h2 class="font-semibold text-gray-900 text-sm sm:text-base">{$t('onboarding.goals.title')}</h2>
				<p class="text-xs sm:text-sm text-gray-500 truncate">
					{#if createdItems.length > 0}
						{$t('onboarding.goals.statusCreated', { values: { count: createdItems.length } })}
					{:else}
						{$t('onboarding.goals.status')}
					{/if}
				</p>
			</div>
		</div>
		<!-- Created items list -->
		{#if createdItems.length > 0}
			<div class="mt-2 sm:mt-3 flex flex-wrap gap-1.5 sm:gap-2 max-h-16 overflow-y-auto">
				{#each createdItems as item}
					<div 
						class="flex items-center gap-1 sm:gap-1.5 px-2 py-0.5 sm:py-1 rounded-full text-[10px] sm:text-xs font-medium bg-green-100 text-green-700"
					>
						<span>🎯</span>
						<span class="truncate max-w-[100px] sm:max-w-[150px]">{item.title}</span>
						{#if item.targetDate}
							<span class="text-green-500 hidden sm:inline">({item.targetDate})</span>
						{/if}
					</div>
				{/each}
			</div>
		{/if}
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
