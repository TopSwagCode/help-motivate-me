<script lang="ts">
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import VoiceInput from '$lib/components/shared/VoiceInput.svelte';
	import IdentityPreviewCard from '$lib/components/ai/previews/IdentityPreviewCard.svelte';
	import HabitStackPreviewCard from '$lib/components/ai/previews/HabitStackPreviewCard.svelte';
	import GoalPreviewCard from '$lib/components/ai/previews/GoalPreviewCard.svelte';
	import { streamOnboardingChat, type ChatMessage, type ExtractedData, type OnboardingStep } from '$lib/api/ai';
	import type { IdentityPreviewData, HabitStackPreviewData, GoalPreviewData } from '$lib/api/aiGeneral';

	interface Props {
		step: OnboardingStep;
		initialMessage?: string;
		onExtractedData: (data: ExtractedData) => Promise<void>;
		onSkip: () => void;
		onNext: () => void;
		onBack?: () => void;
		showBack?: boolean;
		onItemCreated?: () => void;
	}

	let { step, initialMessage, onExtractedData, onSkip, onNext, onBack, showBack = false, onItemCreated }: Props = $props();

	let inputValue = $state('');
	let isLoading = $state(false);
	let isCreating = $state(false);
	let displayContent = $state('');
	let currentPreview = $state<ExtractedData | null>(null);
	let hasCreatedItem = $state(false);
	let hasInitialized = $state(false);

	// Internal conversation history for API context (not displayed)
	let conversationHistory: ChatMessage[] = [];

	// Track created items to prevent duplicates
	const createdItems = new Set<string>();

	// Generate a unique key for an item to detect duplicates
	function getItemKey(data: ExtractedData): string {
		const dataObj = data.data as Record<string, unknown>;
		const type = data.type;

		if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
			const itemKeys = (dataObj.items as Array<Record<string, unknown>>).map(item => {
				if (type === 'identity') return `identity:${item.name}`;
				if (type === 'goal') return `goal:${item.title}`;
				return JSON.stringify(item);
			});
			return itemKeys.join('|');
		}

		if (type === 'habitStack' && Array.isArray(dataObj.stacks) && dataObj.stacks.length > 0) {
			const stackKeys = (dataObj.stacks as Array<Record<string, unknown>>).map(stack => {
				const habits = (stack.habits || stack.items || []) as Array<Record<string, string>>;
				const habitsKey = habits.map(h => h.habitDescription).join(',');
				return `habitStack:${stack.name}:${stack.triggerCue}:${habitsKey}`;
			});
			return stackKeys.join('|');
		}

		if (type === 'identity') return `identity:${dataObj.name}`;
		if (type === 'habitStack') {
			const habits = (dataObj.habits || dataObj.items || []) as Array<Record<string, string>>;
			const habitsKey = habits.map(h => h.habitDescription).join(',');
			return `habitStack:${dataObj.name}:${dataObj.triggerCue}:${habitsKey}`;
		}
		if (type === 'goal') return `goal:${dataObj.title}`;
		return JSON.stringify(data.data);
	}

	// Strip JSON code blocks from content
	function stripJsonBlocks(content: string): string {
		return content.replace(/```json[\s\S]*?```/g, '').trim();
	}

	// Validate extracted data structure
	function isValidExtractedData(data: ExtractedData | null): data is ExtractedData {
		if (!data || data.action !== 'create') return false;

		const dataObj = data.data as Record<string, unknown>;
		if (!dataObj || typeof dataObj !== 'object') return false;

		const type = data.type;
		if (!type) return false;

		if (type === 'identity') {
			if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
				const firstItem = dataObj.items[0] as Record<string, unknown>;
				return typeof firstItem.name === 'string' && firstItem.name.length > 0;
			}
			return typeof dataObj.name === 'string' && dataObj.name.length > 0;
		}

		if (type === 'habitStack') {
			if (Array.isArray(dataObj.stacks) && dataObj.stacks.length > 0) {
				const firstStack = dataObj.stacks[0] as Record<string, unknown>;
				const hasName = typeof firstStack.name === 'string' && firstStack.name.length > 0;
				const hasHabits = Array.isArray(firstStack.habits) && firstStack.habits.length > 0 ||
				                  Array.isArray(firstStack.items) && firstStack.items.length > 0;
				return hasName && hasHabits;
			}
			const hasName = typeof dataObj.name === 'string' && dataObj.name.length > 0;
			const hasHabits = Array.isArray(dataObj.items) && dataObj.items.length > 0 ||
			                  Array.isArray(dataObj.habits) && dataObj.habits.length > 0;
			return hasName && hasHabits;
		}

		if (type === 'goal') {
			if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
				const firstItem = dataObj.items[0] as Record<string, unknown>;
				return typeof firstItem.title === 'string' && firstItem.title.length > 0;
			}
			return typeof dataObj.title === 'string' && dataObj.title.length > 0;
		}

		return false;
	}

	// Map ExtractedData to preview card format
	function mapToPreviewData(data: ExtractedData): { type: string; items: unknown[] } | null {
		if (!isValidExtractedData(data)) return null;

		const dataObj = data.data as Record<string, unknown>;
		const type = data.type!;
		const items: unknown[] = [];

		if (type === 'identity') {
			if (Array.isArray(dataObj.items)) {
				for (const item of dataObj.items as Record<string, unknown>[]) {
					items.push({
						name: item.name,
						description: item.description,
						icon: item.icon,
						color: item.color,
						reasoning: item.reasoning
					} as IdentityPreviewData);
				}
			} else {
				items.push({
					name: dataObj.name,
					description: dataObj.description,
					icon: dataObj.icon,
					color: dataObj.color,
					reasoning: dataObj.reasoning
				} as IdentityPreviewData);
			}
		} else if (type === 'habitStack') {
			if (Array.isArray(dataObj.stacks)) {
				for (const stack of dataObj.stacks as Record<string, unknown>[]) {
					items.push({
						name: stack.name,
						description: stack.description,
						triggerCue: stack.triggerCue,
						habits: stack.habits || stack.items,
						reasoning: stack.reasoning
					} as HabitStackPreviewData);
				}
			} else {
				items.push({
					name: dataObj.name,
					description: dataObj.description,
					triggerCue: dataObj.triggerCue,
					habits: dataObj.habits || dataObj.items,
					reasoning: dataObj.reasoning
				} as HabitStackPreviewData);
			}
		} else if (type === 'goal') {
			if (Array.isArray(dataObj.items)) {
				for (const item of dataObj.items as Record<string, unknown>[]) {
					items.push({
						title: item.title,
						description: item.description,
						targetDate: item.targetDate,
						reasoning: item.reasoning
					} as GoalPreviewData);
				}
			} else {
				items.push({
					title: dataObj.title,
					description: dataObj.description,
					targetDate: dataObj.targetDate,
					reasoning: dataObj.reasoning
				} as GoalPreviewData);
			}
		}

		return items.length > 0 ? { type, items } : null;
	}

	// Initialize with greeting
	$effect(() => {
		if (hasInitialized) return;
		hasInitialized = true;

		if (initialMessage) {
			displayContent = initialMessage;
		}
	});

	async function sendMessage(content: string) {
		if (isLoading || !content.trim()) return;

		const userMessage = content.trim();
		conversationHistory = [...conversationHistory, { role: 'user', content: userMessage }];
		inputValue = '';
		isLoading = true;
		currentPreview = null;
		displayContent = '';

		try {
			let fullContent = '';
			let lastExtractedData: ExtractedData | null = null;

			for await (const chunk of streamOnboardingChat(conversationHistory, step)) {
				fullContent += chunk.content;
				if (chunk.extractedData) {
					lastExtractedData = chunk.extractedData;
				}
				if (chunk.isComplete) break;
			}

			conversationHistory = [...conversationHistory, { role: 'assistant', content: fullContent }];
			displayContent = stripJsonBlocks(fullContent);

			// Handle navigation actions silently
			if (lastExtractedData?.action === 'next_step' || lastExtractedData?.action === 'complete') {
				onNext();
				return;
			}
			if (lastExtractedData?.action === 'skip') {
				onSkip();
				return;
			}

			// Set preview if valid create action
			if (lastExtractedData && isValidExtractedData(lastExtractedData)) {
				currentPreview = lastExtractedData;
			}

		} catch (error) {
			console.error('Chat error:', error);
			displayContent = get(t)('onboarding.chat.errorGeneric');
		} finally {
			isLoading = false;
		}
	}

	async function handleAccept() {
		if (!currentPreview || isCreating) return;

		const itemKey = getItemKey(currentPreview);
		if (createdItems.has(itemKey)) {
			console.log('Duplicate item detected, skipping:', itemKey);
			currentPreview = null;
			return;
		}

		isCreating = true;
		try {
			createdItems.add(itemKey);
			await onExtractedData(currentPreview);
			hasCreatedItem = true;
			currentPreview = null;
			displayContent = get(t)('onboarding.assistant.created');
			onItemCreated?.();
		} catch (error) {
			console.error('Failed to create:', error);
			createdItems.delete(itemKey);
			displayContent = get(t)('onboarding.chat.errorSaving');
		} finally {
			isCreating = false;
		}
	}

	function handleAddMore() {
		currentPreview = null;
		displayContent = get(t)('onboarding.assistant.addMorePrompt');
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Enter' && !event.shiftKey) {
			event.preventDefault();
			sendMessage(inputValue);
		}
	}

	function handleTranscription(text: string) {
		inputValue = text;
		sendMessage(text);
	}

	// Get preview data for rendering
	$effect(() => {
		// This reactive effect ensures preview cards update when currentPreview changes
	});

	const previewData = $derived(currentPreview ? mapToPreviewData(currentPreview) : null);
