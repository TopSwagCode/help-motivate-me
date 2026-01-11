<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getGoals, createGoal } from '$lib/api/goals';
	import type { Goal, CreateGoalRequest } from '$lib/types';
	import GoalForm from '$lib/components/goals/GoalForm.svelte';

	let goals = $state<Goal[]>([]);
	let loading = $state(true);
	let error = $state('');

	// Modal state
	let showModal = $state(false);

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

	function openModal() {
		showModal = true;
	}

	function closeModal() {
		showModal = false;
	}

	async function handleCreateGoal(data: CreateGoalRequest) {
		const goal = await createGoal(data);
		goals = [...goals, goal];
		closeModal();
		goto(`/goals/${goal.id}`);
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="flex justify-between items-center mb-8">
			<h1 class="text-2xl font-bold text-gray-900">{$t('goals.pageTitle')}</h1>
			<button onclick={openModal} class="btn-primary">
				<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				{$t('goals.newGoal')}
			</button>
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
				<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('goals.emptyTitle')}</h3>
				<p class="text-gray-500 mb-6">{$t('goals.emptyDescription')}</p>
				<button onclick={openModal} class="btn-primary">{$t('goals.createFirst')}</button>
			</div>
		{:else}
			<!-- Active Goals -->
			{#if activeGoals.length > 0}
				<section class="mb-8">
					<h2 class="text-lg font-semibold text-gray-900 mb-4">{$t('goals.activeGoals')} ({activeGoals.length})</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each activeGoals as goal (goal.id)}
							<a href="/goals/{goal.id}" class="card-hover p-6 block">
								<h3 class="font-medium text-gray-900 line-clamp-2 mb-3">{goal.title}</h3>

								{#if goal.description}
									<p class="text-sm text-gray-500 line-clamp-2 mb-4">{goal.description}</p>
								{/if}

								<div class="flex items-center justify-between text-sm">
									<span class="text-gray-500">
										{goal.completedTaskCount}/{goal.taskCount} {$t('goals.tasks')}
									</span>
									{#if goal.targetDate}
										<span class="text-gray-400">
											{new Date(goal.targetDate + 'T12:00:00').toLocaleDateString(get(locale) === 'da' ? 'da-DK' : 'en-US')}
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
					<h2 class="text-lg font-semibold text-gray-900 mb-4">{$t('goals.completedGoals')} ({completedGoals.length})</h2>
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

	<!-- Create Goal Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">{$t('goals.newGoal')}</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-gray-600" aria-label="Close">
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</button>
					</div>

					<GoalForm onsubmit={handleCreateGoal} oncancel={closeModal} />
				</div>
			</div>
		</div>
	{/if}
</div>
