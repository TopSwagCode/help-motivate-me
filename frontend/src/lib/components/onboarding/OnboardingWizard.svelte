<script lang="ts">
	import IdentityStep from './IdentityStep.svelte';
	import HabitStackStep from './HabitStackStep.svelte';
	import GoalsStep from './GoalsStep.svelte';

	interface Props {
		oncomplete: () => void;
		onskip: () => void;
	}

	let { oncomplete, onskip }: Props = $props();

	let currentStep = $state(1);
	const totalSteps = 3;

	const steps = [
		{ number: 1, title: 'Identity', description: 'Define who you want to become' },
		{ number: 2, title: 'Habit Stacks', description: 'Build powerful habit chains' },
		{ number: 3, title: 'Goals', description: 'Set meaningful objectives' }
	];

	function nextStep() {
		if (currentStep < totalSteps) {
			currentStep++;
		} else {
			oncomplete();
		}
	}

	function prevStep() {
		if (currentStep > 1) {
			currentStep--;
		}
	}

	function handleStepComplete() {
		nextStep();
	}
</script>

<div class="min-h-screen bg-gradient-to-b from-primary-50 to-white">
	<div class="max-w-2xl mx-auto px-4 py-8">
		<!-- Header with skip button -->
		<div class="flex justify-between items-center mb-8">
			<h1 class="text-2xl font-bold text-gray-900">Welcome to HelpMotivateMe</h1>
			<button onclick={onskip} class="text-sm text-gray-500 hover:text-gray-700">
				Skip for now
			</button>
		</div>

		<!-- Progress indicator -->
		<div class="mb-8">
			<div class="flex items-center justify-between mb-2">
				{#each steps as step}
					<div class="flex items-center">
						<div
							class="w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium transition-colors {currentStep >= step.number
								? 'bg-primary-600 text-white'
								: 'bg-gray-200 text-gray-500'}"
						>
							{#if currentStep > step.number}
								<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
									<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
								</svg>
							{:else}
								{step.number}
							{/if}
						</div>
						{#if step.number < totalSteps}
							<div
								class="hidden sm:block w-24 h-1 mx-2 {currentStep > step.number
									? 'bg-primary-600'
									: 'bg-gray-200'}"
							></div>
						{/if}
					</div>
				{/each}
			</div>
			<div class="flex justify-between text-sm">
				{#each steps as step}
					<div
						class="text-center {currentStep >= step.number ? 'text-primary-600' : 'text-gray-400'}"
					>
						<span class="hidden sm:inline">{step.title}</span>
					</div>
				{/each}
			</div>
		</div>

		<!-- Step content -->
		<div class="card p-6 sm:p-8">
			{#if currentStep === 1}
				<IdentityStep onnext={handleStepComplete} onskip={nextStep} />
			{:else if currentStep === 2}
				<HabitStackStep onnext={handleStepComplete} onskip={nextStep} onback={prevStep} />
			{:else if currentStep === 3}
				<GoalsStep oncomplete={oncomplete} onskip={oncomplete} onback={prevStep} />
			{/if}
		</div>

		<!-- Step indicator text -->
		<p class="text-center text-sm text-gray-500 mt-4">
			Step {currentStep} of {totalSteps}: {steps[currentStep - 1].description}
		</p>
	</div>
</div>