</script>

<div class="flex flex-col h-full">
	<!-- AI Response area -->
	<div class="flex-1 overflow-y-auto p-4">
		<!-- Display content -->
		{#if displayContent || isLoading}
			<div class="mb-4">
				{#if isLoading}
					<div class="flex items-center gap-3 text-gray-500">
						<div class="animate-spin w-5 h-5 border-2 border-primary-600 border-t-transparent rounded-full"></div>
						<span>{$t('ai.commandBar.thinking')}</span>
					</div>
				{:else}
					<p class="text-gray-700 whitespace-pre-wrap">{displayContent}</p>
				{/if}
			</div>
		{/if}

		<!-- Preview cards -->
		{#if previewData && !isLoading}
			<div class="space-y-3">
				{#each previewData.items as item}
					{#if previewData.type === 'identity'}
						<IdentityPreviewCard data={item as IdentityPreviewData} />
					{:else if previewData.type === 'habitStack'}
						<HabitStackPreviewCard data={item as HabitStackPreviewData} />
					{:else if previewData.type === 'goal'}
						<GoalPreviewCard data={item as GoalPreviewData} />
					{/if}
				{/each}
			</div>
		{/if}

		<!-- Empty state hint -->
		{#if !displayContent && !currentPreview && !isLoading}
			<div class="text-center text-gray-400 py-8">
				<p>{$t('onboarding.assistant.hint')}</p>
			</div>
		{/if}
	</div>

	<!-- Input and actions area -->
	<div class="border-t bg-white p-4 flex-shrink-0">
		<!-- Input row -->
		<div class="flex items-end gap-2 mb-3">
			<div class="flex-1">
				<textarea
					bind:value={inputValue}
					onkeydown={handleKeydown}
					placeholder={$t('onboarding.chat.placeholder')}
					disabled={isLoading || isCreating}
					rows="1"
					class="input resize-none text-base w-full"
				></textarea>
			</div>

			<VoiceInput
				onTranscription={handleTranscription}
				disabled={isLoading || isCreating}
			/>

			<button
				onclick={() => sendMessage(inputValue)}
				disabled={isLoading || isCreating || !inputValue.trim()}
				class="btn-primary p-2.5 flex-shrink-0"
				aria-label={$t('onboarding.chat.send')}
			>
				<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
					/>
				</svg>
			</button>
		</div>

		<!-- Action buttons -->
		<div class="flex flex-wrap gap-2">
			{#if currentPreview && !isLoading}
				<button
					onclick={handleAccept}
					disabled={isCreating}
					class="btn-primary px-4 py-2 text-sm"
				>
					{#if isCreating}
						<span class="flex items-center gap-2">
							<div class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
							{$t('common.saving')}
						</span>
					{:else}
						{$t('onboarding.assistant.accept')}
					{/if}
				</button>
			{/if}

			{#if hasCreatedItem && !currentPreview && !isLoading}
				<button
					onclick={handleAddMore}
					class="btn-secondary px-4 py-2 text-sm"
				>
					{$t('onboarding.assistant.addMore')}
				</button>
				<button
					onclick={onNext}
					class="btn-primary px-4 py-2 text-sm"
				>
					{$t('onboarding.assistant.done')}
				</button>
			{/if}

			<button
				onclick={onSkip}
				class="btn-text px-4 py-2 text-sm text-gray-500 hover:text-gray-700"
			>
				{$t('onboarding.chat.skipStep')}
			</button>

			{#if showBack && onBack}
				<button
					onclick={onBack}
					class="btn-text px-4 py-2 text-sm text-gray-500 hover:text-gray-700 ml-auto"
				>
					{$t('onboarding.chat.back')}
				</button>
			{/if}
		</div>
	</div>
</div>
