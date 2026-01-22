<script lang="ts">
	import { t } from 'svelte-i18n';
	import { createGoal } from '$lib/api/goals';
	import GoalForm from '$lib/components/goals/GoalForm.svelte';
	import type { CreateGoalRequest } from '$lib/types';

	interface Props {
		oncomplete: () => void;
		onskip: () => void;
		onback: () => void;
	}

	let { oncomplete, onskip, onback }: Props = $props();

	let showForm = $state(false);
	let createdGoal = $state<string | null>(null);

	async function handleCreateGoal(data: CreateGoalRequest) {
		const goal = await createGoal(data);
		createdGoal = goal.title;
		showForm = false;
	}

	function handleCancel() {
		showForm = false;
	}
</script>

<div>
	<!-- Intro section -->
	<div class="mb-8">
		<div class="flex items-center gap-3 mb-4">
			<div class="w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
				<span class="text-2xl">🎯</span>
			</div>
			<h2 class="text-xl font-semibold text-gray-900">{$t('onboarding.manual.goals.title')}</h2>
		</div>

		<div class="prose prose-sm text-gray-600">
			<p class="mb-3">
				{@html $t('onboarding.manual.goals.intro')}
			</p>
			<p class="mb-3">{$t('onboarding.manual.goals.greatGoals')}</p>
			<ul class="list-disc list-inside space-y-1 text-sm">
				<li>{@html $t('onboarding.manual.goals.specific')}</li>
				<li>{@html $t('onboarding.manual.goals.meaningful')}</li>
				<li>{@html $t('onboarding.manual.goals.actionable')}</li>
			</ul>
			<p class="mt-3 text-sm">
				{$t('onboarding.manual.goals.addTasks')}
			</p>
		</div>
	</div>

	<!-- Created goal feedback -->
	{#if createdGoal}
		<div class="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
			<div class="flex items-center gap-2">
				<svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 24 24">
					<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
				</svg>
				<span class="text-green-700">
					{@html $t('onboarding.manual.goals.created', { values: { name: `<strong>${createdGoal}</strong>` } })}
				</span>
			</div>
		</div>
	{/if}

	<!-- Form or action buttons -->
	{#if showForm}
		<GoalForm onsubmit={handleCreateGoal} oncancel={handleCancel} />
	{:else}
		<div class="space-y-4">
			{#if !createdGoal}
				<button onclick={() => (showForm = true)} class="btn-primary w-full">
					<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					{$t('onboarding.manual.goals.createFirst')}
				</button>
			{:else}
				<button onclick={() => (showForm = true)} class="btn-secondary w-full">
					<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					{$t('onboarding.manual.goals.createAnother')}
				</button>
			{/if}

			<div class="flex gap-3">
				<button onclick={onback} class="btn-secondary">
					<svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M15 19l-7-7 7-7"
						/>
					</svg>
					{$t('onboarding.manual.goals.back')}
				</button>
				<button onclick={oncomplete} class="btn-primary flex-1">
					{createdGoal ? $t('onboarding.manual.goals.completeSetup') : $t('onboarding.manual.goals.skipFinish')}
					<svg class="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M5 13l4 4L19 7"
						/>
					</svg>
				</button>
			</div>
		</div>
	{/if}
</div>
