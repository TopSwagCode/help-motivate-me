<script lang="ts">
	import { t } from 'svelte-i18n';
	import { getCommitmentOptions } from '$lib/api/dailyCommitment';
	import { createIdentityProof } from '$lib/api/identityProofs';
	import type { IdentityOption } from '$lib/types/dailyCommitment';
	import type { ProofIntensity, CreateIdentityProofRequest } from '$lib/types/identityProof';

	interface Props {
		isOpen: boolean;
		onClose: () => void;
		onProofCreated: () => void;
	}

	let { isOpen, onClose, onProofCreated }: Props = $props();

	// State
	let isLoading = $state(false);
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);
	let showSuccess = $state(false);
	let successVoteValue = $state(0);

	// Data
	let identities = $state<IdentityOption[]>([]);

	// Selections
	let selectedIdentity = $state<IdentityOption | null>(null);
	let selectedIntensity = $state<ProofIntensity>('Easy');
	let description = $state('');

	// Load identity options when modal opens
	$effect(() => {
		if (isOpen) {
			loadIdentityOptions();
		} else {
			// Reset state when modal closes
			selectedIdentity = null;
			selectedIntensity = 'Easy';
			description = '';
			error = null;
			showSuccess = false;
		}
	});

	async function loadIdentityOptions() {
		isLoading = true;
		error = null;
		try {
			const options = await getCommitmentOptions();
			identities = options.identities;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load identities';
		} finally {
			isLoading = false;
		}
	}

	function selectIdentity(identity: IdentityOption) {
		selectedIdentity = identity;
	}

	function selectIntensity(intensity: ProofIntensity) {
		selectedIntensity = intensity;
	}

	function getVoteValue(intensity: ProofIntensity): number {
		switch (intensity) {
			case 'Easy':
				return 1;
			case 'Moderate':
				return 2;
			case 'Hard':
				return 3;
		}
	}

	async function handleSubmit() {
		if (!selectedIdentity) return;

		isSubmitting = true;
		error = null;

		try {
			const request: CreateIdentityProofRequest = {
				identityId: selectedIdentity.id,
				description: description.trim() || null,
				intensity: selectedIntensity
			};

			await createIdentityProof(request);

			// Show success animation
			successVoteValue = getVoteValue(selectedIntensity);
			showSuccess = true;

			// Auto close after success
			setTimeout(() => {
				onProofCreated();
				onClose();
			}, 2000);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to log proof';
		} finally {
			isSubmitting = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && !showSuccess) {
			onClose();
		}
	}

	const intensityOptions: { value: ProofIntensity; emoji: string; votes: number }[] = [
		{ value: 'Easy', emoji: '🔹', votes: 1 },
		{ value: 'Moderate', emoji: '🔸', votes: 2 },
		{ value: 'Hard', emoji: '🔥', votes: 3 }
	];
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<!-- Backdrop -->
	<!-- svelte-ignore a11y_interactive_supports_focus -->
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-end sm:items-center justify-center"
		onclick={() => !showSuccess && onClose()}
		role="dialog"
		aria-modal="true"
	>
		<!-- Modal (bottom sheet on mobile, centered on desktop) -->
		<div
			class="bg-white rounded-t-2xl sm:rounded-2xl shadow-xl w-full sm:max-w-md max-h-[85vh] overflow-hidden flex flex-col"
			onclick={(e) => e.stopPropagation()}
		>
			{#if showSuccess}
				<!-- Success State -->
				<div class="p-6 text-center">
					<div class="w-20 h-20 mx-auto mb-4 rounded-full bg-green-100 flex items-center justify-center">
						<span class="text-4xl animate-bounce">⚡</span>
					</div>
					<h3 class="text-xl font-semibold text-gray-900 mb-2">
						{$t('identityProof.success')}
					</h3>
					<p class="text-2xl font-bold text-green-600 mb-2">
						+{successVoteValue} {successVoteValue === 1 ? 'vote' : 'votes'}
					</p>
					<p class="text-gray-600">
						{$t('dailyCommitment.reinforcement', { values: { identity: selectedIdentity?.name } })}
					</p>
				</div>
			{:else}
				<!-- Header -->
				<div class="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
					<h2 class="text-lg font-semibold text-gray-900">
						{$t('identityProof.title')}
					</h2>
					<button
						onclick={onClose}
						class="p-1 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</button>
				</div>

				<!-- Content -->
				<div class="flex-1 overflow-y-auto p-5">
					{#if error}
						<div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
							{error}
						</div>
					{/if}

					{#if isLoading}
						<div class="flex items-center justify-center py-8">
							<svg class="animate-spin h-8 w-8 text-primary-500" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						</div>
					{:else}
						<!-- Identity Selection -->
						<div class="mb-5">
							<p class="text-sm font-medium text-gray-700 mb-3">
								{$t('identityProof.whichIdentity')}
							</p>
							<div class="flex flex-wrap gap-2">
								{#each identities as identity (identity.id)}
									<button
										onclick={() => selectIdentity(identity)}
										class="px-3 py-2 rounded-xl border-2 text-sm font-medium transition-all
											{selectedIdentity?.id === identity.id
												? 'border-primary-500 bg-primary-50 text-primary-700'
												: 'border-gray-200 hover:border-gray-300 bg-white text-gray-700'}"
									>
										<span class="mr-1">{identity.icon || '🎯'}</span>
										{identity.name}
									</button>
								{/each}
							</div>
						</div>

						<!-- Intensity Selection -->
						<div class="mb-5">
							<p class="text-sm font-medium text-gray-700 mb-3">
								{$t('identityProof.howChallenging')}
							</p>
							<div class="grid grid-cols-3 gap-2">
								{#each intensityOptions as option (option.value)}
									<button
										onclick={() => selectIntensity(option.value)}
										class="p-3 rounded-xl border-2 text-center transition-all
											{selectedIntensity === option.value
												? 'border-primary-500 bg-primary-50'
												: 'border-gray-200 hover:border-gray-300 bg-white'}"
									>
										<span class="text-xl mb-1 block">{option.emoji}</span>
										<span class="text-xs font-medium text-gray-900 block">
											{$t(`identityProof.${option.value.toLowerCase()}`)}
										</span>
										<span class="text-xs text-gray-500 block">+{option.votes}</span>
									</button>
								{/each}
							</div>
						</div>

						<!-- Description (Optional) -->
						<div>
							<p class="text-sm font-medium text-gray-700 mb-2">
								{$t('identityProof.whatDidYouDo')} <span class="text-gray-400 font-normal">({$t('common.optional')})</span>
							</p>
							<textarea
								bind:value={description}
								placeholder="..."
								rows="2"
								maxlength="200"
								class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent resize-none text-sm"
							></textarea>
							<p class="text-xs text-gray-400 mt-1 text-right">{description.length}/200</p>
						</div>
					{/if}
				</div>

				<!-- Footer -->
				<div class="px-5 py-4 border-t border-gray-100 bg-gray-50">
					<button
						onclick={handleSubmit}
						disabled={!selectedIdentity || isSubmitting}
						class="w-full py-2.5 bg-primary-600 hover:bg-primary-700 text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
					>
						{#if isSubmitting}
							<svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						{:else}
							<span>⚡</span>
						{/if}
						{$t('identityProof.logProof')} (+{getVoteValue(selectedIntensity)})
					</button>
				</div>
			{/if}
		</div>
	</div>
{/if}
