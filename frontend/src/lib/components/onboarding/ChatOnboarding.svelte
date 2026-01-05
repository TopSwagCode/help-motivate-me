<script lang="ts">
	import { tick } from 'svelte';
	import VoiceInput from '$lib/components/shared/VoiceInput.svelte';
	import { streamOnboardingChat, type ChatMessage, type ExtractedData, type OnboardingStep } from '$lib/api/ai';

	interface Props {
		step: OnboardingStep;
		initialMessage?: string;
		onExtractedData: (data: ExtractedData) => Promise<void>;
		onSkip: () => void;
		onNext: () => void;
		onBack?: () => void;
		showBack?: boolean;
	}

	let { step, initialMessage, onExtractedData, onSkip, onNext, onBack, showBack = false }: Props = $props();

	interface DisplayMessage {
		role: 'user' | 'assistant';
		content: string;
		isStreaming?: boolean;
		extractedData?: ExtractedData | null;
	}

	let messages = $state<DisplayMessage[]>([]);
	let inputValue = $state('');
	let isLoading = $state(false);
	let isTypingMessage = $state(false);
	let messagesContainer: HTMLDivElement;
	let isProcessing = $state(false);
	let hasInitialized = $state(false);
	let successNotification = $state<{ name: string; icon: string; color: string } | null>(null);
	let extractedDataProcessed = $state(false);
	let suggestedActions = $state<string[]>([]);

	// Track created items to prevent duplicates (using JSON stringified key)
	const createdItems = new Set<string>();

	// Generate a unique key for an item to detect duplicates
	function getItemKey(data: ExtractedData): string {
		const dataObj = data.data as Record<string, unknown>;
		const type = data.type; // type is at root level
		if (type === 'identity') {
			return `identity:${dataObj.name}`;
		} else if (type === 'habitStack') {
			return `habitStack:${dataObj.name}:${dataObj.triggerCue}`;
		} else if (type === 'goal') {
			return `goal:${dataObj.title}`;
		}
		return JSON.stringify(data.data);
	}

	// Get default suggested actions - simple and consistent
	function getDefaultSuggestedActions(): string[] {
		return ['Yes', 'Skip'];
	}

	// Strip JSON code blocks from content for display
	function stripJsonBlocks(content: string): string {
		return content.replace(/```json[\s\S]*?```/g, '').trim();
	}

	// Check if action requires creating something (not just navigation)
	function isCreateAction(data: ExtractedData | null): boolean {
		return data?.action === 'create';
	}

	// Check if action is navigation (skip, next_step, complete)
	function isNavigationAction(data: ExtractedData | null): boolean {
		if (!data) return false;
		return ['skip', 'next_step', 'complete'].includes(data.action);
	}

	// Validate extracted data has the expected structure
	function isValidExtractedData(data: ExtractedData | null): data is ExtractedData {
		if (!data || !data.action) return false;

		const validActions = ['none', 'create', 'next_step', 'complete', 'skip'];
		if (!validActions.includes(data.action)) return false;

		// For create actions, validate the data structure based on step
		if (data.action === 'create') {
			const dataObj = data.data as Record<string, unknown>;
			if (!dataObj || typeof dataObj !== 'object') return false;

			// Type is at root level: {"action":"create","type":"identity","data":{...}}
			const type = data.type;
			if (!type) {
				console.warn('No type field found in extracted data');
				return false;
			}

			// Validate based on type
			if (type === 'identity') {
				const hasName = typeof dataObj.name === 'string' && dataObj.name.length > 0;
				if (!hasName) console.warn('Identity validation failed: missing name');
				return hasName;
			} else if (type === 'habitStack') {
				const hasName = typeof dataObj.name === 'string' && dataObj.name.length > 0;
				const hasItems = Array.isArray(dataObj.items) && dataObj.items.length > 0;
				if (!hasName || !hasItems) console.warn('HabitStack validation failed:', { hasName, hasItems });
				return hasName && hasItems;
			} else if (type === 'goal') {
				const hasTitle = typeof dataObj.title === 'string' && dataObj.title.length > 0;
				if (!hasTitle) console.warn('Goal validation failed: missing title');
				return hasTitle;
			}

			console.warn('Unknown type:', type);
			return false;
		}

		return true;
	}

	// Typewriter effect for any message
	async function typeMessage(message: string, extractedData: ExtractedData | null = null): Promise<void> {
		isTypingMessage = true;

		// Add placeholder
		messages = [...messages, { role: 'assistant', content: '', isStreaming: true }];

		const words = message.split(' ');
		let currentContent = '';

		for (let i = 0; i < words.length; i++) {
			currentContent += (i === 0 ? '' : ' ') + words[i];
			messages = messages.map((m, idx) =>
				idx === messages.length - 1
					? { ...m, content: currentContent }
					: m
			);
			await new Promise((resolve) => setTimeout(resolve, 15 + Math.random() * 25));
		}

		// Finalize the message
		messages = messages.map((m, idx) =>
			idx === messages.length - 1
				? { ...m, content: message, isStreaming: false, extractedData }
				: m
		);
		isTypingMessage = false;

		// Update suggested actions from extracted data, or use defaults
		// Use tick() to ensure state updates before setting suggested actions
		await tick();
		if (extractedData?.suggestedActions?.length) {
			suggestedActions = extractedData.suggestedActions;
		} else {
			suggestedActions = getDefaultSuggestedActions();
		}
	}

	// Initialize with greeting - only run once
	$effect(() => {
		if (hasInitialized) return;
		hasInitialized = true;

		if (initialMessage) {
			typeMessage(initialMessage);
		}
	});

	async function scrollToBottom() {
		await tick();
		if (messagesContainer) {
			messagesContainer.scrollTop = messagesContainer.scrollHeight;
		}
	}

	$effect(() => {
		if (messages.length > 0) {
			scrollToBottom();
		}
	});

	// Handle quick action button click
	async function handleQuickAction(action: string) {
		// Clear suggested actions immediately
		suggestedActions = [];

		const lowerAction = action.toLowerCase();

		// Handle "Skip" action directly without API call
		if (lowerAction === 'skip' || lowerAction === 'skip this step') {
			onSkip();
			return;
		}

		// Handle "Done" type actions - send to API but navigate silently
		if (lowerAction === 'done' || lowerAction.includes('done') || lowerAction.includes('next step') || lowerAction.includes('finish')) {
			await sendMessage(action, false, true);
			return;
		}

		// For "Yes" or any other action, send as regular message
		await sendMessage(action);
	}

	async function sendMessage(content: string, isInitial = false, silentNavigation = false) {
		if (isLoading || isTypingMessage) return;
		if (!isInitial && !content.trim()) return;

		const userMessage = content.trim();

		// Build message history for API BEFORE adding the new message
		const apiMessages: ChatMessage[] = messages
			.filter(m => !m.isStreaming)
			.map(m => ({ role: m.role, content: m.content }));

		// Add user message if this is not the initial greeting request
		if (userMessage && !isInitial) {
			apiMessages.push({ role: 'user', content: userMessage });
			// Only show user message in chat if not silent navigation
			if (!silentNavigation) {
				messages = [...messages, { role: 'user', content: userMessage }];
			}
		}

		inputValue = '';
		isLoading = true;
		extractedDataProcessed = false;
		suggestedActions = []; // Clear while loading

		// Show loading indicator
		messages = [...messages, { role: 'assistant', content: '', isStreaming: true }];

		try {
			let fullContent = '';
			let lastExtractedData: ExtractedData | null = null;

			// Collect the full response first (no streaming to UI)
			for await (const chunk of streamOnboardingChat(apiMessages, step)) {
				fullContent += chunk.content;

				if (chunk.extractedData) {
					lastExtractedData = chunk.extractedData;
				}

				if (chunk.isComplete) {
					break;
				}
			}

			// Remove the loading placeholder
			messages = messages.filter((_, i) => i !== messages.length - 1);

			// Strip JSON from content
			const displayContent = stripJsonBlocks(fullContent);

			// Check if this is a navigation action (skip/next/complete)
			if (isNavigationAction(lastExtractedData)) {
				// For silent navigation, also remove the user message we just added
				if (silentNavigation && messages.length > 0 && messages[messages.length - 1].role === 'user') {
					messages = messages.slice(0, -1);
				}

				// Handle navigation without showing message
				if (!extractedDataProcessed) {
					extractedDataProcessed = true;
					await handleExtractedData(lastExtractedData!);
				}
				return;
			}

			// For non-navigation responses, use typewriter effect
			if (displayContent) {
				await typeMessage(displayContent, lastExtractedData);
			}

			// Handle create actions after showing message
			if (!extractedDataProcessed && lastExtractedData) {
				console.log('Extracted data:', JSON.stringify(lastExtractedData, null, 2));

				if (isCreateAction(lastExtractedData)) {
					if (isValidExtractedData(lastExtractedData)) {
						extractedDataProcessed = true;
						await handleExtractedData(lastExtractedData);
					} else {
						console.warn('Create action received but data validation failed:', lastExtractedData);
					}
				}
			}

		} catch (error) {
			console.error('Chat error:', error);
			// Remove loading placeholder and show error
			messages = messages.filter((_, i) => i !== messages.length - 1);
			messages = [...messages, {
				role: 'assistant',
				content: 'Sorry, something went wrong. Please try again.',
				isStreaming: false
			}];
		} finally {
			isLoading = false;
		}
	}

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create') {
			// Check for duplicate before creating
			const itemKey = getItemKey(data);
			if (createdItems.has(itemKey)) {
				console.log('Duplicate item detected, skipping:', itemKey);
				return;
			}

			isProcessing = true;
			try {
				const dataObj = data.data as Record<string, unknown>;

				// Mark as created BEFORE the API call to prevent race conditions
				createdItems.add(itemKey);

				// Show success notification
				successNotification = {
					name: String(dataObj.name || dataObj.title || 'Item'),
					icon: String(dataObj.icon || '✓'),
					color: String(dataObj.color || '#22c55e')
				};

				await onExtractedData(data);

				// Auto-hide notification after 3 seconds
				setTimeout(() => {
					successNotification = null;
				}, 3000);
			} catch (error) {
				console.error('Failed to save:', error);
				// Remove from created items on error so user can retry
				createdItems.delete(itemKey);
				successNotification = null;
				messages = [...messages, {
					role: 'assistant',
					content: 'Sorry, there was an error saving. Please try again.'
				}];
			} finally {
				isProcessing = false;
			}
		} else if (data.action === 'next_step' || data.action === 'complete') {
			onNext();
		} else if (data.action === 'skip') {
			onSkip();
		}
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

