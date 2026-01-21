<script lang="ts">
	import type { Identity, CreateIdentityRequest, UpdateIdentityRequest } from '$lib/types';
	import EmojiPicker from '$lib/components/shared/EmojiPicker.svelte';
	import ColorPicker from '$lib/components/shared/ColorPicker.svelte';

	interface Props {
		identity?: Identity;
		onsubmit: (data: CreateIdentityRequest | UpdateIdentityRequest) => Promise<void>;
		oncancel: () => void;
	}

	let { identity, onsubmit, oncancel }: Props = $props();

	let name = $state('');
	let description = $state('');
	let color = $state('#6366f1');
	let icon = $state('');
	let loading = $state(false);
	let error = $state('');

	// Initialize form fields from identity prop
	$effect(() => {
		name = identity?.name ?? '';
		description = identity?.description ?? '';
		color = identity?.color ?? '#6366f1';
		icon = identity?.icon ?? '';
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!name.trim()) return;

		loading = true;
		error = '';

		try {
			await onsubmit({
				name: name.trim(),
				description: description.trim() || undefined,
				color: color || undefined,
				icon: icon || undefined
			});
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save identity';
		} finally {
			loading = false;
		}
	}
</script>

<form onsubmit={handleSubmit} class="space-y-4">
	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
			{error}
		</div>
	{/if}

	<div>
		<label for="name" class="label">Identity Name *</label>
		<input
			id="name"
			type="text"
			bind:value={name}
			required
			maxlength="100"
			placeholder="e.g., Healthy Person"
			class="input"
		/>
		<p class="text-xs text-gray-500 mt-1">Who do you want to become?</p>
	</div>

	<div>
		<label for="description" class="label">Description</label>
		<textarea
			id="description"
			bind:value={description}
			rows="2"
			placeholder="What does this identity mean to you?"
			class="input"
		></textarea>
	</div>

	<EmojiPicker value={icon} onchange={(emoji) => (icon = emoji)} />

	<ColorPicker value={color} onchange={(newColor) => (color = newColor)} />

	<div class="flex gap-3 pt-4">
		<button type="submit" disabled={loading || !name.trim()} class="btn-primary flex-1">
			{loading ? 'Saving...' : identity ? 'Update Identity' : 'Create Identity'}
		</button>
		<button type="button" onclick={oncancel} class="btn-secondary">Cancel</button>
	</div>
</form>
