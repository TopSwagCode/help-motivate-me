<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { createGoal } from '$lib/api/goals';

	let title = $state('');
	let description = $state('');
	let targetDate = $state('');
	let loading = $state(false);
	let error = $state('');

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		error = '';
		loading = true;

		try {
			const goal = await createGoal({
				title,
				description: description || undefined,
				targetDate: targetDate || undefined
			});
			goto(`/goals/${goal.id}`);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to create goal';
			loading = false;
		}
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<!-- Page Header -->
		<h1 class="text-2xl font-bold text-gray-900 mb-6">New Goal</h1>
		<form onsubmit={handleSubmit} class="card p-6 space-y-6">
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
				></textarea>
			</div>

			<div>
				<label for="targetDate" class="label">Target Date</label>
				<input
					id="targetDate"
					type="date"
					bind:value={targetDate}
					class="input"
				/>
			</div>

			<div class="flex gap-3 pt-4">
				<button type="submit" disabled={loading || !title.trim()} class="btn-primary flex-1">
					{loading ? 'Creating...' : 'Create Goal'}
				</button>
				<a href="/dashboard" class="btn-secondary">Cancel</a>
			</div>
		</form>
	</main>
</div>
