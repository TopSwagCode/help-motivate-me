<script lang="ts">
	import { t } from 'svelte-i18n';
	import { getCommitmentOptions, getActionSuggestions, createDailyCommitment } from '$lib/api/dailyCommitment';
	import type { IdentityOption, ActionSuggestion, CreateDailyCommitmentRequest } from '$lib/types';

	interface Props {
		isOpen: boolean;
		onClose: () => void;
		onCommitmentCreated: () => void;
	}

	let { isOpen, onClose, onCommitmentCreated }: Props = $props();

	// State
	let step = $state<'identity' | 'action' | 'confirm' | 'success'>('identity');
	let isLoading = $state(false);
	let error = $state<string | null>(null);

	// Data
	let identities = $state<IdentityOption[]>([]);
	let recommendedIdentityId = $state<string | null>(null);
	let suggestions = $state<ActionSuggestion[]>([]);

	// Selections
	let selectedIdentity = $state<IdentityOption | null>(null);
	let selectedSuggestion = $state<ActionSuggestion | null>(null);
	let customAction = $state('');

	// Load identity options when modal opens
	$effect(() => {
		if (isOpen) {
			loadIdentityOptions();
		} else {
			// Reset state when modal closes
			step = 'identity';
			selectedIdentity = null;
			selectedSuggestion = null;
			customAction = '';
			error = null;
		}
	});

	async function loadIdentityOptions() {
		isLoading = true;
		error = null;
		try {
			const options = await getCommitmentOptions();
			identities = options.identities;
			recommendedIdentityId = options.recommendedIdentityId;

			// Pre-select recommended identity
			if (recommendedIdentityId) {
				selectedIdentity = identities.find(i => i.id === recommendedIdentityId) || null;
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load identities';
		} finally {
			isLoading = false;
		}
	}

	async function loadSuggestions() {
		if (!selectedIdentity) return;
		isLoading = true;
		error = null;
		try {
			const result = await getActionSuggestions(selectedIdentity.id);
			suggestions = result.suggestions;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load suggestions';
		} finally {
			isLoading = false;
		}
	}

	function selectIdentity(identity: IdentityOption) {
		selectedIdentity = identity;
	}

	async function goToActionStep() {
		if (!selectedIdentity) return;
		await loadSuggestions();
		step = 'action';
	}

	function selectSuggestion(suggestion: ActionSuggestion) {
		selectedSuggestion = suggestion;
		customAction = '';
	}

	function clearSuggestionSelection() {
		selectedSuggestion = null;
	}

	function goToConfirmStep() {
		if (!selectedIdentity) return;
		if (!selectedSuggestion && !customAction.trim()) return;
		step = 'confirm';
	}

	async function createCommitment() {
		if (!selectedIdentity) return;

		const actionDescription = selectedSuggestion
			? selectedSuggestion.description
			: customAction.trim();

		if (!actionDescription) return;

		isLoading = true;
		error = null;

		try {
			const request: CreateDailyCommitmentRequest = {
				identityId: selectedIdentity.id,
				actionDescription,
				linkedHabitStackItemId: selectedSuggestion?.habitStackItemId || null,
				linkedTaskId: selectedSuggestion?.taskId || null
			};

			await createDailyCommitment(request);
			step = 'success';

			// Auto close after success
			setTimeout(() => {
				onCommitmentCreated();
				onClose();
			}, 2000);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to create commitment';
		} finally {
			isLoading = false;
		}
	}

	function goBack() {
		if (step === 'action') {
			step = 'identity';
			selectedSuggestion = null;
			customAction = '';
		} else if (step === 'confirm') {
			step = 'action';
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && step !== 'success') {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<!-- Backdrop -->
	<!-- svelte-ignore a11y_interactive_supports_focus -->
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
		onclick={() => step !== 'success' && onClose()}
		role="dialog"
		aria-modal="true"
	>
		<!-- Modal -->
		<div
			class="bg-warm-paper rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col"
			onclick={(e) => e.stopPropagation()}
		>
			<!-- Header -->
			{#if step !== 'success'}
				<div class="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
					<div class="flex items-center gap-2">
						{#if step !== 'identity'}
							<button
								onclick={goBack}
								class="p-1 -ml-1 text-gray-400 hover:text-cocoa-600 rounded-2xl hover:bg-primary-50 transition-colors"
							>
								<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
								</svg>
							</button>
						{/if}
						<h2 class="text-lg font-semibold text-cocoa-800">
							{#if step === 'identity'}
								{$t('dailyCommitment.modal.titleIdentity')}
							{:else if step === 'action'}
								{$t('dailyCommitment.modal.titleAction')}
							{:else if step === 'confirm'}
								{$t('dailyCommitment.modal.titleConfirm')}
							{/if}
						</h2>
					</div>
					<button
						onclick={onClose}
						class="p-1 text-gray-400 hover:text-cocoa-600 rounded-2xl hover:bg-primary-50 transition-colors"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</button>
				</div>
			{/if}

			<!-- Content -->
			<div class="flex-1 overflow-y-auto p-5">
				{#if error}
					<div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-2xl text-sm text-red-700">
						{error}
					</div>
				{/if}

				{#if step === 'identity'}
					<!-- Step 1: Identity Selection -->
					<p class="text-sm text-cocoa-600 mb-4">
						{$t('dailyCommitment.modal.identityPrompt')}
					</p>

					{#if isLoading}
						<div class="flex items-center justify-center py-8">
							<svg class="animate-spin h-8 w-8 text-primary-500" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						</div>
					{:else}
						<div class="space-y-2">
							{#each identities as identity (identity.id)}
								<button
									onclick={() => selectIdentity(identity)}
									class="w-full p-3 rounded-xl border-2 text-left transition-all hover:scale-[1.01] active:scale-[0.99]
										{selectedIdentity?.id === identity.id
											? 'border-primary-500 bg-primary-50'
											: 'border-primary-100 hover:border-primary-200 bg-warm-paper'}"
								>
									<div class="flex items-center gap-3">
										<div
											class="w-10 h-10 rounded-2xl flex items-center justify-center"
											style="background-color: {identity.color || '#d4944c'}20"
										>
											<span class="text-lg">{identity.icon || '🎯'}</span>
										</div>
										<div class="flex-1 min-w-0">
											<div class="flex items-center gap-2">
												<span class="font-medium text-cocoa-800">{identity.name}</span>
												{#if identity.isRecommended}
													<span class="text-[10px] font-medium px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700">
														{$t('dailyCommitment.modal.recommended')}
													</span>
												{/if}
											</div>
											<div class="flex items-center gap-2 mt-0.5">
												<div class="flex-1 h-1.5 bg-gray-100 rounded-full">
													<div
														class="h-1.5 rounded-full transition-all"
														style="width: {identity.score}%; background-color: {identity.color || '#d4944c'}"
													></div>
												</div>
												<span class="text-xs font-medium text-cocoa-500">{identity.score}</span>
											</div>
										</div>
										{#if selectedIdentity?.id === identity.id}
											<svg class="w-5 h-5 text-primary-500" fill="currentColor" viewBox="0 0 24 24">
												<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
											</svg>
										{/if}
									</div>
								</button>
							{/each}
						</div>
					{/if}

				{:else if step === 'action'}
					<!-- Step 2: Action Input -->
					<p class="text-sm text-cocoa-600 mb-2">
						{$t('dailyCommitment.modal.actionPrompt', { values: { identity: selectedIdentity?.name } })}
					</p>
					<p class="text-xs text-cocoa-500 mb-4 italic">
						💡 {$t('dailyCommitment.modal.actionHint')}
					</p>

					{#if isLoading}
						<div class="flex items-center justify-center py-8">
							<svg class="animate-spin h-8 w-8 text-primary-500" fill="none" viewBox="0 0 24 24">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
							</svg>
						</div>
					{:else}
						<!-- Suggestions -->
						{#if suggestions.length > 0}
							<div class="mb-4">
								<p class="text-xs font-medium text-cocoa-500 uppercase tracking-wide mb-2">
									{$t('dailyCommitment.modal.suggestions')}
								</p>
								<div class="space-y-2">
									{#each suggestions as suggestion, index (index)}
										<button
											onclick={() => selectSuggestion(suggestion)}
											class="w-full p-3 rounded-2xl border text-left transition-all
												{selectedSuggestion === suggestion
													? 'border-primary-500 bg-primary-50'
													: 'border-primary-100 hover:border-primary-200 bg-warm-paper'}"
										>
											<div class="flex items-center gap-2">
												<span class="text-sm">
													{suggestion.type === 'habit' ? '🔗' : '📋'}
												</span>
												<span class="text-sm text-cocoa-800">{suggestion.description}</span>
											</div>
										</button>
									{/each}
								</div>
							</div>
						{/if}

						<!-- Custom input -->
						<div>
							<p class="text-xs font-medium text-cocoa-500 uppercase tracking-wide mb-2">
								{suggestions.length > 0 ? $t('dailyCommitment.modal.orCustom') : $t('dailyCommitment.modal.yourAction')}
							</p>
							<textarea
								bind:value={customAction}
								onfocus={clearSuggestionSelection}
								placeholder={$t('dailyCommitment.modal.customPlaceholder')}
								rows="3"
								maxlength="500"
								class="w-full px-3 py-2 border border-primary-100 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-transparent resize-none text-sm"
							></textarea>
							<p class="text-xs text-gray-400 mt-1 text-right">{customAction.length}/500</p>
						</div>
					{/if}

				{:else if step === 'confirm'}
					<!-- Step 3: Confirmation -->
					<div class="text-center py-4">
						<div
							class="w-16 h-16 mx-auto mb-4 rounded-xl flex items-center justify-center"
							style="background-color: {selectedIdentity?.color || '#d4944c'}20"
						>
							<span class="text-3xl">{selectedIdentity?.icon || '🎯'}</span>
						</div>
						<p class="text-sm text-cocoa-600 mb-2">
							{$t('dailyCommitment.modal.confirmPrompt')}
						</p>
						<p class="text-lg font-medium text-cocoa-800 mb-1">
							"{selectedSuggestion?.description || customAction.trim()}"
						</p>
						<p class="text-sm" style="color: {selectedIdentity?.color || '#d4944c'}">
							{$t('dailyCommitment.modal.asA', { values: { identity: selectedIdentity?.name } })}
						</p>
					</div>

				{:else if step === 'success'}
					<!-- Step 4: Success -->
					<div class="text-center py-8">
						<div class="w-20 h-20 mx-auto mb-4 rounded-full bg-green-100 flex items-center justify-center animate-bounce">
							<img src="/done.png" alt="Done" class="object-contain" />
						</div>
						<h3 class="text-xl font-semibold text-cocoa-800 mb-2">
							{$t('dailyCommitment.modal.successTitle')}
						</h3>
						<p class="text-cocoa-600">
							{$t('dailyCommitment.reinforcement', { values: { identity: selectedIdentity?.name } })}
						</p>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			{#if step !== 'success'}
				<div class="px-5 py-4 border-t border-gray-100 bg-warm-cream">
					{#if step === 'identity'}
						<button
							onclick={goToActionStep}
							disabled={!selectedIdentity || isLoading}
							class="w-full py-2.5 bg-primary-600 hover:bg-primary-700 text-white font-medium rounded-2xl transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
						>
							{$t('dailyCommitment.modal.continueButton')}
						</button>
					{:else if step === 'action'}
						<button
							onclick={goToConfirmStep}
							disabled={!selectedSuggestion && !customAction.trim()}
							class="w-full py-2.5 bg-primary-600 hover:bg-primary-700 text-white font-medium rounded-2xl transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
						>
							{$t('dailyCommitment.modal.continueButton')}
						</button>
					{:else if step === 'confirm'}
						<button
							onclick={createCommitment}
							disabled={isLoading}
							class="w-full py-2.5 bg-green-500 hover:bg-green-600 text-white font-medium rounded-2xl transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
						>
							{#if isLoading}
								<svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
									<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
									<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
								</svg>
							{/if}
							{$t('dailyCommitment.modal.commitButton')}
						</button>
					{/if}
				</div>
			{/if}
		</div>
	</div>
{/if}
