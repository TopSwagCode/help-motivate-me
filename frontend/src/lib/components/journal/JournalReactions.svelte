<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { JournalReaction } from '$lib/types';
	import type { BuddyJournalReaction } from '$lib/types/buddy';
	import EmojiPicker from './EmojiPicker.svelte';

	// Generic reaction type that works for both
	type ReactionData = JournalReaction | BuddyJournalReaction;

	interface Props {
		reactions: ReactionData[];
		currentUserId: string | null;
		onAddReaction: (emoji: string) => Promise<void>;
		onRemoveReaction: (reactionId: string) => Promise<void>;
		compact?: boolean;
	}

	let {
		reactions,
		currentUserId,
		onAddReaction,
		onRemoveReaction,
		compact = false
	}: Props = $props();

	let showPicker = $state(false);
	let loading = $state(false);

	// Check if the current user can remove a reaction (only their own)
	function canRemove(reaction: ReactionData): boolean {
		return currentUserId === reaction.userId;
	}

	async function handleReactionClick(event: MouseEvent, reaction: ReactionData) {
		event.preventDefault();
		event.stopPropagation();
		
		if (loading || !currentUserId) return;
		
		// Only allow removing your own reactions
		if (canRemove(reaction)) {
			loading = true;
			try {
				await onRemoveReaction(reaction.id);
			} finally {
				loading = false;
			}
		}
	}

	async function handleAddNewReaction(emoji: string) {
		if (loading || !currentUserId) return;
		
		showPicker = false;
		
		loading = true;
		try {
			await onAddReaction(emoji);
		} finally {
			loading = false;
		}
	}

	function openPicker(event: MouseEvent) {
		event.preventDefault();
		event.stopPropagation();
		showPicker = true;
	}

	function closePicker() {
		showPicker = false;
	}
</script>

<div class="reactions-container flex items-center gap-2 flex-wrap" role="group" aria-label="Reactions">
	<!-- Individual reactions with tooltips -->
	{#each reactions as reaction (reaction.id)}
		<button
			type="button"
			onclick={(e) => handleReactionClick(e, reaction)}
			disabled={loading}
			class="reaction-btn inline-flex items-center justify-center rounded-full text-lg transition-all duration-200
				{canRemove(reaction) 
					? 'bg-primary-100 border-2 border-primary-300 hover:bg-primary-200 hover:border-primary-400 hover:shadow-md active:scale-95' 
					: 'bg-gray-100 border-2 border-gray-200 hover:bg-gray-200 hover:border-gray-300'}
				{loading ? 'opacity-50' : ''}
				{compact ? 'w-7 h-7 text-base' : 'w-9 h-9'}"
			title="{reaction.emoji} {$t('journal.reactions.from')} {reaction.userDisplayName}{canRemove(reaction) ? ` - ${$t('journal.reactions.clickToRemove')}` : ''}"
		>
			<span class="reaction-emoji">{reaction.emoji}</span>
		</button>
	{/each}

	<!-- Add reaction button -->
	{#if currentUserId}
		<div class="relative">
			<button
				type="button"
				onclick={openPicker}
				disabled={loading}
				class="add-reaction-btn inline-flex items-center justify-center rounded-full bg-white
					border-2 border-dashed border-gray-300 hover:border-primary-400 hover:bg-primary-50
					text-gray-400 hover:text-primary-600 transition-all duration-200 hover:shadow-md active:scale-95
					{compact ? 'w-7 h-7' : 'w-9 h-9'}"
				title={$t('journal.reactions.addReaction')}
				aria-label={$t('journal.reactions.addReaction')}
			>
				<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
				</svg>
			</button>

			{#if showPicker}
				<div class="absolute bottom-full left-0 mb-2" style="z-index: 10000;">
					<EmojiPicker 
						onSelect={handleAddNewReaction}
						onClose={closePicker}
					/>
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.reactions-container {
		position: relative;
		z-index: 5;
	}

	.reaction-btn,
	.add-reaction-btn {
		cursor: pointer;
		position: relative;
		z-index: 10;
		pointer-events: auto !important;
	}

	.reaction-btn:hover .reaction-emoji,
	.add-reaction-btn:hover svg {
		transform: scale(1.2);
	}

	.reaction-emoji {
		transition: transform 0.2s ease;
		display: inline-block;
	}

	.add-reaction-btn svg {
		transition: transform 0.2s ease;
	}

	.reaction-btn:disabled {
		cursor: default;
	}
</style>
