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
	class="fixed inset-0 z-40"
	role="presentation"
	onclick={handleClickOutside}
	onkeydown={handleKeydown}
></div>

<!-- Emoji picker popup -->
<div class="emoji-picker-container bg-white rounded-xl shadow-lg border border-gray-200 p-2 z-50" style="min-width: 240px;">
	<!-- Quick emoji bar -->
	<div class="flex items-center gap-1 pb-2 border-b border-gray-100">
		{#each quickEmojis as emoji}
			<button
				type="button"
				onclick={() => handleQuickSelect(emoji)}
				class="w-8 h-8 flex items-center justify-center text-lg hover:bg-gray-100 rounded-lg transition-colors"
				title={emoji}
			>
				{emoji}
			</button>
		{/each}
		<button
			type="button"
			onclick={() => showExtended = !showExtended}
			class="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100 rounded-lg transition-colors ml-auto"
			title={$t('journal.reactions.moreEmojis')}
		>
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
			</svg>
		</button>
	</div>

	<!-- Extended emoji picker -->
	{#if showExtended}
		<div class="pt-2">
			<!-- Category tabs -->
			<div class="flex gap-1 pb-2 border-b border-gray-100">
				{#each emojiCategories as category, index}
					<button
						type="button"
						onclick={() => activeCategory = index}
						class="w-7 h-7 flex items-center justify-center text-sm rounded-md transition-colors {activeCategory === index ? 'bg-primary-100 text-primary-700' : 'hover:bg-gray-100'}"
					>
						{category.label}
					</button>
				{/each}
			</div>

			<!-- Emoji grid -->
			<div class="grid grid-cols-6 gap-1 pt-2 max-h-32 overflow-y-auto">
				{#each emojiCategories[activeCategory].emojis as emoji}
					<button
						type="button"
						onclick={() => handleExtendedSelect(emoji)}
						class="w-8 h-8 flex items-center justify-center text-lg hover:bg-gray-100 rounded-lg transition-colors"
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
</style>
