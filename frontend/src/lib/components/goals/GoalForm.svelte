<script lang="ts">
	import type { Goal, CreateGoalRequest } from '$lib/types';

	interface Props {
		goal?: Goal;
		onsubmit: (data: CreateGoalRequest) => Promise<void>;
		oncancel: () => void;
	}

	let { goal, onsubmit, oncancel }: Props = $props();

	let title = $state(goal?.title ?? '');
	let description = $state(goal?.description ?? '');
	let targetDate = $state(goal?.targetDate ?? '');
	let loading = $state(false);
	let error = $state('');

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!title.trim()) return;

		loading = true;
		error = '';

		try {
			await onsubmit({
				title: title.trim(),
				description: description.trim() || undefined,
				targetDate: targetDate || undefined
			});
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save goal';
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
		<label for="title" class="label">Goal Title *</label>
		<input
			id="title"
			type="text"
			bind:value={title}
			required
			maxlength="255"
			placeholder="What do you want to achieve?"
			class="input"
			disabled={loading}
		/>
	</div>

	<div>
		<label for="description" class="label">Description</label>
		<textarea
			id="description"
			bind:value={description}
			rows="3"
			placeholder="Add more details about your goal..."
			class="input"
			disabled={loading}
		></textarea>
	</div>

	<div>
		<label for="targetDate" class="label">Target Date</label>
		<input
			id="targetDate"
			type="date"
			bind:value={targetDate}
			class="input"
			disabled={loading}
		/>
	</div>

	<div class="flex gap-3 pt-4">
		<button type="submit" disabled={loading || !title.trim()} class="btn-primary flex-1">
			{loading ? 'Saving...' : goal ? 'Update Goal' : 'Create Goal'}
		</button>
		<button type="button" onclick={oncancel} class="btn-secondary" disabled={loading}>
			Cancel
		</button>
	</div>
</form>
