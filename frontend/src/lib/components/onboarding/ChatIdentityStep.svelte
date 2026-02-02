<script lang="ts">
	import { t } from 'svelte-i18n';
	import OnboardingAssistant from './OnboardingAssistant.svelte';
	import { createIdentity } from '$lib/api/identities';
	import type { ExtractedData } from '$lib/api/ai';

	export interface CreatedIdentity {
		id: string;
		name: string;
		icon: string;
		color: string;
	}

	interface Props {
		onnext: (identities: CreatedIdentity[]) => void;
		onskip: () => void;
	}

	let { onnext, onskip }: Props = $props();

	let createdItems = $state<CreatedIdentity[]>([]);

	const initialMessage = $derived($t('onboarding.identity.initialMessage'));

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'identity') {
			const identityData = data.data as Record<string, unknown>;

			// Support both array format (items) and legacy single item format
			const items = identityData.items as Array<Record<string, unknown>> | undefined;

			if (items && Array.isArray(items)) {
				// New format: multiple items in array
				for (const item of items) {
					const created = await createIdentity({
						name: String(item.name || ''),
						description: String(item.description || ''),
						icon: String(item.icon || ''),
						color: String(item.color || '#d4944c')
					});
					createdItems = [...createdItems, {
						id: created.id,
						name: String(item.name || 'Identity'),
						icon: String(item.icon || '🎯'),
						color: String(item.color || '#d4944c')
					}];
				}
			} else {
				// Legacy format: single item at root level
				const created = await createIdentity({
					name: String(identityData.name || ''),
					description: String(identityData.description || ''),
					icon: String(identityData.icon || ''),
					color: String(identityData.color || '#d4944c')
				});
				createdItems = [...createdItems, {
					id: created.id,
					name: String(identityData.name || 'Identity'),
					icon: String(identityData.icon || '🎯'),
					color: String(identityData.color || '#d4944c')
				}];
			}
		}
	}

	function handleNext() {
		onnext(createdItems);
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-3 sm:p-4 border-b bg-warm-paper flex-shrink-0">
		<div class="flex items-center gap-2 sm:gap-3">
			<div class="w-8 h-8 sm:w-10 sm:h-10 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
				<span class="text-lg sm:text-xl">🎯</span>
			</div>
			<div class="flex-1 min-w-0">
				<h2 class="font-semibold text-cocoa-800 text-sm sm:text-base">{$t('onboarding.identity.title')}</h2>
				<p class="text-xs sm:text-sm text-cocoa-500 truncate">
					{#if createdItems.length > 0}
						{$t('onboarding.identity.statusCreated', { values: { count: createdItems.length } })}
					{:else}
						{$t('onboarding.identity.status')}
					{/if}
				</p>
			</div>
		</div>
		<!-- Created items list -->
		{#if createdItems.length > 0}
			<div class="mt-2 sm:mt-3 flex flex-wrap gap-1.5 sm:gap-2 max-h-16 overflow-y-auto">
				{#each createdItems as item}
					<div 
						class="flex items-center gap-1 sm:gap-1.5 px-2 py-0.5 sm:py-1 rounded-full text-[10px] sm:text-xs font-medium"
						style="background-color: {item.color}20; color: {item.color}"
					>
						<span>{item.icon}</span>
						<span class="truncate max-w-[100px] sm:max-w-[150px]">{item.name}</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="flex-1 overflow-hidden">
		<OnboardingAssistant
			step="identity"
			{initialMessage}
			onExtractedData={handleExtractedData}
			onSkip={onskip}
			onNext={handleNext}
		/>
	</div>
</div>
