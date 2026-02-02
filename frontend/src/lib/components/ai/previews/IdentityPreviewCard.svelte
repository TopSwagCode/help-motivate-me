<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { IdentityPreviewData } from '$lib/api/aiGeneral';
	import EmojiPicker from '$lib/components/shared/EmojiPicker.svelte';
	import ColorPicker from '$lib/components/shared/ColorPicker.svelte';

	interface Props {
		data: IdentityPreviewData;
		onchange?: (data: IdentityPreviewData) => void;
	}

	let { data, onchange }: Props = $props();

	// Editing states
	let editingName = $state(false);
	let editingDescription = $state(false);
	let showEmojiPicker = $state(false);
	let showColorPicker = $state(false);

	// Local copy of data for editing
	let localData = $state({ ...data });

	// Sync when parent data changes
	$effect(() => {
		localData = { ...data };
	});

	function updateField<K extends keyof IdentityPreviewData>(field: K, value: IdentityPreviewData[K]) {
		localData = { ...localData, [field]: value };
		onchange?.(localData);
	}

	function handleEmojiChange(emoji: string) {
		updateField('icon', emoji || null);
	}

	function handleColorChange(color: string) {
		updateField('color', color || null);
	}
</script>

<div class="bg-primary-50 border border-primary-200 rounded-xl p-4">
	<div class="flex items-start justify-between mb-2">
		<div class="flex items-center gap-2 text-primary-600 text-sm font-medium">
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
				/>
			</svg>
			{$t('ai.preview.identity')}
		</div>
	</div>

	<!-- Editable Name -->
	{#if editingName}
		<input
			type="text"
			bind:value={localData.name}
			onblur={() => { editingName = false; onchange?.(localData); }}
			onkeydown={(e) => { if (e.key === 'Enter') { editingName = false; onchange?.(localData); } if (e.key === 'Escape') editingName = false; }}
			class="w-full text-lg font-semibold text-cocoa-800 bg-warm-paper border border-primary-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-primary-400"
			autofocus
		/>
	{:else}
		<button
			type="button"
			onclick={() => editingName = true}
			class="text-lg font-semibold text-cocoa-800 hover:bg-primary-100 rounded px-1 -mx-1 transition-colors text-left w-full"
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
			class="w-full text-cocoa-600 mt-1 text-sm bg-warm-paper border border-primary-300 rounded px-2 py-1 outline-none focus:ring-2 focus:ring-primary-400 resize-none"
			rows="2"
			autofocus
		></textarea>
	{:else}
		<button
			type="button"
			onclick={() => editingDescription = true}
			class="text-cocoa-600 mt-1 text-sm hover:bg-primary-100 rounded px-1 -mx-1 transition-colors text-left w-full {!localData.description ? 'italic text-gray-400' : ''}"
			title={$t('ai.preview.clickToEdit')}
		>
			{localData.description || $t('ai.preview.addDescription')}
		</button>
	{/if}

	<!-- Icon and Color row -->
	<div class="mt-4 flex flex-wrap gap-4">
		<!-- Editable Icon (Emoji) -->
		<div class="flex-1 min-w-0">
			<div class="text-xs font-medium text-cocoa-500 mb-2">{$t('identities.form.emoji')}</div>
			{#if showEmojiPicker}
				<div class="relative">
					<EmojiPicker value={localData.icon || ''} onchange={handleEmojiChange} />
					<button
						type="button"
						onclick={() => showEmojiPicker = false}
						class="mt-2 text-xs text-primary-600 hover:text-primary-800"
					>
						{$t('common.done')}
					</button>
				</div>
			{:else}
				<button
					type="button"
					onclick={() => showEmojiPicker = true}
					class="flex items-center gap-2 px-3 py-2 bg-warm-paper border border-primary-200 rounded-2xl hover:bg-primary-50 transition-colors"
					title={$t('ai.preview.clickToEdit')}
				>
					{#if localData.icon}
						<span class="text-2xl">{localData.icon}</span>
					{:else}
						<span class="text-gray-400 text-sm">{$t('ai.preview.notSet')}</span>
					{/if}
					<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
					</svg>
				</button>
			{/if}
		</div>

		<!-- Editable Color -->
		<div class="flex-1 min-w-0">
			<div class="text-xs font-medium text-cocoa-500 mb-2">{$t('identities.form.color')}</div>
			{#if showColorPicker}
				<div class="relative">
					<ColorPicker value={localData.color || '#d4944c'} onchange={handleColorChange} />
					<button
						type="button"
						onclick={() => showColorPicker = false}
						class="mt-2 text-xs text-primary-600 hover:text-primary-800"
					>
						{$t('common.done')}
					</button>
				</div>
			{:else}
				<button
					type="button"
					onclick={() => showColorPicker = true}
					class="flex items-center gap-2 px-3 py-2 bg-warm-paper border border-primary-200 rounded-2xl hover:bg-primary-50 transition-colors"
					title={$t('ai.preview.clickToEdit')}
				>
					<span
						class="w-6 h-6 rounded-full border border-primary-100"
						style="background-color: {localData.color || '#d4944c'}"
					></span>
					<span class="text-sm text-cocoa-600">{localData.color || '#d4944c'}</span>
					<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
					</svg>
				</button>
			{/if}
		</div>
	</div>

	{#if localData.reasoning}
		<div class="mt-4 p-3 bg-primary-100 rounded-2xl">
			<div class="flex items-start gap-2">
				<svg class="w-4 h-4 text-primary-600 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
				</svg>
				<p class="text-sm text-primary-700">{localData.reasoning}</p>
			</div>
		</div>
	{/if}
</div>
