<script lang="ts">
	import { tick, onMount } from 'svelte';
	import { t } from 'svelte-i18n';
	import type { CreateHabitStackRequest, HabitStackItemRequest, Identity } from '$lib/types';
	import { getIdentities } from '$lib/api/identities';

	interface Props {
		onsubmit: (data: CreateHabitStackRequest) => Promise<void>;
		oncancel: () => void;
	}

	let { onsubmit, oncancel }: Props = $props();

	let name = $state('');
	let identityId = $state('');
	let identities = $state<Identity[]>([]);
	let items = $state<HabitStackItemRequest[]>([
		{ cueDescription: '', habitDescription: '' }
	]);
	let loading = $state(false);
	let error = $state('');
	let formContainerRef = $state<HTMLElement | null>(null);

	onMount(async () => {
		try {
			identities = await getIdentities();
		} catch {
			// Failed to load identities - form will work without identity selection
		}
	});

	async function addItem() {
		const lastItem = items[items.length - 1];
		items = [
			...items,
			{
				cueDescription: lastItem?.habitDescription || '',
				habitDescription: ''
			}
		];
		
		// Wait for DOM update, then scroll and focus
		await tick();
		
		// Scroll to absolute bottom
		if (formContainerRef) {
			setTimeout(() => {
				if (formContainerRef) {
					formContainerRef.scrollTo({
						top: formContainerRef.scrollHeight + 1000, // Extra padding to ensure bottom
						behavior: 'smooth'
					});
				}
			}, 50);
		}
		
		// Focus the new "I will..." input
		const newIndex = items.length - 1;
		const habitInput = document.querySelector(
			`#habit-input-${newIndex}`
		) as HTMLInputElement;
		if (habitInput) {
			habitInput.focus();
		}
	}

	function handleItemKeyPress(e: KeyboardEvent, index: number) {
		// If Enter is pressed in the last habit's "I will..." field, add another habit
		if (e.key === 'Enter' && index === items.length - 1) {
			e.preventDefault();
			addItem();
		}
	}

	function removeItem(index: number) {
		if (items.length > 1) {
			items = items.filter((_, i) => i !== index);
		}
	}

	function updateItem(index: number, field: 'cueDescription' | 'habitDescription', value: string) {
		items = items.map((item, i) => (i === index ? { ...item, [field]: value } : item));
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!name.trim()) {
			error = $t('habitStacks.form.errors.nameRequired');
			return;
		}

		const validItems = items.filter((item) => item.cueDescription.trim() && item.habitDescription.trim());
		if (validItems.length === 0) {
			error = $t('habitStacks.form.errors.habitRequired');
			return;
		}

		loading = true;
		error = '';

		try {
			await onsubmit({
				name: name.trim(),
				items: validItems,
				identityId: identityId || undefined
			});
		} catch (e) {
			error = e instanceof Error ? e.message : $t('habitStacks.form.errors.createFailed');
		} finally {
			loading = false;
		}
	}
</script>

<form bind:this={formContainerRef} onsubmit={handleSubmit} class="space-y-6">
	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm">
			{error}
		</div>
	{/if}

	<div>
		<label for="name" class="block text-sm font-medium text-cocoa-700 mb-1">{$t('habitStacks.form.name')}</label>
		<input
			type="text"
			id="name"
			bind:value={name}
			placeholder={$t('habitStacks.form.namePlaceholder')}
			class="input"
			required
		/>
	</div>

	{#if identities.length > 0}
		<div>
			<label for="identityId" class="block text-sm font-medium text-cocoa-700 mb-1">
				{$t('habitStacks.form.identity')}
			</label>
			<select
				id="identityId"
				bind:value={identityId}
				class="input"
				disabled={loading}
			>
				<option value="">{$t('habitStacks.form.noIdentity')}</option>
				{#each identities as identity (identity.id)}
					<option value={identity.id}>
						{identity.icon ? `${identity.icon} ` : ''}{identity.name}
					</option>
				{/each}
			</select>
			<p class="text-xs text-cocoa-500 mt-1">{$t('habitStacks.form.identityHint')}</p>
		</div>
	{/if}

	<div>
		<span class="block text-sm font-medium text-cocoa-700 mb-3">{$t('habitStacks.createPopup.habitChain')}</span>
		<div class="space-y-4">
			{#each items as item, i (i)}
				<div class="relative pl-6 pb-4 {i < items.length - 1 ? 'border-l-2 border-primary-100 ml-3' : 'ml-3'}">
					<div class="absolute left-0 top-0 w-6 h-6 -ml-3 rounded-full bg-primary-100 border-2 border-primary-500 flex items-center justify-center">
						<span class="text-xs font-medium text-primary-700">{i + 1}</span>
					</div>
					<div class="bg-warm-cream rounded-2xl p-4">
						<div class="space-y-3">
							<div>
								<label for="cue-input-{i}" class="block text-xs font-medium text-cocoa-500 mb-1">{$t('habitStacks.createPopup.afterI')}</label>
								<input
									id="cue-input-{i}"
									type="text"
									value={item.cueDescription}
									oninput={(e) => updateItem(i, 'cueDescription', (e.target as HTMLInputElement).value)}
									placeholder={i === 0 ? $t('habitStacks.createPopup.placeholderCue') : $t('habitStacks.createPopup.placeholderPreviousHabit')}
									class="input text-sm"
								/>
							</div>
							<div>
								<label for="habit-input-{i}" class="block text-xs font-medium text-cocoa-500 mb-1">{$t('habitStacks.createPopup.iWillDo')}</label>
								<input
									id="habit-input-{i}"
									type="text"
									value={item.habitDescription}
									oninput={(e) => updateItem(i, 'habitDescription', (e.target as HTMLInputElement).value)}
									onkeydown={(e) => handleItemKeyPress(e, i)}
									placeholder={$t('habitStacks.createPopup.placeholderHabit')}
									class="input text-sm"
								/>
							</div>
						</div>
						{#if items.length > 1}
							<button
								type="button"
								onclick={() => removeItem(i)}
								class="mt-2 text-xs text-red-500 hover:text-red-700"
							>
								{$t('habitStacks.createPopup.remove')}
							</button>
						{/if}
					</div>
				</div>
			{/each}
		</div>

		<button
			type="button"
			onclick={addItem}
			class="mt-4 ml-3 flex items-center text-sm text-primary-600 hover:text-primary-700"
		>
			<svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
			</svg>
			{$t('habitStacks.createPopup.addAnother')}
		</button>
	</div>

	<div class="flex justify-end gap-3 pt-4 border-t border-primary-100">
		<button type="button" onclick={oncancel} class="btn-secondary" disabled={loading}>
			{$t('common.cancel')}
		</button>
		<button type="submit" class="btn-primary" disabled={loading}>
			{loading ? $t('habitStacks.createPopup.creating') : $t('habitStacks.createPopup.createStack')}
		</button>
	</div>
</form>
