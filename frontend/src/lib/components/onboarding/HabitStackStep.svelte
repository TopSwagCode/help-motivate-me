<script lang="ts">
	import { createHabitStack } from '$lib/api/habitStacks';
	import StackForm from '$lib/components/habit-stacks/StackForm.svelte';
	import type { CreateHabitStackRequest } from '$lib/types';

	interface Props {
		onnext: () => void;
		onskip: () => void;
		onback: () => void;
	}

	let { onnext, onskip, onback }: Props = $props();

	let showForm = $state(false);
	let createdStack = $state<string | null>(null);

	async function handleCreateStack(data: CreateHabitStackRequest) {
		const stack = await createHabitStack(data);
		createdStack = stack.name;
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
				<span class="text-2xl">🔗</span>
			</div>
			<h2 class="text-xl font-semibold text-gray-900">Build Habit Stacks</h2>
		</div>

		<div class="prose prose-sm text-gray-600">
			<p class="mb-3">
				<strong>Habit stacking</strong> is one of the most effective strategies for building new
				habits. The idea is simple: link a new habit to an existing one.
			</p>
			<p class="mb-3">The formula is:</p>
			<div class="bg-gray-50 rounded-lg p-3 my-3 text-center font-medium">
				"After I [CURRENT HABIT], I will [NEW HABIT]."
			</div>
			<p class="mb-3 text-sm">For example:</p>
			<ul class="list-disc list-inside space-y-1 text-sm">
				<li>After I pour my morning coffee, I will meditate for 5 minutes.</li>
				<li>After I finish lunch, I will write in my journal.</li>
				<li>After I sit down at my desk, I will review my goals.</li>
			</ul>
			<p class="mt-3 text-sm">
				You can chain multiple habits together to create a powerful routine!
			</p>
		</div>
	</div>

	<!-- Created stack feedback -->
	{#if createdStack}
		<div class="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
			<div class="flex items-center gap-2">
				<svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 24 24">
					<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
				</svg>
				<span class="text-green-700">
					Habit stack "<strong>{createdStack}</strong>" created!
				</span>
			</div>
		</div>
	{/if}

	<!-- Form or action buttons -->
	{#if showForm}
		<StackForm onsubmit={handleCreateStack} oncancel={handleCancel} />
	{:else}
		<div class="space-y-4">
			{#if !createdStack}
				<button onclick={() => (showForm = true)} class="btn-primary w-full">
					<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					Create My First Habit Stack
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
					Create Another Habit Stack
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
					Back
				</button>
				<button onclick={onnext} class="btn-primary flex-1">
					{createdStack ? 'Continue' : 'Skip & Continue'}
					<svg class="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M9 5l7 7-7 7"
						/>
					</svg>
				</button>
			</div>
		</div>
	{/if}
</div>
