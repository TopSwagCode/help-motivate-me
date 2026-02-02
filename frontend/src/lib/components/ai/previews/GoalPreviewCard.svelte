<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { GoalPreviewData } from '$lib/api/aiGeneral';

	// Generic identity type that works with both full Identity and CreatedIdentity
	interface SimpleIdentity {
		id: string;
		name: string;
		icon?: string | null;
	}

	interface Props {
		data: GoalPreviewData;
		identities?: SimpleIdentity[];
		onchange?: (data: GoalPreviewData) => void;
	}

	let { data, identities = [], onchange }: Props = $props();

	// Editing states
	let editingTitle = $state(false);
	let editingDescription = $state(false);

	// Local copy of data for editing
	let localData = $state({ ...data });

	// Sync when parent data changes
	$effect(() => {
		localData = { ...data };
	});

	function updateField<K extends keyof GoalPreviewData>(field: K, value: GoalPreviewData[K]) {
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

	function formatDate(dateStr?: string | null): string {
		if (!dateStr) return '';
		try {
			const date = new Date(dateStr);
			return date.toLocaleDateString(undefined, {
				month: 'long',
				day: 'numeric',
				year: 'numeric'
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

<div class="bg-green-50 border border-green-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-green-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
				/>
			</svg>
			{$t('ai.preview.goal')}
		</div>
	</div>

	<!-- Editable Title -->
	{#if editingTitle}
		<input
			type="text"
			bind:value={localData.title}
			onblur={() => { editingTitle = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Enter') { editingTitle = false; onchange?.(localData); } if (e.key === 'Escape') editingTitle = false; }}
			class="w-full text-lg font-semibold text-cocoa-800 bg-warm-paper border border-green-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-green-400"
			autofocus
		/>
	{:else}
		<button
			type="button"
			onclick={() => editingTitle = true}
			class="text-lg font-semibold text-cocoa-800 hover:bg-green-100 rounded px-1 -mx-1 transition-colors text-left w-full"
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
			class="w-full text-cocoa-600 mt-1 text-sm bg-warm-paper border border-green-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-green-400 resize-none"
			rows="2"
			autofocus
		></textarea>
	{:else}
		<button
			type="button"
			onclick={() => editingDescription = true}
			class="text-cocoa-600 mt-1 text-sm hover:bg-green-100 rounded px-1 -mx-1 transition-colors text-left w-full {!localData.description ? 'italic text-gray-400' : ''}"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.description || $t('ai.preview.addDescription')}
		</button>
	{/if}

	<div class="flex flex-wrap gap-2 mt-3">
		<!-- Editable Target Date -->
		<div class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-green-700 bg-green-100 rounded-full">
			<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
				/>
			</svg>
			<span class="mr-1">{$t('ai.preview.targetDate')}:</span>
			<input
				type="date"
				value={formatDateForInput(localData.targetDate)}
				onchange={(e) => updateField('targetDate', e.currentTarget.value || null)}
				class="bg-transparent border-none outline-none text-xs font-medium text-green-700 cursor-pointer"
			/>
			{#if !localData.targetDate}
				<span class="text-green-500">{$t('ai.preview.setDate')}</span>
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
	</div>
</div>
