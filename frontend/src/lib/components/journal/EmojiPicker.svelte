<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		onSelect: (emoji: string) => void;
		onClose: () => void;
	}

	let { onSelect, onClose }: Props = $props();

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

	function handleQuickSelect(emoji: string) {
		onSelect(emoji);
	}

	function handleExtendedSelect(emoji: string) {
		onSelect(emoji);
		showExtended = false;
	}

	function toggleExtended(e: MouseEvent) {
		e.stopPropagation();
		showExtended = !showExtended;
	}

	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('.emoji-picker-container')) {
			onClose();
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			if (showExtended) {
				showExtended = false;
			} else {
				onClose();
			}
		}
	}
</script>

<svelte:window on:keydown={handleKeydown} />

<!-- Backdrop to capture outside clicks -->
<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<div 
	class="fixed inset-0"
	style="z-index: 9998;"
	role="presentation"
	onclick={handleClickOutside}
	onkeydown={handleKeydown}
></div>

<!-- Emoji picker popup -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<!-- svelte-ignore a11y_click_events_have_key_events -->
<div 
	class="emoji-picker-container bg-white rounded-xl shadow-lg border border-gray-200 p-3 relative"
	style="min-width: 260px; z-index: 9999;"
	onclick={(e) => e.stopPropagation()}
>
	{#if !showExtended}
		<!-- Quick emoji bar -->
		<div class="quick-emojis">
			<div class="flex items-center gap-1.5">
				{#each quickEmojis as emoji}
					<button
						type="button"
						onclick={(e) => { e.stopPropagation(); handleQuickSelect(emoji); }}
						class="emoji-btn w-9 h-9 flex items-center justify-center text-xl rounded-lg transition-all cursor-pointer
							hover:bg-primary-100 active:scale-95"
						title={emoji}
					>
						{emoji}
					</button>
				{/each}
				<button
					type="button"
					onclick={toggleExtended}
					class="w-9 h-9 flex items-center justify-center text-gray-400 hover:text-primary-600 
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
		<div class="extended-emojis">
			<!-- Header with back button -->
			<div class="flex items-center gap-2 pb-2 mb-2 border-b border-gray-100">
				<button
					type="button"
					onclick={toggleExtended}
					class="w-7 h-7 flex items-center justify-center text-gray-500 hover:text-gray-700 
						hover:bg-gray-100 rounded-md transition-all cursor-pointer"
					title="Back"
				>
					<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>
				<span class="text-xs font-medium text-gray-500 uppercase tracking-wide">
					{$t('journal.reactions.moreEmojis')}
				</span>
			</div>

			<!-- Category tabs with clear label -->
			<div class="mb-2">
				<div class="text-[10px] font-medium text-gray-400 uppercase tracking-wider mb-1.5 px-0.5">
					{$t('journal.reactions.category')}
				</div>
				<div class="flex gap-1 p-1 bg-gray-50 rounded-lg">
					{#each emojiCategories as category, index}
						<button
							type="button"
							onclick={(e) => { e.stopPropagation(); activeCategory = index; }}
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
			<div class="emoji-grid grid grid-cols-6 gap-1">
				{#each emojiCategories[activeCategory].emojis as emoji}
					<button
						type="button"
						onclick={(e) => { e.stopPropagation(); handleExtendedSelect(emoji); }}
						class="emoji-btn w-9 h-9 flex items-center justify-center text-xl rounded-lg transition-all cursor-pointer
							hover:bg-primary-100 active:scale-95"
					>
						{emoji}
					</button>
				{/each}
			</div>
		</div>
	{/if}
</div>

<style>
	.emoji-picker-container {
		animation: fadeInScale 150ms ease-out;
	}

	.quick-emojis {
		animation: fadeIn 150ms ease-out;
	}

	.extended-emojis {
		animation: slideIn 200ms ease-out;
	}

	.emoji-btn:hover {
		transform: scale(1.15);
	}

	.emoji-grid {
		/* Fixed height to prevent scrollbar flashing */
		height: 80px;
		overflow: hidden;
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

	@keyframes slideIn {
		from {
			opacity: 0;
			transform: translateX(-8px);
		}
		to {
			opacity: 1;
			transform: translateX(0);
		}
	}
</style>
			transform: scale(1);
		}
	}
</style>
