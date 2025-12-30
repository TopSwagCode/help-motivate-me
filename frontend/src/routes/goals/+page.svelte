<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getGoals } from '$lib/api/goals';
	import type { Goal } from '$lib/types';

	let goals = $state<Goal[]>([]);
	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		try {
			goals = await getGoals();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load goals';
		} finally {
			loading = false;
		}
	});

	const completedGoals = $derived(goals.filter((g) => g.isCompleted));
	const activeGoals = $derived(goals.filter((g) => !g.isCompleted));
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="flex justify-between items-center mb-8">
			<h1 class="text-2xl font-bold text-gray-900">My Goals</h1>
			<a href="/goals/new" class="btn-primary">
				<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				New Goal
			</a>
		</div>

		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
				{error}
			</div>
		{:else if goals.length === 0}
			<div class="card p-12 text-center">
				<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
					<svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
				</div>
				<h3 class="text-lg font-medium text-gray-900 mb-2">No goals yet</h3>
				<p class="text-gray-500 mb-6">Get started by creating your first goal.</p>
				<a href="/goals/new" class="btn-primary">Create your first goal</a>
			</div>
		{:else}
			<!-- Active Goals -->
			{#if activeGoals.length > 0}
				<section class="mb-8">
					<h2 class="text-lg font-semibold text-gray-900 mb-4">Active Goals ({activeGoals.length})</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each activeGoals as goal (goal.id)}
							<a href="/goals/{goal.id}" class="card-hover p-6 block">
								<h3 class="font-medium text-gray-900 line-clamp-2 mb-3">{goal.title}</h3>

								{#if goal.description}
									<p class="text-sm text-gray-500 line-clamp-2 mb-4">{goal.description}</p>
								{/if}

								<div class="flex items-center justify-between text-sm">
									<span class="text-gray-500">
										{goal.completedTaskCount}/{goal.taskCount} tasks
									</span>
									{#if goal.targetDate}
										<span class="text-gray-400">
											{new Date(goal.targetDate + 'T12:00:00').toLocaleDateString()}
										</span>
									{/if}
								</div>

								{#if goal.taskCount > 0}
									<div class="mt-3 bg-gray-200 rounded-full h-1.5">
										<div
											class="bg-primary-600 h-1.5 rounded-full transition-all duration-300"
											style="width: {(goal.completedTaskCount / goal.taskCount) * 100}%"
										></div>
									</div>
								{/if}
							</a>
						{/each}
					</div>
				</section>
			{/if}

			<!-- Completed Goals -->
			{#if completedGoals.length > 0}
				<section>
					<h2 class="text-lg font-semibold text-gray-900 mb-4">Completed ({completedGoals.length})</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each completedGoals as goal (goal.id)}
							<a href="/goals/{goal.id}" class="card-hover p-6 block opacity-60">
								<div class="flex items-center gap-3">
									<svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
										<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
									</svg>
									<h3 class="font-medium text-gray-900 line-clamp-1">{goal.title}</h3>
								</div>
							</a>
						{/each}
					</div>
				</section>
			{/if}
		{/if}
	</main>
</div>
