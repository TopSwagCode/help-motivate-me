<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { IdentityProofPreviewData } from '$lib/api/aiGeneral';
	import type { Identity } from '$lib/types/identity';

	interface Props {
		data: IdentityProofPreviewData;
		identities?: Identity[];
		onchange?: (data: IdentityProofPreviewData) => void;
	}

	let { data, identities = [], onchange }: Props = $props();

	// Editing states
	let editingDescription = $state(false);

	// Local copy of data for editing
	let localData = $state({ ...data });

	// Sync when parent data changes
	$effect(() => {
		localData = { ...data };
	});

	function updateField<K extends keyof IdentityProofPreviewData>(field: K, value: IdentityProofPreviewData[K]) {
		localData = { ...localData, [field]: value };
		onchange?.(localData);
	}

	function handleIdentityChange(e: Event) {
		const select = e.target as HTMLSelectElement;
		const selectedIdentity = identities.find(i => i.id === select.value);
		if (selectedIdentity) {
			localData = {
				...localData,
				identityId: selectedIdentity.id,
				identityName: selectedIdentity.name
			};
			onchange?.(localData);
		}
	}

	function handleIntensityChange(e: Event) {
		const select = e.target as HTMLSelectElement;
		updateField('intensity', select.value as 'Easy' | 'Moderate' | 'Hard');
	}

	const intensityColors = {
		Easy: 'bg-green-100 text-green-700',
		Moderate: 'bg-yellow-100 text-yellow-700',
		Hard: 'bg-red-100 text-red-700'
	};

	const intensityEmoji = {
		Easy: '🌱',
		Moderate: '💪',
		Hard: '🔥'
	};
</script>

<div class="bg-amber-50 border border-amber-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-amber-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
				/>
			</svg>
			{$t('ai.preview.identityProof')}
		</div>
	</div>

	<!-- Identity Selection -->
	<div class="mb-3">
		<label class="block text-xs text-cocoa-500 mb-1">{$t('ai.preview.forIdentity')}</label>
		{#if identities.length > 0}
			<select
				value={localData.identityId}
				onchange={handleIdentityChange}
				class="w-full px-2 py-1.5 border border-amber-300 rounded-2xl text-sm bg-warm-paper focus:ring-2 focus:ring-amber-400 focus:outline-none"
			>
				{#each identities as identity}
					<option value={identity.id}>
						{identity.icon || ''} {identity.name}
					</option>
				{/each}
			</select>
		{:else}
			<div class="flex items-center gap-2 text-sm text-cocoa-700">
				<span>{localData.identityName}</span>
			</div>
		{/if}
	</div>

	<!-- Editable Description -->
	<div class="mb-3">
		<label class="block text-xs text-cocoa-500 mb-1">{$t('ai.preview.whatYouDid')}</label>
		{#if editingDescription}
			<textarea
				bind:value={localData.description}
				onblur={() => { editingDescription = false; onchange?.(localData); }}
				onkeydown={(e) => { if (e.key === 'Escape') editingDescription = false; }}
				class="w-full text-sm text-cocoa-700 bg-warm-paper border border-amber-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-amber-400 resize-none"
				rows="2"
				autofocus
			></textarea>
		{:else}
			<button
				type="button"
				onclick={() => editingDescription = true}
				class="text-sm text-cocoa-700 hover:bg-amber-100 rounded px-1 -mx-1 transition-colors text-left w-full min-h-[40px]"
				title={$t('ai.preview.clickToEdit')}
			>
				{localData.description || $t('ai.preview.notSet')}
			</button>
		{/if}
	</div>

	<!-- Intensity Selection -->
	<div class="mb-3">
		<label class="block text-xs text-cocoa-500 mb-1">{$t('ai.preview.effortLevel')}</label>
		<select
			value={localData.intensity}
			onchange={handleIntensityChange}
			class="w-full px-2 py-1.5 border border-amber-300 rounded-2xl text-sm bg-warm-paper focus:ring-2 focus:ring-amber-400 focus:outline-none"
		>
			<option value="Easy">{intensityEmoji.Easy} {$t('identityProof.intensity.easy')}</option>
			<option value="Moderate">{intensityEmoji.Moderate} {$t('identityProof.intensity.moderate')}</option>
			<option value="Hard">{intensityEmoji.Hard} {$t('identityProof.intensity.hard')}</option>
		</select>
	</div>

	<!-- Reasoning (read-only) -->
	{#if localData.reasoning}
		<div class="mt-3 pt-3 border-t border-amber-200">
			<p class="text-xs text-amber-600 italic">
				💡 {localData.reasoning}
			</p>
		</div>
	{/if}

	<!-- Intensity Badge -->
	<div class="mt-2 flex items-center gap-2">
		<span class="text-xs px-2 py-0.5 rounded-full {intensityColors[localData.intensity]}">
			{intensityEmoji[localData.intensity]} {$t(`identityProof.intensity.${localData.intensity.toLowerCase()}`)}
		</span>
	</div>
</div>
