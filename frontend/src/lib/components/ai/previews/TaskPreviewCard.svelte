<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { TaskPreviewData } from '$lib/api/aiGeneral';
	import type { Identity } from '$lib/types/identity';
	import type { Goal } from '$lib/types/goal';
	import IdentityRecommendationBadge from '../IdentityRecommendationBadge.svelte';

	interface Props {
		data: TaskPreviewData;
		identities?: Identity[];
		goals?: Goal[];
		onchange?: (data: TaskPreviewData) => void;
	}

	let { data, identities = [], goals = [], onchange }: Props = $props();

	// Editing states
	let editingTitle = $state(false);
	let editingDescription = $state(false);

	// Local copy of data for editing
	let localData = $state({ ...data });

	// Sync when parent data changes
	$effect(() => {
		localData = { ...data };
	});

	function updateField<K extends keyof TaskPreviewData>(field: K, value: TaskPreviewData[K]) {
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

	function handleGoalChange(goalId: string) {
		const goal = goals.find(g => g.id === goalId);
		localData = {
			...localData,
			goalId: goalId || null,
			goalTitle: goal?.title ?? null
		};
		onchange?.(localData);
	}

	function formatDate(dateStr?: string | null): string {
		if (!dateStr) return '';
		try {
			const date = new Date(dateStr);
			return date.toLocaleDateString(undefined, {
				weekday: 'short',
				month: 'short',
				day: 'numeric'
			});
		} catch {
			return dateStr;
		}
	}

	function formatDateForInput(dateStr?: string | null): string {
		if (!dateStr) return '';
		try {
			const date = new Date(dateStr);
			return date.toISOString().split('T')[0];
		} catch {
			return '';
		}
	}
</script>

<div class="bg-blue-50 border border-blue-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-blue-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"
				/>
			</svg>
			{$t('ai.preview.task')}
		</div>
	</div>

	<!-- Editable Title -->
	{#if editingTitle}
		<input
			type="text"
			bind:value={localData.title}
			onblur={() => { editingTitle = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Enter') { editingTitle = false; onchange?.(localData); } if (e.key === 'Escape') editingTitle = false; }}
			class="w-full text-lg font-semibold text-cocoa-800 bg-warm-paper border border-blue-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-blue-400"
			autofocus
		/>
	{:else}
		<button
			type="button"
			onclick={() => editingTitle = true}
			class="text-lg font-semibold text-cocoa-800 hover:bg-blue-100 rounded px-1 -mx-1 transition-colors text-left w-full"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.title || $t('ai.preview.notSet')}
		</button>
	{/if}

	<!-- Editable Description -->
	{#if editingDescription}
		<textarea
			bind:value={localData.description}
			onblur={() => { editingDescription = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Escape') editingDescription = false; }}
			class="w-full text-cocoa-600 mt-1 text-sm bg-warm-paper border border-blue-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-blue-400 resize-none"
			rows="2"
			autofocus
		></textarea>
	{:else}
		<button
			type="button"
			onclick={() => editingDescription = true}
			class="text-cocoa-600 mt-1 text-sm hover:bg-blue-100 rounded px-1 -mx-1 transition-colors text-left w-full {!localData.description ? 'italic text-gray-400' : ''}"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.description || $t('ai.preview.addDescription')}
		</button>
	{/if}

	<div class="flex flex-wrap gap-2 mt-3">
		<!-- Editable Due Date -->
		<div class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-blue-700 bg-blue-100 rounded-full">
			<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
				/>
			</svg>
			<input
				type="date"
				value={formatDateForInput(localData.dueDate)}
				onchange={(e) => updateField('dueDate', e.currentTarget.value || null)}
				class="bg-transparent border-none outline-none text-xs font-medium text-blue-700 cursor-pointer"
			/>
			{#if !localData.dueDate}
				<span class="text-blue-500">{$t('ai.preview.setDate')}</span>
			{/if}
		</div>

		<!-- Editable Identity -->
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

		<!-- Editable Parent Goal -->
		{#if goals.length > 0}
			<div class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-green-700 bg-green-100 rounded-full">
				<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
				</svg>
				<select
					value={localData.goalId || ''}
					onchange={(e) => handleGoalChange(e.currentTarget.value)}
					class="bg-transparent border-none outline-none text-xs font-medium text-green-700 cursor-pointer appearance-none pr-4 max-w-32 truncate"
				>
					<option value="">{$t('ai.preview.selectGoal')}</option>
					{#each goals as goal}
						<option value={goal.id}>{goal.title}</option>
					{/each}
				</select>
			</div>
		{/if}
	</div>

	{#if localData.identityName && localData.reasoning}
		<IdentityRecommendationBadge identityName={localData.identityName} reasoning={localData.reasoning} />
	{/if}
</div>
