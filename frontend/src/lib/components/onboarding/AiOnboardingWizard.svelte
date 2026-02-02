<script lang="ts">
	import ChatIdentityStep, { type CreatedIdentity } from './ChatIdentityStep.svelte';
	import ChatHabitStackStep from './ChatHabitStackStep.svelte';
	import ChatGoalsStep from './ChatGoalsStep.svelte';

	interface Props {
		oncomplete: () => void;
		onskip: () => void;
	}

	let { oncomplete, onskip }: Props = $props();

	let currentStep = $state(1);
	const totalSteps = 3;

	// Track created identities to pass to subsequent steps
	let createdIdentities = $state<CreatedIdentity[]>([]);

	const steps = [
		{ number: 1, title: 'Identity', description: 'Define who you want to become' },
		{ number: 2, title: 'Habit Stacks', description: 'Build powerful habit chains' },
		{ number: 3, title: 'Goals', description: 'Set meaningful objectives' }
	];

	function handleIdentityNext(identities: CreatedIdentity[]) {
		createdIdentities = identities;
		nextStep();
	}

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
</script>

<div class="h-[100dvh] bg-gradient-to-b from-primary-50 to-white flex flex-col overflow-hidden">
	<div class="max-w-2xl mx-auto px-3 sm:px-4 py-3 sm:py-6 w-full flex-shrink-0">
		<!-- Header with skip button -->
		<div class="flex justify-between items-center mb-4 sm:mb-6">
			<h1 class="text-lg sm:text-xl font-bold text-cocoa-800">AI-Assisted Setup</h1>
			<button onclick={onskip} class="text-sm text-cocoa-500 hover:text-cocoa-700">
				Skip for now
			</button>
		</div>

		<!-- Progress indicator -->
		<div class="mb-4 sm:mb-6">
			<div class="flex items-center justify-center">
				{#each steps as step, index}
					<!-- Step circle -->
					<div class="flex flex-col items-center">
						<div
							class="w-8 h-8 sm:w-10 sm:h-10 rounded-full flex items-center justify-center text-xs sm:text-sm font-medium transition-colors {currentStep >= step.number
								? 'bg-primary-600 text-white'
								: 'bg-gray-200 text-cocoa-500'}"
						>
							{#if currentStep > step.number}
								<svg class="w-4 h-4 sm:w-5 sm:h-5" fill="currentColor" viewBox="0 0 24 24">
									<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
								</svg>
							{:else}
								{step.number}
							{/if}
						</div>
						<span
							class="mt-1 sm:mt-2 text-[10px] sm:text-xs font-medium {currentStep >= step.number ? 'text-primary-600' : 'text-gray-400'}"
						>
							{step.title}
						</span>
					</div>
					<!-- Connecting line (not after last step) -->
					{#if index < steps.length - 1}
						<div
							class="w-8 sm:w-20 h-0.5 sm:h-1 mx-1 sm:mx-2 mb-5 sm:mb-6 transition-colors {currentStep > step.number
								? 'bg-primary-600'
								: 'bg-gray-200'}"
						></div>
					{/if}
				{/each}
			</div>
		</div>
	</div>

	<!-- Step content - takes remaining height -->
	<div class="flex-1 max-w-2xl mx-auto w-full px-2 sm:px-4 pb-2 sm:pb-4 min-h-0">
		<div class="card h-full flex flex-col overflow-hidden">
			{#if currentStep === 1}
				<ChatIdentityStep onnext={handleIdentityNext} onskip={nextStep} />
			{:else if currentStep === 2}
				<ChatHabitStackStep onnext={nextStep} onskip={nextStep} onback={prevStep} identities={createdIdentities} />
			{:else if currentStep === 3}
				<ChatGoalsStep oncomplete={oncomplete} onskip={oncomplete} onback={prevStep} identities={createdIdentities} />
			{/if}
		</div>
	</div>

	<!-- Step indicator text -->
	<div class="max-w-2xl mx-auto px-4 py-2 sm:py-3 w-full flex-shrink-0">
		<p class="text-center text-xs sm:text-sm text-cocoa-500">
			Step {currentStep} of {totalSteps}: {steps[currentStep - 1].description}
		</p>
	</div>
</div>