</script>

<div class="flex flex-col h-full relative">
	<!-- Success notification - fixed to top-right of screen -->
	{#if successNotification}
		<div
			class="fixed top-4 right-4 z-50 flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg border bg-white"
			style="border-color: {successNotification.color}"
		>
			<span class="text-2xl">{successNotification.icon}</span>
			<div>
				<p class="font-medium" style="color: {successNotification.color}">{successNotification.name}</p>
				<p class="text-sm text-gray-600">Created successfully!</p>
			</div>
		</div>
	{/if}

	<!-- Messages area -->
	<div
		bind:this={messagesContainer}
		class="flex-1 overflow-y-auto p-4 space-y-4"
	>
		{#each messages as message}
			{@const displayContent = stripJsonBlocks(message.content)}
			{@const showBubble = message.isStreaming || displayContent}
			{#if showBubble}
				<div class="flex {message.role === 'user' ? 'justify-end' : 'justify-start'}">
					<div
						class="max-w-[80%] rounded-2xl px-4 py-2 {message.role === 'user'
							? 'bg-primary-600 text-white'
							: 'bg-gray-100 text-gray-900'}"
					>
						{#if message.isStreaming && !message.content}
							<div class="flex space-x-1">
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0ms"></span>
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 150ms"></span>
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 300ms"></span>
							</div>
						{:else if displayContent}
							<div class="whitespace-pre-wrap">{displayContent}</div>
						{/if}
					</div>
				</div>
			{/if}
		{/each}
	</div>

	<!-- Input area -->
	<div class="border-t p-4 bg-white">
		<!-- Quick action buttons -->
		{#if suggestedActions.length > 0 && !isLoading && !isTypingMessage && !isProcessing}
			<div class="flex flex-wrap gap-2 mb-3">
				{#each suggestedActions as action}
					<button
						onclick={() => handleQuickAction(action)}
						class="px-3 py-1.5 text-sm rounded-full border border-primary-300 text-primary-700 hover:bg-primary-50 hover:border-primary-400 transition-colors"
					>
						{action}
					</button>
				{/each}
			</div>
		{/if}

		<div class="flex items-end gap-2">
			<div class="flex-1 relative">
				<textarea
					bind:value={inputValue}
					onkeydown={handleKeydown}
					placeholder="Type your message..."
					disabled={isLoading || isProcessing || isTypingMessage}
					rows="1"
					class="input resize-none pr-12"
				></textarea>
			</div>

			<VoiceInput
				onTranscription={handleTranscription}
				disabled={isLoading || isProcessing || isTypingMessage}
			/>

			<button
				onclick={() => sendMessage(inputValue)}
				disabled={isLoading || isProcessing || isTypingMessage || !inputValue.trim()}
				class="btn-primary p-3"
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

		<!-- Navigation -->
		<div class="flex justify-between items-center mt-4 pt-4 border-t">
			<div>
				{#if showBack && onBack}
					<button onclick={onBack} class="text-sm text-gray-600 hover:text-gray-900">
						← Back
					</button>
				{/if}
			</div>
			<button onclick={onSkip} class="text-sm text-gray-500 hover:text-gray-700">
				Skip this step
			</button>
		</div>
	</div>
</div>
