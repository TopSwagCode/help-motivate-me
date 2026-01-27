<script lang="ts">
	import { t } from 'svelte-i18n';
	import OnboardingAssistant from './OnboardingAssistant.svelte';
	import { createHabitStack } from '$lib/api/habitStacks';
	import type { ExtractedData } from '$lib/api/ai';
	import type { CreatedIdentity } from './ChatIdentityStep.svelte';

	interface Props {
		onnext: () => void;
		onskip: () => void;
		onback: () => void;
		identities?: CreatedIdentity[];
	}

	let { onnext, onskip, onback, identities = [] }: Props = $props();

	interface CreatedItem {
		name: string;
		habitCount: number;
	}

	let createdItems = $state<CreatedItem[]>([]);

	const initialMessage = $derived($t('onboarding.habitStack.initialMessage'));

	interface HabitItem {
		cueDescription: string;
		habitDescription: string;
	}

	interface StackData {
		name: string;
		description?: string;
		triggerCue: string;
		habits?: HabitItem[];
		items?: HabitItem[]; // Legacy format support
		identityId?: string;
		identityName?: string;
	}

	// Helper to resolve identity ID from name if AI provides name instead of ID
	function resolveIdentityId(identityId?: string, identityName?: string): string | undefined {
		if (identityId) return identityId;
		if (identityName && identities.length > 0) {
			const match = identities.find(i =>
				i.name.toLowerCase() === identityName.toLowerCase()
			);
			return match?.id;
		}
		return undefined;
	}

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'habitStack') {
			const stackData = data.data as Record<string, unknown>;

			// New format: stacks array containing multiple habit stacks
			const stacks = stackData.stacks as StackData[] | undefined;

			if (stacks && Array.isArray(stacks)) {
				// New format: multiple stacks in one create action
				for (const stack of stacks) {
					const habits = stack.habits || stack.items || [];
					const identityId = resolveIdentityId(stack.identityId, stack.identityName);
					await createHabitStack({
						name: String(stack.name || ''),
						description: String(stack.description || ''),
						triggerCue: String(stack.triggerCue || ''),
						identityId,
						items: habits.map((item) => ({
							cueDescription: item.cueDescription,
							habitDescription: item.habitDescription
						}))
					});
					createdItems = [...createdItems, {
						name: String(stack.name || 'Habit Stack'),
						habitCount: habits.length
					}];
				}
			} else {
				// Legacy format: single stack at root level
				const habits = (stackData.habits as HabitItem[]) || (stackData.items as HabitItem[]) || [];
				const identityId = resolveIdentityId(
					stackData.identityId as string | undefined,
					stackData.identityName as string | undefined
				);

				await createHabitStack({
					name: String(stackData.name || ''),
					description: String(stackData.description || ''),
					triggerCue: String(stackData.triggerCue || ''),
					identityId,
					items: habits.map((item) => ({
						cueDescription: item.cueDescription,
						habitDescription: item.habitDescription
					}))
				});
				createdItems = [...createdItems, {
					name: String(stackData.name || 'Habit Stack'),
					habitCount: habits.length
				}];
			}
		}
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-3 sm:p-4 border-b bg-white flex-shrink-0">
		<div class="flex items-center gap-2 sm:gap-3">
			<div class="w-8 h-8 sm:w-10 sm:h-10 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
				<span class="text-lg sm:text-xl">🔗</span>
			</div>
			<div class="flex-1 min-w-0">
				<h2 class="font-semibold text-gray-900 text-sm sm:text-base">{$t('onboarding.habitStack.title')}</h2>
				<p class="text-xs sm:text-sm text-gray-500 truncate">
					{#if createdItems.length > 0}
						{$t('onboarding.habitStack.statusCreated', { values: { count: createdItems.length } })}
					{:else}
						{$t('onboarding.habitStack.status')}
					{/if}
				</p>
			</div>
		</div>
		<!-- Created items list -->
		{#if createdItems.length > 0}
			<div class="mt-2 sm:mt-3 flex flex-wrap gap-1.5 sm:gap-2 max-h-16 overflow-y-auto">
				{#each createdItems as item}
					<div 
						class="flex items-center gap-1 sm:gap-1.5 px-2 py-0.5 sm:py-1 rounded-full text-[10px] sm:text-xs font-medium bg-amber-100 text-amber-700"
					>
						<span>🔗</span>
						<span class="truncate max-w-[80px] sm:max-w-[120px]">{item.name}</span>
						<span class="text-amber-500">({item.habitCount})</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="flex-1 overflow-hidden">
		<OnboardingAssistant
			step="habitStack"
			{initialMessage}
			onExtractedData={handleExtractedData}
			onSkip={onskip}
			onNext={onnext}
			onBack={onback}
			showBack={true}
			{identities}
		/>
	</div>
</div>
