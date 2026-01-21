<script lang="ts">
	import { t } from 'svelte-i18n';
	import { onMount, onDestroy } from 'svelte';
	import { closeEmojiPicker } from '$lib/stores/emojiPickerStore';

	interface Props {
		onSelect: (emoji: string) => void;
		onClose: () => void;
		anchorElement?: HTMLElement | null;
	}

	let { onSelect, onClose, anchorElement = null }: Props = $props();

	// Quick reactions - most common emojis
	const quickEmojis = ['❤️', '👍', '🔥', '👏', '💪', '🎉'];

	// Extended emoji categories
	const emojiCategories = [
		{
			name: 'smileys',
			label: '😀',
			emojis: ['😀', '😊', '😄', '🥰', '😍', '🤩', '😎', '🥳', '😇', '🤗', '😌', '😏']
		},
		{
			name: 'gestures',
			label: '👋',
			emojis: ['👍', '👎', '👏', '🙌', '🤝', '💪', '✊', '🤞', '🤟', '👋', '🙏', '✨']
		},
		{
			name: 'hearts',
			label: '❤️',
			emojis: ['❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍', '💖', '💗', '💓', '💕']
		},
		{
			name: 'celebration',
			label: '🎉',
			emojis: ['🎉', '🎊', '🎈', '🎁', '🏆', '🥇', '🏅', '⭐', '🌟', '💫', '🔥', '💥']
		},
		{
			name: 'nature',
			label: '🌱',
			emojis: ['🌱', '🌿', '🍀', '🌸', '🌺', '🌻', '🌈', '☀️', '🌙', '⚡', '❄️', '🔥']
		},
		{
			name: 'misc',
			label: '💡',
			emojis: ['💡', '💎', '🎯', '🚀', '💫', '✅', '💯', '🔑', '📍', '🎵', '📸', '💼']
		}
	];

	let showExtended = $state(false);
	let activeCategory = $state(0);
	let isMobile = $state(false);
	let pickerRef: HTMLDivElement | undefined = $state(undefined);
	
	// Position state
	let positionStyle = $state('');
	let openDirection = $state<'up' | 'down'>('up'); // Whether picker opens upward or downward

	// Picker dimensions
	const PICKER_WIDTH = 300;
	const QUICK_HEIGHT = 56;
	const EXTENDED_HEIGHT = 220;

	// Check if we're on mobile
	function checkMobile() {
		isMobile = window.innerWidth < 640;
	}

	// Calculate best position for the picker
	function calculatePosition() {
		if (isMobile || !anchorElement) {
			positionStyle = '';
			return;
		}

		const rect = anchorElement.getBoundingClientRect();
		const padding = 8;
		const viewportWidth = window.innerWidth;
		const viewportHeight = window.innerHeight;
		
		// Current height based on state
		const currentHeight = showExtended ? EXTENDED_HEIGHT : QUICK_HEIGHT;

		// Calculate horizontal position - try to align with button, but keep in viewport
		let left = rect.left;
		if (left + PICKER_WIDTH > viewportWidth - padding) {
			left = viewportWidth - PICKER_WIDTH - padding;
		}
		if (left < padding) {
			left = padding;
		}

		// Calculate vertical position
		// Check space above and below the anchor
		const spaceAbove = rect.top;
		const spaceBelow = viewportHeight - rect.bottom;
		
		// We need enough space for the EXTENDED height (since it might expand)
		const neededSpace = EXTENDED_HEIGHT + padding;

		let top: number;
		
		if (spaceAbove >= neededSpace) {
			// Position above - anchor to bottom of picker
			openDirection = 'up';
			top = rect.top - currentHeight - padding;
		} else if (spaceBelow >= neededSpace) {
			// Position below
			openDirection = 'down';
			top = rect.bottom + padding;
		} else if (spaceAbove > spaceBelow) {
			// More space above, but not enough - position at top of viewport
			openDirection = 'up';
			top = padding;
		} else {
			// More space below - position below anchor
			openDirection = 'down';
			top = rect.bottom + padding;
			// Clamp to viewport
			if (top + currentHeight > viewportHeight - padding) {
				top = viewportHeight - currentHeight - padding;
			}
		}

		positionStyle = `left: ${left}px; top: ${top}px;`;
	}

	onMount(() => {
		checkMobile();
		calculatePosition();
		window.addEventListener('resize', checkMobile);
		window.addEventListener('resize', calculatePosition);
		window.addEventListener('scroll', calculatePosition, true);
	});

	onDestroy(() => {
		if (typeof window !== 'undefined') {
			window.removeEventListener('resize', checkMobile);
			window.removeEventListener('resize', calculatePosition);
			window.removeEventListener('scroll', calculatePosition, true);
		}
	});

	// Recalculate position when extended state changes
	$effect(() => {
		if (showExtended !== undefined && !isMobile) {
			// Small delay to let the transition start
			requestAnimationFrame(calculatePosition);
		}
	});

	function handleQuickSelect(e: MouseEvent | TouchEvent, emoji: string) {
		e.preventDefault();
		e.stopPropagation();
		onSelect(emoji);
	}

	function handleExtendedSelect(e: MouseEvent | TouchEvent, emoji: string) {
		e.preventDefault();
		e.stopPropagation();
		onSelect(emoji);
		showExtended = false;
	}

	function toggleExtended(e: MouseEvent | TouchEvent) {
		e.preventDefault();
		e.stopPropagation();
		showExtended = !showExtended;
	}

	function handleBackdropClick(event: MouseEvent | TouchEvent) {
		event.preventDefault();
		event.stopPropagation();
		closeEmojiPicker();
		onClose();
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			if (showExtended) {
				showExtended = false;
			} else {
				closeEmojiPicker();
				onClose();
			}
		}
	}

	function handleCloseButton(e: MouseEvent | TouchEvent) {
		e.preventDefault();
		e.stopPropagation();
		closeEmojiPicker();
		onClose();
	}

	function selectCategory(e: MouseEvent | TouchEvent, index: number) {
		e.preventDefault();
		e.stopPropagation();
		activeCategory = index;
	}

	// Computed height for animation
	let currentHeight = $derived(showExtended ? EXTENDED_HEIGHT : QUICK_HEIGHT);
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isMobile}
	<!-- Mobile: Full-screen modal from bottom -->
	<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
	<!-- svelte-ignore a11y_interactive_supports_focus -->
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<div 
		class="fixed inset-0 bg-black/50 flex items-end justify-center"
		style="z-index: 10000;"
		role="dialog"
		aria-modal="true"
		onclick={handleBackdropClick}
		ontouchend={handleBackdropClick}
	>
		<!-- svelte-ignore a11y_click_events_have_key_events -->
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div 
			class="emoji-picker-content bg-white rounded-t-2xl w-full max-h-[70vh] overflow-hidden animate-slide-up"
			onclick={(e) => e.stopPropagation()}
			ontouchstart={(e) => e.stopPropagation()}
		>
			<!-- Handle bar -->
			<div class="flex justify-center py-3">
				<div class="w-10 h-1 bg-gray-300 rounded-full"></div>
			</div>

			<!-- Header -->
			<div class="flex items-center justify-between px-4 pb-3 border-b border-gray-100">
				<h3 class="text-lg font-semibold text-gray-900">
					{$t('journal.reactions.addReaction')}
				</h3>
				<button
					type="button"
					onclick={handleCloseButton}
					ontouchend={handleCloseButton}
					class="w-10 h-10 flex items-center justify-center text-gray-400 hover:text-gray-600 
						hover:bg-gray-100 rounded-full transition-colors active:bg-gray-200"
					aria-label="Close"
				>
					<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<div class="p-4 overflow-y-auto max-h-[calc(70vh-100px)]">
				{#if !showExtended}
					<!-- Quick emoji section -->
					<div class="mb-4">
						<div class="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
							{$t('journal.reactions.quickReactions')}
						</div>
						<div class="flex justify-around">
							{#each quickEmojis as emoji}
								<button
									type="button"
									onclick={(e) => handleQuickSelect(e, emoji)}
									ontouchend={(e) => handleQuickSelect(e, emoji)}
									class="w-14 h-14 flex items-center justify-center text-3xl rounded-xl transition-all
										hover:bg-primary-100 active:scale-95 active:bg-primary-100"
								>
									{emoji}
								</button>
							{/each}
						</div>
					</div>

					<button
						type="button"
						onclick={toggleExtended}
						ontouchend={toggleExtended}
						class="w-full py-3 text-center text-primary-600 font-medium 
							hover:bg-primary-50 active:bg-primary-100 rounded-xl transition-colors"
					>
						{$t('journal.reactions.moreEmojis')} →
					</button>
				{:else}
					<!-- Extended section -->
					<div class="mb-4">
						<button
							type="button"
							onclick={toggleExtended}
							ontouchend={toggleExtended}
							class="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-4 active:text-gray-900"
						>
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
							</svg>
							<span class="font-medium">{$t('journal.reactions.hideMore')}</span>
						</button>
					</div>

					<!-- Category tabs -->
					<div class="mb-4">
						<div class="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
							{$t('journal.reactions.category')}
						</div>
						<div class="flex gap-2 p-1.5 bg-gray-100 rounded-xl overflow-x-auto overflow-y-hidden">
							{#each emojiCategories as category, index}
								<button
									type="button"
									onclick={(e) => selectCategory(e, index)}
									ontouchend={(e) => selectCategory(e, index)}
									class="flex-1 min-w-[44px] h-11 flex items-center justify-center text-xl rounded-lg transition-all
										{activeCategory === index 
											? 'bg-white shadow-sm' 
											: 'hover:bg-white/50 active:bg-white/70'}"
								>
									{category.label}
								</button>
							{/each}
						</div>
					</div>

					<!-- Emoji grid -->
					<div class="grid grid-cols-6 gap-2">
						{#each emojiCategories[activeCategory].emojis as emoji}
							<button
								type="button"
								onclick={(e) => handleExtendedSelect(e, emoji)}
								ontouchend={(e) => handleExtendedSelect(e, emoji)}
								class="w-12 h-12 flex items-center justify-center text-2xl rounded-xl transition-all
									hover:bg-primary-100 active:scale-95 active:bg-primary-100"
							>
								{emoji}
							</button>
						{/each}
					</div>
				{/if}
			</div>
		</div>
	</div>
{:else}
	<!-- Desktop: Fixed position picker with smart positioning -->
	<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
	<div 
		class="fixed inset-0"
		style="z-index: 9998;"
		role="presentation"
		onclick={handleBackdropClick}
		ontouchstart={handleBackdropClick}
	></div>

	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<div 
		bind:this={pickerRef}
		class="emoji-picker-content fixed bg-white rounded-xl shadow-xl border border-gray-200 overflow-hidden"
		style="width: {PICKER_WIDTH}px; height: {currentHeight}px; z-index: 9999; {positionStyle}"
		onclick={(e) => e.stopPropagation()}
		ontouchstart={(e) => e.stopPropagation()}
	>
		{#if !showExtended}
			<!-- Quick emoji bar -->
			<div class="quick-emojis h-full flex items-center justify-center p-2">
				<div class="flex items-center gap-1.5">
					{#each quickEmojis as emoji}
						<button
							type="button"
							onclick={(e) => handleQuickSelect(e, emoji)}
							ontouchend={(e) => handleQuickSelect(e, emoji)}
							class="emoji-btn w-10 h-10 flex items-center justify-center text-xl rounded-lg transition-all cursor-pointer
								hover:bg-primary-100 active:scale-95 active:bg-primary-100"
							title={emoji}
						>
							{emoji}
						</button>
					{/each}
					<button
						type="button"
						onclick={toggleExtended}
						ontouchend={toggleExtended}
						class="w-10 h-10 flex items-center justify-center text-gray-400 hover:text-primary-600 
							hover:bg-primary-50 rounded-lg transition-all ml-1 cursor-pointer border border-dashed border-gray-300 hover:border-primary-400"
						title={$t('journal.reactions.moreEmojis')}
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
						</svg>
					</button>
				</div>
			</div>
		{:else}
			<!-- Extended emoji picker -->
			<div class="extended-emojis p-3 h-full flex flex-col">
				<!-- Header with back button -->
				<div class="flex items-center gap-2 pb-2 mb-2 border-b border-gray-100 flex-shrink-0">
					<button
						type="button"
						onclick={toggleExtended}
						ontouchend={toggleExtended}
						class="w-7 h-7 flex items-center justify-center text-gray-500 hover:text-gray-700 
							hover:bg-gray-100 rounded-md transition-all cursor-pointer"
						title={$t('journal.reactions.hideMore')}
					>
						<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
						</svg>
					</button>
					<span class="text-xs font-medium text-gray-500 uppercase tracking-wide">
						{$t('journal.reactions.moreEmojis')}
					</span>
				</div>

				<!-- Category tabs -->
				<div class="mb-2 flex-shrink-0">
					<div class="text-[10px] font-medium text-gray-400 uppercase tracking-wider mb-1.5 px-0.5">
						{$t('journal.reactions.category')}
					</div>
					<div class="flex gap-1 p-1 bg-gray-50 rounded-lg">
						{#each emojiCategories as category, index}
							<button
								type="button"
								onclick={(e) => selectCategory(e, index)}
								ontouchend={(e) => selectCategory(e, index)}
								class="category-btn flex-1 h-8 flex items-center justify-center text-base rounded-md transition-all cursor-pointer
									{activeCategory === index 
										? 'bg-white shadow-sm border border-gray-200' 
										: 'hover:bg-white/50'}"
								title={category.name}
							>
								{category.label}
							</button>
						{/each}
					</div>
				</div>

				<!-- Emoji grid -->
				<div class="emoji-grid grid grid-cols-6 gap-1 flex-1 overflow-hidden">
					{#each emojiCategories[activeCategory].emojis as emoji}
						<button
							type="button"
							onclick={(e) => handleExtendedSelect(e, emoji)}
							ontouchend={(e) => handleExtendedSelect(e, emoji)}
							class="emoji-btn w-10 h-10 flex items-center justify-center text-xl rounded-lg transition-all cursor-pointer
								hover:bg-primary-100 active:scale-95 active:bg-primary-100"
						>
							{emoji}
						</button>
					{/each}
				</div>
			</div>
		{/if}
	</div>
{/if}

<style>
	.emoji-picker-content {
		animation: fadeInScale 150ms ease-out;
		transition: height 200ms ease-out;
	}

	.quick-emojis {
		animation: fadeIn 100ms ease-out;
	}

	.extended-emojis {
		animation: fadeIn 150ms ease-out;
	}

	.emoji-btn:hover {
		transform: scale(1.1);
	}

	@keyframes fadeInScale {
		from {
			opacity: 0;
			transform: scale(0.95);
		}
		to {
			opacity: 1;
			transform: scale(1);
		}
	}

	@keyframes fadeIn {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}

	@keyframes animate-slide-up {
		from {
			transform: translateY(100%);
		}
		to {
			transform: translateY(0);
		}
	}

	.animate-slide-up {
		animation: animate-slide-up 0.3s ease-out;
	}
</style>
