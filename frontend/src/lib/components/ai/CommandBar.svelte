<script lang="ts">
	import { tick } from 'svelte';
	import { t } from 'svelte-i18n';
	import VoiceInput from '$lib/components/shared/VoiceInput.svelte';
	import TaskPreviewCard from './previews/TaskPreviewCard.svelte';
	import GoalPreviewCard from './previews/GoalPreviewCard.svelte';
	import HabitStackPreviewCard from './previews/HabitStackPreviewCard.svelte';
	import IdentityPreviewCard from './previews/IdentityPreviewCard.svelte';
	import IdentityProofPreviewCard from './previews/IdentityProofPreviewCard.svelte';
	import {
		streamGeneralChat,
		stripJsonBlocks,
		type ChatMessage,
		type AiIntentResponse,
		type AiPreview,
		type TaskPreviewData,
		type GoalPreviewData,
		type HabitStackPreviewData,
		type IdentityPreviewData,
		type IdentityProofPreviewData
	} from '$lib/api/aiGeneral';
	import { getIdentities } from '$lib/api/identities';
	import { getGoals } from '$lib/api/goals';
	import type { Identity } from '$lib/types/identity';
	import type { Goal } from '$lib/types/goal';

	interface Props {
		isOpen: boolean;
		onClose: () => void;
		onCreateTask?: (data: TaskPreviewData) => Promise<void>;
		onCreateGoal?: (data: GoalPreviewData) => Promise<void>;
		onCreateHabitStack?: (data: HabitStackPreviewData) => Promise<void>;
		onCreateIdentity?: (data: IdentityPreviewData) => Promise<void>;
		onCreateIdentityProof?: (data: IdentityProofPreviewData) => Promise<void>;
	}

	let { isOpen, onClose, onCreateTask, onCreateGoal, onCreateHabitStack, onCreateIdentity, onCreateIdentityProof }: Props = $props();

	let inputValue = $state('');
	let isLoading = $state(false);
	let isCreating = $state(false);
	let messages = $state<ChatMessage[]>([]);
	let displayContent = $state('');
	let currentIntent = $state<AiIntentResponse | null>(null);
	let inputRef: HTMLInputElement | undefined = $state();
	let successMessage = $state<string | null>(null);

	// Data for editable dropdowns
	let identities = $state<Identity[]>([]);
	let goals = $state<Goal[]>([]);

	// Editable preview data - modified copy of currentIntent.preview.data
	let editablePreviewData = $state<TaskPreviewData | GoalPreviewData | HabitStackPreviewData | IdentityPreviewData | IdentityProofPreviewData | null>(null);

	// Load identities and goals when opening
	async function loadEditingData() {
		try {
			const [identitiesResult, goalsResult] = await Promise.all([
				getIdentities().catch(() => []),
				getGoals().catch(() => [])
			]);
			identities = identitiesResult;
			goals = goalsResult;
		} catch {
			// Silently fail - editing will work without dropdowns
		}
	}

	// Focus input when opened
	$effect(() => {
		if (isOpen) {
			// Reset state when opening
			inputValue = '';
			messages = [];
			displayContent = '';
			currentIntent = null;
			successMessage = null;
			editablePreviewData = null;
			loadEditingData();
			tick().then(() => inputRef?.focus());
		}
	});

	// Sync editable data when intent changes
	$effect(() => {
		if (currentIntent?.preview?.data) {
			editablePreviewData = { ...currentIntent.preview.data };
		} else {
			editablePreviewData = null;
		}
	});

	// Handler for preview data changes from child components
	function handlePreviewDataChange(data: TaskPreviewData | GoalPreviewData | HabitStackPreviewData | IdentityPreviewData | IdentityProofPreviewData) {
		editablePreviewData = data;
	}

	// Handle escape key
	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen) {
			e.preventDefault();
			onClose();
		}
	}

	async function handleSubmit() {
		if (!inputValue.trim() || isLoading) return;

		const userMessage = inputValue.trim();
		inputValue = '';
		messages = [...messages, { role: 'user', content: userMessage }];
		displayContent = '';
		currentIntent = null;
		isLoading = true;

		try {
			let fullContent = '';
			let lastIntent: AiIntentResponse | null = null;

			for await (const chunk of streamGeneralChat(messages)) {
				fullContent += chunk.content;
				if (chunk.intentData) {
					lastIntent = chunk.intentData;
				}
			}

			// Strip JSON from display
			displayContent = stripJsonBlocks(fullContent);
			messages = [...messages, { role: 'assistant', content: fullContent }];
			currentIntent = lastIntent;

			// If AI responded with createNow=true, auto-create
			if (lastIntent?.createNow && lastIntent.preview) {
				await createFromPreview(lastIntent.preview);
			}
		} catch (error) {
			console.error('AI chat error:', error);
			displayContent = $t('ai.errors.requestFailed');
		} finally {
			isLoading = false;
		}
	}

	async function handleAction(action: string) {
		if (action === 'confirm' && currentIntent?.preview) {
			await createFromPreview(currentIntent.preview);
		} else if (action === 'edit') {
			// For now, just ask AI to edit - could open a modal in the future
			inputValue = 'I want to make some changes';
			await handleSubmit();
		} else if (action === 'cancel') {
			onClose();
		} else if (['task', 'goal', 'habit_stack', 'make_task'].includes(action)) {
			// User is clarifying the type they want
			const actionLabel =
				action === 'task' || action === 'make_task'
					? 'a task'
					: action === 'goal'
						? 'a goal'
						: 'a habit stack';
			inputValue = `Make it ${actionLabel}`;
			await handleSubmit();
		} else {
			// Generic action - send as message
			inputValue = action;
			await handleSubmit();
		}
	}

	async function createFromPreview(preview: AiPreview) {
		isCreating = true;
		try {
			// Use editablePreviewData if available (user may have edited), otherwise fall back to original
			const dataToCreate = editablePreviewData ?? preview.data;

			if (preview.type === 'task' && onCreateTask) {
				await onCreateTask(dataToCreate as TaskPreviewData);
				successMessage = $t('ai.successMessages.taskCreated');
			} else if (preview.type === 'goal' && onCreateGoal) {
				await onCreateGoal(dataToCreate as GoalPreviewData);
				successMessage = $t('ai.successMessages.goalCreated');
			} else if (preview.type === 'habitStack' && onCreateHabitStack) {
				await onCreateHabitStack(dataToCreate as HabitStackPreviewData);
				successMessage = $t('ai.successMessages.habitStackCreated');
			} else if (preview.type === 'identity' && onCreateIdentity) {
				await onCreateIdentity(dataToCreate as IdentityPreviewData);
				successMessage = $t('ai.successMessages.identityCreated');
			} else if (preview.type === 'identityProof' && onCreateIdentityProof) {
				await onCreateIdentityProof(dataToCreate as IdentityProofPreviewData);
				successMessage = $t('ai.successMessages.identityProofCreated');
			}

			// Show success briefly then close
			setTimeout(() => {
				onClose();
			}, 1500);
		} catch (error) {
			console.error('Failed to create:', error);
			displayContent = $t('ai.errors.creationFailed');
		} finally {
			isCreating = false;
		}
	}

	function handleVoiceTranscription(text: string) {
		inputValue = text;
		handleSubmit();
	}

	function getActionLabel(action: string): string {
		const labels: Record<string, string> = {
			confirm: $t('ai.actions.confirm'),
			edit: $t('ai.actions.edit'),
			cancel: $t('ai.actions.cancel'),
			task: $t('ai.actions.task'),
			goal: $t('ai.actions.goal'),
			habit_stack: $t('ai.actions.habitStack'),
			make_task: $t('ai.actions.makeTask')
		};
		return labels[action] || action;
	}

	function getConfidenceLabel(confidence: number): string {
		if (confidence >= 0.85) return $t('ai.confidence.high');
		if (confidence >= 0.5) return $t('ai.confidence.medium');
		return $t('ai.confidence.low');
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<!-- Backdrop -->
	<button
		type="button"
		class="fixed inset-0 bg-black/50 z-50"
		onclick={onClose}
		aria-label="Close command bar"
	></button>

	<!-- Command Bar Modal -->
	<div class="fixed inset-x-0 top-[15%] mx-auto max-w-2xl z-50 px-4">
		<div class="bg-warm-paper rounded-2xl shadow-2xl overflow-hidden border border-primary-100">
			<!-- Success Message -->
			{#if successMessage}
				<div class="p-6 text-center">
					<div
						class="w-16 h-16 mx-auto mb-4 rounded-full bg-green-100 flex items-center justify-center"
					>
						<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M5 13l4 4L19 7"
							/>
						</svg>
					</div>
					<p class="text-lg font-semibold text-cocoa-800">{successMessage}</p>
				</div>
			{:else}
				<!-- Input area -->
				<div class="flex items-center gap-2 sm:gap-3 p-3 sm:p-4 border-b border-primary-100">
					<svg class="w-5 h-5 text-gray-400 flex-shrink-0 hidden xs:block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M13 10V3L4 14h7v7l9-11h-7z"
						/>
					</svg>
					<input
						bind:this={inputRef}
						bind:value={inputValue}
						onkeydown={(e) => e.key === 'Enter' && handleSubmit()}
						placeholder={$t('ai.commandBar.placeholder')}
						class="flex-1 text-sm sm:text-base outline-none bg-transparent placeholder-gray-400 min-w-0"
						disabled={isLoading || isCreating}
					/>
					<div class="flex-shrink-0">
						<VoiceInput onTranscription={handleVoiceTranscription} disabled={isLoading || isCreating} />
					</div>
					<kbd class="hidden sm:inline-block px-2 py-1 text-xs text-cocoa-500 bg-gray-100 rounded border border-primary-100 flex-shrink-0">
						Esc
					</kbd>
				</div>

				<!-- Content area -->
				{#if displayContent || currentIntent || isLoading}
					<div class="max-h-[50vh] overflow-y-auto">
						<!-- AI Response -->
						{#if displayContent}
							<div class="p-4 text-cocoa-700 whitespace-pre-wrap">{displayContent}</div>
						{/if}

						<!-- Preview Card -->
						{#if currentIntent?.preview}
							<div class="px-4 pb-4">
								<!-- Confidence indicator -->
								{#if currentIntent.confidence < 1}
									<div class="mb-3 flex items-center gap-2 text-sm">
										<div
											class="h-2 w-24 bg-gray-200 rounded-full overflow-hidden"
											title={`Confidence: ${Math.round(currentIntent.confidence * 100)}%`}
										>
											<div
												class="h-full transition-all duration-300"
												class:bg-green-500={currentIntent.confidence >= 0.85}
												class:bg-yellow-500={currentIntent.confidence >= 0.5 &&
													currentIntent.confidence < 0.85}
												class:bg-red-400={currentIntent.confidence < 0.5}
												style="width: {currentIntent.confidence * 100}%"
											></div>
										</div>
										<span class="text-cocoa-500">{getConfidenceLabel(currentIntent.confidence)}</span>
									</div>
								{/if}

								{#if currentIntent.preview.type === 'task' && editablePreviewData}
									<TaskPreviewCard
										data={editablePreviewData as TaskPreviewData}
										{identities}
										{goals}
										onchange={(d) => handlePreviewDataChange(d)}
									/>
								{:else if currentIntent.preview.type === 'goal' && editablePreviewData}
									<GoalPreviewCard
										data={editablePreviewData as GoalPreviewData}
										{identities}
										onchange={(d) => handlePreviewDataChange(d)}
									/>
								{:else if currentIntent.preview.type === 'habitStack' && editablePreviewData}
									<HabitStackPreviewCard
										data={editablePreviewData as HabitStackPreviewData}
										{identities}
										onchange={(d) => handlePreviewDataChange(d)}
									/>
								{:else if currentIntent.preview.type === 'identity' && editablePreviewData}
									<IdentityPreviewCard
										data={editablePreviewData as IdentityPreviewData}
										onchange={(d) => handlePreviewDataChange(d)}
									/>
								{:else if currentIntent.preview.type === 'identityProof' && editablePreviewData}
									<IdentityProofPreviewCard
										data={editablePreviewData as IdentityProofPreviewData}
										{identities}
										onchange={(d) => handlePreviewDataChange(d)}
									/>
								{/if}
							</div>
						{/if}

						<!-- Clarifying Question -->
						{#if currentIntent?.clarifyingQuestion && !currentIntent.preview}
							<div class="px-4 pb-4 text-cocoa-600 italic">
								{currentIntent.clarifyingQuestion}
							</div>
						{/if}

						<!-- Loading indicator -->
						{#if isLoading}
							<div class="flex items-center gap-3 p-4 text-cocoa-500">
								<div
									class="animate-spin w-5 h-5 border-2 border-primary-600 border-t-transparent rounded-full"
								></div>
								<span>{$t('ai.commandBar.thinking')}</span>
							</div>
						{/if}
					</div>
				{/if}

				<!-- Action buttons -->
				{#if currentIntent?.actions?.length && !isLoading}
					<div class="flex flex-wrap gap-2 p-4 border-t border-gray-100 bg-warm-cream">
						{#each currentIntent.actions as action}
							<button
								onclick={() => handleAction(action)}
								disabled={isCreating}
								class="px-4 py-2 text-sm font-medium rounded-2xl transition-colors disabled:opacity-50
                                    {action === 'confirm'
									? 'bg-primary-600 text-white hover:bg-primary-700'
									: action === 'cancel'
										? 'bg-warm-paper border border-primary-200 text-cocoa-700 hover:bg-warm-cream'
										: 'bg-warm-paper border border-primary-200 text-cocoa-700 hover:bg-warm-cream'}"
							>
								{#if action === 'confirm' && isCreating}
									<span class="flex items-center gap-2">
										<div
											class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"
										></div>
										{$t('ai.actions.creating')}
									</span>
								{:else}
									{getActionLabel(action)}
								{/if}
							</button>
						{/each}
					</div>
				{/if}

				<!-- Hint when empty -->
				{#if !displayContent && !currentIntent && !isLoading}
					<div class="p-4 text-center text-gray-400 text-sm">
						<p>{$t('ai.commandBar.hint')}</p>
						<p class="mt-2 text-xs">
							{$t('ai.commandBar.examples')}
						</p>
					</div>
				{/if}
			{/if}
		</div>
	</div>
{/if}
