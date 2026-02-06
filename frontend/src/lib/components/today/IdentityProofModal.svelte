<script lang="ts">
	import { t } from 'svelte-i18n';
	import { getCommitmentOptions } from '$lib/api/dailyCommitment';
	import { createIdentityProof } from '$lib/api/identityProofs';
	import { milestoneStore } from '$lib/stores/milestones';
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

			// Check for milestones that may have been unlocked
			milestoneStore.checkForNew();

			// Auto close after success
			setTimeout(() => {
				onProofCreated();
				onClose();
			}, 1500);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to log win';
		} finally {
			isSubmitting = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && !showSuccess) {
			onClose();
		}
	}

	const intensityOptions: { value: ProofIntensity; votes: number }[] = [
		{ value: 'Easy', votes: 1 },
		{ value: 'Moderate', votes: 2 },
		{ value: 'Hard', votes: 3 }
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
			class="bg-warm-paper rounded-t-2xl sm:rounded-xl shadow-xl w-full sm:max-w-sm overflow-hidden"
			onclick={(e) => e.stopPropagation()}
		>
			{#if showSuccess}
				<!-- Success State -->
				<div class="p-6 text-center">
					<div class="w-16 h-16 mx-auto mb-3 rounded-full bg-green-100 flex items-center justify-center">
						<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<p class="text-xl font-bold text-green-600">
						+{successVoteValue}
					</p>
					<p class="text-sm text-cocoa-500 mt-1">
						{selectedIdentity?.name}
					</p>
				</div>
			{:else}
				<!-- Header -->
				<div class="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
					<h2 class="text-base font-semibold text-cocoa-800">
						{$t('identityProof.title')}
					</h2>
					<button
						onclick={onClose}
						class="p-1 text-gray-400 hover:text-cocoa-600 rounded-2xl hover:bg-primary-50 transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</button>
				</div>

				<!-- Content -->
				<div class="p-4">
					{#if error}
						<div class="mb-3 p-2 bg-red-50 border border-red-200 rounded-2xl text-sm text-red-700">
							{error}
						</div>
					{/if}

					{#if isLoading}
						<div class="flex items-center justify-center py-8">
							<svg class="animate-spin h-6 w-6 text-primary-500" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						</div>
					{:else}
						<!-- Identity Selection -->
						<div class="mb-4">
							<label for="identity-select" class="text-xs font-medium text-cocoa-500 uppercase tracking-wide mb-2 block">
								{$t('identityProof.whichIdentity')}
							</label>
							<select
								id="identity-select"
								onchange={(e) => {
									const id = (e.target as HTMLSelectElement).value;
									const identity = identities.find(i => i.id === id);
									if (identity) selectIdentity(identity);
								}}
								class="w-full px-3 py-2 border border-primary-100 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm bg-warm-paper"
							>
								<option value="" disabled selected={!selectedIdentity}>
									{$t('identityProof.selectIdentity')}
								</option>
								{#each identities as identity (identity.id)}
									<option value={identity.id} selected={selectedIdentity?.id === identity.id}>
										{identity.icon ? `${identity.icon} ` : ''}{identity.name}
									</option>
								{/each}
							</select>
						</div>

						<!-- Intensity Selection -->
						<div class="mb-4">
							<p class="text-xs font-medium text-cocoa-500 uppercase tracking-wide mb-2">
								{$t('identityProof.howChallenging')}
							</p>
							<div class="flex gap-2">
								{#each intensityOptions as option (option.value)}
									<button
										onclick={() => selectIntensity(option.value)}
										class="flex-1 py-2 px-3 rounded-2xl border text-center transition-all
											{selectedIntensity === option.value
												? 'border-primary-500 bg-primary-50'
												: 'border-primary-100 hover:border-primary-200 bg-warm-paper'}"
									>
										<!-- Intensity dots indicator -->
										<div class="flex justify-center gap-1 mb-1">
											{#each Array(option.votes) as _, i}
												<div
													class="w-2 h-2 rounded-full {selectedIntensity === option.value ? 'bg-primary-500' : 'bg-gray-300'}"
												></div>
											{/each}
										</div>
										<span class="text-xs font-medium {selectedIntensity === option.value ? 'text-primary-700' : 'text-cocoa-600'}">
											{$t(`identityProof.${option.value.toLowerCase()}`)}
										</span>
									</button>
								{/each}
							</div>
						</div>

						<!-- Description (Optional) -->
						<div>
							<p class="text-xs font-medium text-cocoa-500 uppercase tracking-wide mb-2">
								{$t('identityProof.whatDidYouDo')} <span class="font-normal normal-case">({$t('common.optional')})</span>
							</p>
							<input
								type="text"
								bind:value={description}
								placeholder="..."
								maxlength="200"
								class="w-full px-3 py-2 border border-primary-100 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm"
							/>
						</div>
					{/if}
				</div>

				<!-- Footer -->
				<div class="px-4 pb-4">
					<button
						onclick={handleSubmit}
						disabled={!selectedIdentity || isSubmitting}
						class="w-full py-2.5 bg-amber-500 hover:bg-amber-600 text-white font-medium rounded-2xl transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
					>
						{#if isSubmitting}
							<svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						{/if}
						{$t('identityProof.logProof')} <span class="opacity-75">+{getVoteValue(selectedIntensity)}</span>
					</button>
				</div>
			{/if}
		</div>
	</div>
{/if}
