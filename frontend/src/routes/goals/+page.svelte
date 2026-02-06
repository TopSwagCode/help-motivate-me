<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { commandBar } from '$lib/stores/commandBar';
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getGoals, createGoal } from '$lib/api/goals';
	import type { Goal, CreateGoalRequest } from '$lib/types';
	import GoalForm from '$lib/components/goals/GoalForm.svelte';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

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

		await loadGoals();
	});

	async function loadGoals() {
		loading = true;
		error = '';
		try {
			goals = await getGoals();
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('errors.generic');
		} finally {
			loading = false;
		}
	}

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

<div class="bg-warm-cream">
	<main class="max-w-4xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
		<div class="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3 mb-6 sm:mb-8">
			<InfoOverlay 
				title={$t('goals.pageTitle')} 
				description={$t('goals.info.description')} 
			/>
			<button onclick={openModal} class="btn-primary w-full sm:w-auto justify-center">
				<svg class="w-5 h-5 sm:mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				<span class="ml-2 sm:ml-0">{$t('goals.newGoal')}</span>
			</button>
		</div>

		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="card">
				<ErrorState message={error} onRetry={loadGoals} size="md" />
			</div>
		{:else if goals.length === 0}
			<div class="card p-8 sm:p-12">
				<div class="max-w-md mx-auto text-center">
					<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
						<svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
						</svg>
					</div>
					<h3 class="text-lg font-medium text-cocoa-800 mb-2">{$t('goals.emptyTitle')}</h3>
					<p class="text-cocoa-500 mb-4">{$t('goals.emptyDescription')}</p>
					<p class="text-cocoa-500 text-sm mb-6 flex items-center justify-center gap-1 flex-wrap">
						{$t('goals.emptyHowTo')}
						<button
							type="button"
							onclick={() => commandBar.open()}
							class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
							title="Open AI Assistant"
						>
							<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
							</svg>
						</button>
					</p>
					<button onclick={openModal} class="btn-primary">{$t('goals.createFirst')}</button>
				</div>
			</div>
		{:else}
			<div data-tour="goals-list">
			<!-- Active Goals -->
			{#if activeGoals.length > 0}
				<section class="mb-8">
					<h2 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('goals.activeGoals')} ({activeGoals.length})</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each activeGoals as goal (goal.id)}
							<a
								href="/goals/{goal.id}"
								class="block rounded-2xl overflow-hidden transition-all hover:shadow-md"
								style="background-color: {goal.identityColor || '#d4944c'}08; border: 1px solid {goal.identityColor || '#d4944c'}20"
							>
								<!-- Header with identity badge -->
								<div
									class="px-4 py-2 flex items-center justify-between"
									style="background-color: {goal.identityColor || '#d4944c'}15"
								>
									{#if goal.identityIcon || goal.identityName}
										<span
											class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium"
											style="background-color: {goal.identityColor || '#d4944c'}20; color: {goal.identityColor || '#d4944c'}"
										>
											{#if goal.identityIcon}
												<span>{goal.identityIcon}</span>
											{/if}
											{goal.identityName || ''}
										</span>
									{:else}
										<span></span>
									{/if}
									{#if goal.targetDate}
										<span class="text-xs text-cocoa-500">
											{new Date(goal.targetDate + 'T12:00:00').toLocaleDateString(get(locale) === 'da' ? 'da-DK' : 'en-US')}
										</span>
									{/if}
								</div>

								<div class="p-4">
									<h3 class="font-medium text-cocoa-800 line-clamp-2 mb-2">{goal.title}</h3>

									{#if goal.description}
										<p class="text-sm text-cocoa-500 line-clamp-2 mb-3">{goal.description}</p>
									{/if}

									<div class="flex items-center justify-between text-sm">
										<span class="text-cocoa-500">
											{goal.completedTaskCount}/{goal.taskCount} {$t('goals.tasks')}
										</span>
									</div>

									{#if goal.taskCount > 0}
										<div class="mt-3 bg-gray-200 rounded-full h-1.5">
											<div
												class="h-1.5 rounded-full transition-all duration-300"
												style="width: {(goal.completedTaskCount / goal.taskCount) * 100}%; background-color: {goal.identityColor || '#d4944c'}"
											></div>
										</div>
									{/if}
								</div>
							</a>
						{/each}
					</div>
				</section>
			{/if}

			<!-- Completed Goals -->
			{#if completedGoals.length > 0}
				<section>
					<h2 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('goals.completedGoals')} ({completedGoals.length})</h2>
					<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each completedGoals as goal (goal.id)}
							<a
								href="/goals/{goal.id}"
								class="block rounded-2xl overflow-hidden transition-all hover:shadow-md opacity-70"
								style="background-color: {goal.identityColor || '#d4944c'}06; border: 1px solid {goal.identityColor || '#d4944c'}15"
							>
								<div class="p-4 flex items-center gap-3">
									<svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
										<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
									</svg>
									<h3 class="font-medium text-cocoa-800 line-clamp-1">{goal.title}</h3>
									{#if goal.identityIcon}
										<span class="flex-shrink-0 text-lg ml-auto" style="color: {goal.identityColor || '#d4944c'}">
											{goal.identityIcon}
										</span>
									{/if}
								</div>
							</a>
						{/each}
					</div>
				</section>
			{/if}
			</div>
		{/if}
	</main>

	<!-- Create Goal Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-3 sm:p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-warm-paper rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
				<div class="p-4 sm:p-6">
					<div class="flex items-center justify-between mb-4 sm:mb-6">
						<h2 class="text-lg sm:text-xl font-semibold text-cocoa-800">{$t('goals.newGoal')}</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-cocoa-600 p-1" aria-label="Close">
							<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
