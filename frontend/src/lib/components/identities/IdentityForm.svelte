<script lang="ts">
	import type { Identity, CreateIdentityRequest, UpdateIdentityRequest } from '$lib/types';

	interface Props {
		identity?: Identity;
		onsubmit: (data: CreateIdentityRequest | UpdateIdentityRequest) => Promise<void>;
		oncancel: () => void;
	}

	let { identity, onsubmit, oncancel }: Props = $props();

	let name = $state(identity?.name ?? '');
	let description = $state(identity?.description ?? '');
	let color = $state(identity?.color ?? '#6366f1');
	let icon = $state(identity?.icon ?? '');
	let loading = $state(false);
	let error = $state('');

	const commonIcons = ['💪', '🧠', '📚', '🏃', '🧘', '💼', '🎯', '⭐', '🌱', '❤️'];

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

	<div>
		<label class="label">Icon (optional)</label>
		<div class="flex flex-wrap gap-2">
			{#each commonIcons as emoji}
				<button
					type="button"
					class="w-10 h-10 text-xl rounded-lg border-2 transition-colors {icon === emoji
						? 'border-primary-500 bg-primary-50'
						: 'border-gray-200 hover:border-gray-300'}"
					onclick={() => (icon = icon === emoji ? '' : emoji)}
				>
					{emoji}
				</button>
			{/each}
		</div>
	</div>

	<div>
		<label for="color" class="label">Color</label>
		<div class="flex items-center gap-3">
			<input id="color" type="color" bind:value={color} class="w-10 h-10 rounded cursor-pointer" />
			<span class="text-sm text-gray-500">{color}</span>
		</div>
	</div>

	<div class="flex gap-3 pt-4">
		<button type="submit" disabled={loading || !name.trim()} class="btn-primary flex-1">
			{loading ? 'Saving...' : identity ? 'Update Identity' : 'Create Identity'}
		</button>
		<button type="button" onclick={oncancel} class="btn-secondary">Cancel</button>
	</div>
</form>
