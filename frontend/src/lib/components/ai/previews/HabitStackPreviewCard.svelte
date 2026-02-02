<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { HabitStackPreviewData } from '$lib/api/aiGeneral';
	import IdentityRecommendationBadge from '../IdentityRecommendationBadge.svelte';

	// Generic identity type that works with both full Identity and CreatedIdentity
	interface SimpleIdentity {
		id: string;
		name: string;
		icon?: string | null;
	}

	interface Props {
		data: HabitStackPreviewData;
		identities?: SimpleIdentity[];
		onchange?: (data: HabitStackPreviewData) => void;
	}

	let { data, identities = [], onchange }: Props = $props();

	// Editing states
	let editingName = $state(false);
	let editingDescription = $state(false);
	let editingTrigger = $state(false);
	let editingHabitIndex = $state<number | null>(null);

	// Local copy of data for editing
	let localData = $state({ ...data, habits: [...(data.habits || [])] });

	// Sync when parent data changes
	$effect(() => {
		localData = { ...data, habits: [...(data.habits || [])] };
	});

	function updateField<K extends keyof HabitStackPreviewData>(field: K, value: HabitStackPreviewData[K]) {
		localData = { ...localData, [field]: value };
		onchange?.(localData);
	}

	function handleIdentityChange(identityId: string) {
		const identity = identities.find(i => i.id === identityId);
		localData = {
			...localData,
			identityId: identityId || null,
			identityName: identity?.name ?? null
		};
		onchange?.(localData);
	}

	function updateHabit(index: number, field: 'cueDescription' | 'habitDescription', value: string) {
		const newHabits = [...localData.habits];
		newHabits[index] = { ...newHabits[index], [field]: value };
		localData = { ...localData, habits: newHabits };
		onchange?.(localData);
	}

	function addHabit() {
		const lastHabit = localData.habits[localData.habits.length - 1];
		const newCue = lastHabit ? lastHabit.habitDescription : '';
		const newHabits = [...localData.habits, { cueDescription: newCue, habitDescription: '' }];
		localData = { ...localData, habits: newHabits };
		onchange?.(localData);
		// Auto-focus the new habit
		editingHabitIndex = newHabits.length - 1;
	}

	function removeHabit(index: number) {
		if (localData.habits.length <= 1) return; // Keep at least one habit
		const newHabits = localData.habits.filter((_, i) => i !== index);
		localData = { ...localData, habits: newHabits };
		onchange?.(localData);
		editingHabitIndex = null;
	}
</script>

