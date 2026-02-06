<script lang="ts">
	import { tick } from 'svelte';
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
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
	let suggestedActions = $state<Array<{ key: string; label: string }>>([]);

	// Track created items to prevent duplicates (using JSON stringified key)
	const createdItems = new Set<string>();

	// Generate a unique key for an item to detect duplicates
	// For array format, creates a compound key from all items
	function getItemKey(data: ExtractedData): string {
		const dataObj = data.data as Record<string, unknown>;
		const type = data.type; // type is at root level
		
		// Handle identity/goal array format (items array)
		if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
			const itemKeys = (dataObj.items as Array<Record<string, unknown>>).map(item => {
				if (type === 'identity') {
					return `identity:${item.name}`;
				} else if (type === 'goal') {
					return `goal:${item.title}`;
				}
				return JSON.stringify(item);
			});
			return itemKeys.join('|');
		}
		
		// Handle habitStack stacks array format (new format)
		if (type === 'habitStack' && Array.isArray(dataObj.stacks) && dataObj.stacks.length > 0) {
			const stackKeys = (dataObj.stacks as Array<Record<string, unknown>>).map(stack => {
				const habits = (stack.habits || stack.items || []) as Array<Record<string, string>>;
				const habitsKey = habits.map(h => h.habitDescription).join(',');
				return `habitStack:${stack.name}:${stack.triggerCue}:${habitsKey}`;
			});
			return stackKeys.join('|');
		}
		
		// Legacy single item format
		if (type === 'identity') {
			return `identity:${dataObj.name}`;
		} else if (type === 'habitStack') {
			// Legacy format: include habits content in key to detect duplicates with same content
			const habits = (dataObj.habits || dataObj.items || []) as Array<Record<string, string>>;
			const habitsKey = habits.map(h => h.habitDescription).join(',');
			return `habitStack:${dataObj.name}:${dataObj.triggerCue}:${habitsKey}`;
		} else if (type === 'goal') {
			return `goal:${dataObj.title}`;
		}
		return JSON.stringify(data.data);
	}

	// Get default suggested actions - simple and consistent
	function getDefaultSuggestedActions(): Array<{ key: string; label: string }> {
		return [
			{ key: 'yes', label: $t('onboarding.chat.yes') },
			{ key: 'skip', label: $t('onboarding.chat.skip') }
		];
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
				return false;
			}

			// Validate based on type - support both array (items) and single item formats
			if (type === 'identity') {
				// Check for items array format first
				if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
					const firstItem = dataObj.items[0] as Record<string, unknown>;
					const hasName = typeof firstItem.name === 'string' && firstItem.name.length > 0;
					return hasName;
				}
				// Legacy single item format
				const hasName = typeof dataObj.name === 'string' && dataObj.name.length > 0;
				return hasName;
			} else if (type === 'habitStack') {
				// Check for stacks array format first (new format)
				if (Array.isArray(dataObj.stacks) && dataObj.stacks.length > 0) {
					const firstStack = dataObj.stacks[0] as Record<string, unknown>;
					const hasName = typeof firstStack.name === 'string' && firstStack.name.length > 0;
					const hasHabits = Array.isArray(firstStack.habits) && firstStack.habits.length > 0 ||
					                  Array.isArray(firstStack.items) && firstStack.items.length > 0;
					return hasName && hasHabits;
				}
				// Legacy single item format (name/items at root)
				const hasName = typeof dataObj.name === 'string' && dataObj.name.length > 0;
				const hasHabits = Array.isArray(dataObj.items) && dataObj.items.length > 0 ||
				                  Array.isArray(dataObj.habits) && dataObj.habits.length > 0;
				return hasName && hasHabits;
			} else if (type === 'goal') {
				// Check for items array format first
				if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
					const firstItem = dataObj.items[0] as Record<string, unknown>;
					const hasTitle = typeof firstItem.title === 'string' && firstItem.title.length > 0;
					return hasTitle;
				}
				// Legacy single item format
				const hasTitle = typeof dataObj.title === 'string' && dataObj.title.length > 0;
				return hasTitle;
			}

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
			// Map string actions to { key, label } format
			suggestedActions = extractedData.suggestedActions.map(action => ({
				key: action,
				label: action
			}));
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
	async function handleQuickAction(actionKey: string) {
		// Clear suggested actions immediately
		suggestedActions = [];

		// Handle "Skip" action directly without API call
		if (actionKey === 'skip') {
			onSkip();
			return;
		}

		// Handle "Done" type actions - send to API but navigate silently
		if (actionKey === 'done' || actionKey === 'next_step' || actionKey === 'finish') {
			await sendMessage('Yes', false, true);
			return;
		}

		// For "Yes" or any other action, send as regular message (send English "Yes" to API)
		await sendMessage('Yes');
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
				if (isCreateAction(lastExtractedData)) {
					if (isValidExtractedData(lastExtractedData)) {
						extractedDataProcessed = true;
						await handleExtractedData(lastExtractedData);
					}
				}
			}

		} catch {
			// Remove loading placeholder and show error
			messages = messages.filter((_, i) => i !== messages.length - 1);
			messages = [...messages, {
				role: 'assistant',
				content: get(t)('onboarding.chat.errorGeneric'),
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
				return;
			}

			isProcessing = true;
			try {
				const dataObj = data.data as Record<string, unknown>;

				// Mark as created BEFORE the API call to prevent race conditions
				createdItems.add(itemKey);

				// Determine notification details based on format
				let notificationName: string;
				let notificationIcon: string;
				let notificationColor: string;
				
				// Handle habitStack stacks array format
				if (data.type === 'habitStack' && Array.isArray(dataObj.stacks) && dataObj.stacks.length > 0) {
					const stacks = dataObj.stacks as Array<Record<string, unknown>>;
					const count = stacks.length;
					notificationName = count > 1 
						? `${count} habit stacks` 
						: String(stacks[0].name || 'Habit Stack');
					notificationIcon = '🔗';
					notificationColor = '#f59e0b'; // amber
				} else if (Array.isArray(dataObj.items) && dataObj.items.length > 0) {
					// Identity/Goal items array format
					const items = dataObj.items as Array<Record<string, unknown>>;
					const count = items.length;
					const firstItem = items[0];
					notificationName = count > 1 
						? `${count} items` 
						: String(firstItem.name || firstItem.title || 'Item');
					notificationIcon = String(firstItem.icon || '✓');
					notificationColor = String(firstItem.color || '#22c55e');
				} else {
					notificationName = String(dataObj.name || dataObj.title || 'Item');
					notificationIcon = String(dataObj.icon || '✓');
					notificationColor = String(dataObj.color || '#22c55e');
				}

				// Show success notification
				successNotification = {
					name: notificationName,
					icon: notificationIcon,
					color: notificationColor
				};

				await onExtractedData(data);

				// Auto-hide notification after 3 seconds
				setTimeout(() => {
					successNotification = null;
				}, 3000);
			} catch {
				// Remove from created items on error so user can retry
				createdItems.delete(itemKey);
				successNotification = null;
				messages = [...messages, {
					role: 'assistant',
					content: get(t)('onboarding.chat.errorSaving')
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
	<!-- Success notification - fixed to top-right of viewport -->
	{#if successNotification}
		<div
			class="fixed top-4 right-4 z-[100] flex items-center gap-3 px-4 py-3 rounded-xl shadow-xl border-2 bg-warm-paper animate-slide-in"
			style="border-color: {successNotification.color}; box-shadow: 0 4px 20px {successNotification.color}40;"
		>
			<div 
				class="w-10 h-10 rounded-full flex items-center justify-center text-xl"
				style="background-color: {successNotification.color}20"
			>
				{successNotification.icon}
			</div>
			<div>
				<p class="font-semibold text-cocoa-800">{successNotification.name}</p>
				<p class="text-sm font-medium" style="color: {successNotification.color}">✓ {$t('onboarding.chat.createdSuccess')}</p>
			</div>
		</div>
	{/if}

	<!-- Messages area - fixed height with scrollbar -->
	<div
		bind:this={messagesContainer}
		class="flex-1 overflow-y-auto p-3 sm:p-4 space-y-3 sm:space-y-4 min-h-0"
	>
		{#each messages as message}
			{@const displayContent = stripJsonBlocks(message.content)}
			{@const showBubble = message.isStreaming || displayContent}
			{#if showBubble}
				<div class="flex {message.role === 'user' ? 'justify-end' : 'justify-start'}">
					<div
						class="max-w-[85%] sm:max-w-[80%] rounded-2xl px-3 sm:px-4 py-2 text-sm sm:text-base {message.role === 'user'
							? 'bg-primary-600 text-white'
							: 'bg-gray-100 text-cocoa-800'}"
					>
						{#if message.isStreaming && !message.content}
							<div class="flex space-x-1">
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0ms"></span>
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 150ms"></span>
								<span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 300ms"></span>
							</div>
						{:else if displayContent}
							<div class="whitespace-pre-wrap break-words">{displayContent}</div>
						{/if}
					</div>
				</div>
			{/if}
		{/each}
	</div>

	<!-- Input area -->
	<div class="border-t p-3 sm:p-4 bg-warm-paper flex-shrink-0">
		<!-- Quick action buttons - always visible -->
		<div class="flex flex-wrap gap-2 mb-3 min-h-[36px]">
			{#each getDefaultSuggestedActions() as action}
				{@const isDisabled = isLoading || isTypingMessage || isProcessing}
				<button
					onclick={() => handleQuickAction(action.key)}
					disabled={isDisabled}
					class="px-4 py-1.5 text-sm rounded-full border transition-all {isDisabled
						? 'border-primary-100 text-gray-400 bg-warm-cream cursor-not-allowed'
						: 'border-primary-300 text-primary-700 hover:bg-primary-50 hover:border-primary-400'}"
				>
					{action.label}
				</button>
			{/each}
		</div>

		<div class="flex items-end gap-2">
			<div class="flex-1 relative">
				<textarea
					bind:value={inputValue}
					onkeydown={handleKeydown}
					placeholder={$t('onboarding.chat.placeholder')}
					disabled={isLoading || isProcessing || isTypingMessage}
					rows="1"
					class="input resize-none pr-2 text-base"
				></textarea>
			</div>

			<VoiceInput
				onTranscription={handleTranscription}
				disabled={isLoading || isProcessing || isTypingMessage}
			/>

			<button
				onclick={() => sendMessage(inputValue)}
				disabled={isLoading || isProcessing || isTypingMessage || !inputValue.trim()}
				class="btn-primary p-2.5 sm:p-3 flex-shrink-0"
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

		<!-- Navigation -->
		<div class="flex justify-between items-center mt-3 sm:mt-4 pt-3 sm:pt-4 border-t">
			<div>
				{#if showBack && onBack}
					<button onclick={onBack} class="text-sm text-cocoa-600 hover:text-cocoa-800">
						← {$t('onboarding.chat.back')}
					</button>
				{/if}
			</div>
			<button onclick={onSkip} class="text-sm text-cocoa-500 hover:text-cocoa-700">
				{$t('onboarding.chat.skipStep')}
			</button>
		</div>
	</div>
</div>

<style>
	@keyframes slide-in {
		from {
			transform: translateX(100%);
			opacity: 0;
		}
		to {
			transform: translateX(0);
			opacity: 1;
		}
	}
	
	:global(.animate-slide-in) {
		animation: slide-in 0.3s ease-out forwards;
	}
</style>
