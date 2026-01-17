<script lang="ts">
	import { tick, onMount } from 'svelte';
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
		} catch (e) {
			console.error('Failed to load identities', e);
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
			error = 'Stack name is required';
			return;
		}

		const validItems = items.filter((item) => item.cueDescription.trim() && item.habitDescription.trim());
		if (validItems.length === 0) {
			error = 'At least one complete habit item is required';
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
			error = e instanceof Error ? e.message : 'Failed to create habit stack';
		} finally {
			loading = false;
		}
	}
</script>

<form bind:this={formContainerRef} onsubmit={handleSubmit} class="space-y-6">
	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
			{error}
		</div>
	{/if}

	<div>
		<label for="name" class="block text-sm font-medium text-gray-700 mb-1">Stack Name</label>
		<input
			type="text"
			id="name"
			bind:value={name}
			placeholder="Morning Routine"
			class="input"
			required
		/>
	</div>

	{#if identities.length > 0}
		<div>
			<label for="identityId" class="block text-sm font-medium text-gray-700 mb-1">
				Identity <span class="text-gray-500 text-sm">(Optional)</span>
			</label>
			<select
				id="identityId"
				bind:value={identityId}
				class="input"
				disabled={loading}
			>
				<option value="">No identity</option>
				{#each identities as identity (identity.id)}
					<option value={identity.id}>
						{identity.icon ? `${identity.icon} ` : ''}{identity.name}
					</option>
				{/each}
			</select>
			<p class="text-xs text-gray-500 mt-1">Link this habit stack to an identity</p>
		</div>
	{/if}

	<div>
		<label class="block text-sm font-medium text-gray-700 mb-3">Habit Chain</label>
		<div class="space-y-4">
			{#each items as item, i (i)}
				<div class="relative pl-6 pb-4 {i < items.length - 1 ? 'border-l-2 border-gray-200 ml-3' : 'ml-3'}">
					<div class="absolute left-0 top-0 w-6 h-6 -ml-3 rounded-full bg-primary-100 border-2 border-primary-500 flex items-center justify-center">
						<span class="text-xs font-medium text-primary-700">{i + 1}</span>
					</div>
					<div class="bg-gray-50 rounded-lg p-4">
						<div class="space-y-3">
							<div>
								<label class="block text-xs font-medium text-gray-500 mb-1">After I...</label>
								<input
									type="text"
									value={item.cueDescription}
									oninput={(e) => updateItem(i, 'cueDescription', (e.target as HTMLInputElement).value)}
									placeholder={i === 0 ? 'wake up' : 'previous habit'}
									class="input text-sm"
								/>
							</div>
							<div>
								<label class="block text-xs font-medium text-gray-500 mb-1">I will...</label>
								<input
									id="habit-input-{i}"
									type="text"
									value={item.habitDescription}
									oninput={(e) => updateItem(i, 'habitDescription', (e.target as HTMLInputElement).value)}
									onkeydown={(e) => handleItemKeyPress(e, i)}
									placeholder="drink a glass of water"
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
								Remove
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
			Add another habit
		</button>
	</div>

	<div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
		<button type="button" onclick={oncancel} class="btn-secondary" disabled={loading}>
			Cancel
		</button>
		<button type="submit" class="btn-primary" disabled={loading}>
			{loading ? 'Creating...' : 'Create Stack'}
		</button>
	</div>
</form>