<div class="bg-amber-50 border border-amber-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-amber-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
				/>
			</svg>
			{$t('ai.preview.habitStack')}
		</div>
	</div>

	<!-- Editable Name -->
	{#if editingName}
		<input
			type="text"
			bind:value={localData.name}
			onblur={() => { editingName = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Enter') { editingName = false; onchange?.(localData); } if (e.key === 'Escape') editingName = false; }}
			class="w-full text-lg font-semibold text-cocoa-800 bg-warm-paper border border-amber-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-amber-400"
			autofocus
		/>
	{:else}
		<button
			type="button"
			onclick={() => editingName = true}
			class="text-lg font-semibold text-cocoa-800 hover:bg-amber-100 rounded px-1 -mx-1 transition-colors text-left w-full"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.name || $t('ai.preview.notSet')}
		</button>
	{/if}

	<!-- Editable Description -->
	{#if editingDescription}
		<textarea
			bind:value={localData.description}
			onblur={() => { editingDescription = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Escape') editingDescription = false; }}
			class="w-full text-cocoa-600 mt-1 text-sm bg-warm-paper border border-amber-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-amber-400 resize-none"
			rows="2"
			autofocus
		></textarea>
	{:else}
		<button
			type="button"
			onclick={() => editingDescription = true}
			class="text-cocoa-600 mt-1 text-sm hover:bg-amber-100 rounded px-1 -mx-1 transition-colors text-left w-full {!localData.description ? 'italic text-gray-400' : ''}"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.description || $t('ai.preview.addDescription')}
		</button>
	{/if}

	<!-- Editable Trigger cue -->
	<div class="mt-3 flex items-center gap-2 text-sm text-amber-700">
		<span class="font-medium">{$t('ai.preview.trigger')}:</span>
		{#if editingTrigger}
			<input
				type="text"
				bind:value={localData.triggerCue}
				onblur={() => { editingTrigger = false; onchange?.(localData); }}
				onkeydown={(e) => { if (e.key === 'Enter') { editingTrigger = false; onchange?.(localData); } if (e.key === 'Escape') editingTrigger = false; }}
				class="flex-1 italic bg-warm-paper border border-amber-300 rounded px-2 py-0.5 outline-none focus:ring-2 focus:ring-amber-400 text-sm"
				autofocus
			/>
		{:else}
			<button
				type="button"
				onclick={() => editingTrigger = true}
				class="italic hover:bg-amber-100 rounded px-1 transition-colors {!localData.triggerCue ? 'text-amber-500' : ''}"
				title={$t('ai.preview.clickToEdit')}
			>
				{localData.triggerCue || $t('ai.preview.notSet')}
			</button>
		{/if}
	</div>

	<!-- Editable Habits list -->
	{#if localData.habits && localData.habits.length > 0}
		<div class="mt-3 space-y-2">
			{#each localData.habits as habit, index}
				<div class="flex items-start gap-2 text-sm group">
					<div
						class="flex-shrink-0 w-5 h-5 rounded-full bg-amber-200 text-amber-700 flex items-center justify-center text-xs font-medium"
					>
						{index + 1}
					</div>
					<div class="flex-1">
						{#if editingHabitIndex === index}
							<div class="space-y-1">
								<input
									type="text"
									value={habit.cueDescription}
									onchange={(e) => updateHabit(index, 'cueDescription', e.currentTarget.value)}
									onblur={() => { editingHabitIndex = null; onchange?.(localData); }}
									class="w-full text-cocoa-500 bg-warm-paper border border-amber-300 rounded px-2 py-0.5 outline-none focus:ring-2 focus:ring-amber-400 text-sm"
									placeholder="After I..."
								/>
								<div class="flex items-center gap-1">
									<span class="text-gray-400">→</span>
									<input
										type="text"
										value={habit.habitDescription}
										onchange={(e) => updateHabit(index, 'habitDescription', e.currentTarget.value)}
										onblur={() => { editingHabitIndex = null; onchange?.(localData); }}
										class="flex-1 font-medium text-cocoa-800 bg-warm-paper border border-amber-300 rounded px-2 py-0.5 outline-none focus:ring-2 focus:ring-amber-400 text-sm"
										placeholder="I will..."
									/>
								</div>
							</div>
						{:else}
							<button
								type="button"
								onclick={() => editingHabitIndex = index}
								class="text-left hover:bg-amber-100 rounded px-1 -mx-1 transition-colors w-full"
								title={$t('ai.preview.clickToEdit')}
							>
								<span class="text-cocoa-500">{habit.cueDescription}</span>
								<span class="mx-1 text-gray-400">→</span>
								<span class="font-medium text-cocoa-800">{habit.habitDescription}</span>
							</button>
						{/if}
					</div>
					<!-- Remove button -->
					{#if localData.habits.length > 1}
						<button
							type="button"
							onclick={() => removeHabit(index)}
							class="flex-shrink-0 w-5 h-5 rounded-full text-gray-400 hover:text-red-500 hover:bg-red-50 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
							title={$t('ai.preview.removeStep')}
						>
							<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
							</svg>
						</button>
					{/if}
				</div>
			{/each}
			<!-- Add habit button -->
			<button
				type="button"
				onclick={addHabit}
				class="flex items-center gap-2 text-sm text-amber-600 hover:text-amber-700 hover:bg-amber-100 rounded px-2 py-1 transition-colors w-full"
			>
				<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				<span>{$t('ai.preview.addStep')}</span>
			</button>
		</div>
	{/if}

	<!-- Editable Identity link -->
	<div class="mt-3 flex flex-wrap gap-2">
		{#if identities.length > 0}
			<div class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-purple-700 bg-purple-100 rounded-full">
				<select
					value={localData.identityId || ''}
					onchange={(e) => handleIdentityChange(e.currentTarget.value)}
					class="bg-transparent border-none outline-none text-xs font-medium text-purple-700 cursor-pointer appearance-none pr-4"
				>
					<option value="">{$t('ai.preview.none')}</option>
					{#each identities as identity}
						<option value={identity.id}>{identity.icon ? `${identity.icon} ` : ''}{identity.name}</option>
					{/each}
				</select>
				{#if !localData.identityId}
					<span class="text-purple-500 -ml-3">{$t('ai.preview.selectIdentity')}</span>
				{/if}
			</div>
		{:else if localData.identityName}
			<span class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-purple-700 bg-purple-100 rounded-full">
				{localData.identityName}
			</span>
		{/if}
	</div>

	{#if localData.identityName && localData.reasoning}
		<IdentityRecommendationBadge identityName={localData.identityName} reasoning={localData.reasoning} />
	{/if}
</div>
