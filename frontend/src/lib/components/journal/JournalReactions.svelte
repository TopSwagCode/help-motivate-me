<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { JournalReaction } from '$lib/types';
	import type { BuddyJournalReaction } from '$lib/types/buddy';
	import EmojiPicker from './EmojiPicker.svelte';
	import { activeEmojiPickerId, openEmojiPicker, closeEmojiPicker } from '$lib/stores/emojiPickerStore';

	// Generic reaction type that works for both
	type ReactionData = JournalReaction | BuddyJournalReaction;

	interface Props {
		reactions: ReactionData[];
		currentUserId: string | null;
		onAddReaction: (emoji: string) => Promise<void>;
		onRemoveReaction: (reactionId: string) => Promise<void>;
		compact?: boolean;
		entryId: string; // Required to track which picker is open
	}

	let {
		reactions,
		currentUserId,
		onAddReaction,
		onRemoveReaction,
		compact = false,
		entryId
	}: Props = $props();

	let loading = $state(false);
	let hoveredReactionId = $state<string | null>(null);
	let addButtonRef: HTMLButtonElement | undefined = $state(undefined);

	// Derived state - check if this entry's picker is open
	let showPicker = $derived($activeEmojiPickerId === entryId);

	// Check if the current user can remove a reaction (only their own)
	function canRemove(reaction: ReactionData): boolean {
		return currentUserId === reaction.userId;
	}

	// Check if this is the current user's reaction
	function isOwnReaction(reaction: ReactionData): boolean {
		return currentUserId === reaction.userId;
	}

	async function handleReactionClick(event: MouseEvent | TouchEvent, reaction: ReactionData) {
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
		
		closeEmojiPicker();
		
		loading = true;
		try {
			await onAddReaction(emoji);
		} finally {
			loading = false;
		}
	}

	function handleOpenPicker(event: MouseEvent | TouchEvent) {
		event.preventDefault();
		event.stopPropagation();
		// This will close any other open picker and open this one
		openEmojiPicker(entryId);
	}

	function handleClosePicker() {
		closeEmojiPicker();
	}

	function showTooltip(reactionId: string) {
		hoveredReactionId = reactionId;
	}

	function hideTooltip() {
		hoveredReactionId = null;
	}
</script>

<div class="reactions-container flex items-center gap-2 flex-wrap" role="group" aria-label="Reactions">
	<!-- Individual reactions with custom tooltips -->
	{#each reactions as reaction (reaction.id)}
		<div class="reaction-wrapper relative">
			<button
				type="button"
				onclick={(e) => handleReactionClick(e, reaction)}
				ontouchend={(e) => handleReactionClick(e, reaction)}
				onmouseenter={() => showTooltip(reaction.id)}
				onmouseleave={hideTooltip}
				onfocus={() => showTooltip(reaction.id)}
				onblur={hideTooltip}
				disabled={loading}
				class="reaction-btn inline-flex items-center gap-1.5 rounded-full text-lg transition-all duration-200
					{isOwnReaction(reaction) 
						? 'bg-primary-100 border-2 border-primary-400 hover:bg-primary-200 hover:border-primary-500 shadow-sm' 
						: 'bg-white border-2 border-gray-200 hover:bg-gray-50 hover:border-gray-300'}
					{loading ? 'opacity-50' : ''}
					{compact ? 'px-2 py-1' : 'px-3 py-1.5'}"
			>
				<span class="reaction-emoji text-xl">{reaction.emoji}</span>
				{#if !compact}
					<span class="reaction-name text-xs font-medium truncate max-w-[80px]
						{isOwnReaction(reaction) ? 'text-primary-700' : 'text-gray-600'}">
						{isOwnReaction(reaction) ? $t('journal.reactions.you') : reaction.userDisplayName}
					</span>
				{/if}
			</button>

			<!-- Custom tooltip -->
			{#if hoveredReactionId === reaction.id}
				<div class="tooltip absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-3 py-2 
					bg-gray-900 text-white text-sm rounded-lg shadow-lg whitespace-nowrap z-50
					animate-fade-in">
					<div class="font-medium">{reaction.userDisplayName}</div>
					<div class="text-gray-300 text-xs mt-0.5">
						{#if isOwnReaction(reaction)}
							{$t('journal.reactions.clickToRemove')}
						{:else}
							{$t('journal.reactions.reacted')} {reaction.emoji}
						{/if}
					</div>
					<!-- Tooltip arrow -->
					<div class="absolute top-full left-1/2 -translate-x-1/2 -mt-px">
						<div class="border-8 border-transparent border-t-gray-900"></div>
					</div>
				</div>
			{/if}
		</div>
	{/each}

	<!-- Add reaction button -->
	{#if currentUserId}
		<div class="relative">
			<button
				bind:this={addButtonRef}
				type="button"
				onclick={handleOpenPicker}
				ontouchend={handleOpenPicker}
				disabled={loading}
				class="add-reaction-btn inline-flex items-center gap-1.5 rounded-full bg-white
					border-2 border-dashed border-gray-300 hover:border-primary-400 hover:bg-primary-50
					text-gray-500 hover:text-primary-600 transition-all duration-200 
					active:bg-primary-100 active:border-primary-500
					{compact ? 'px-2 py-1' : 'px-3 py-1.5'}"
				title={$t('journal.reactions.addReaction')}
				aria-label={$t('journal.reactions.addReaction')}
			>
				<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
				</svg>
				{#if !compact}
					<span class="text-xs font-medium">{$t('journal.reactions.react')}</span>
				{/if}
			</button>

			{#if showPicker}
				<EmojiPicker 
					onSelect={handleAddNewReaction}
					onClose={handleClosePicker}
					anchorElement={addButtonRef}
				/>
			{/if}
		</div>
	{/if}
</div>

<style>
	.reactions-container {
		position: relative;
		z-index: 5;
	}

	.reaction-wrapper {
		position: relative;
	}

	.reaction-btn,
	.add-reaction-btn {
		cursor: pointer;
		position: relative;
		z-index: 10;
		pointer-events: auto !important;
	}

	.reaction-btn:hover {
		transform: translateY(-2px);
	}

	.reaction-btn:active {
		transform: translateY(0) scale(0.98);
	}

	.reaction-btn:hover .reaction-emoji {
		transform: scale(1.15);
	}

	.reaction-emoji {
		transition: transform 0.2s ease;
		display: inline-block;
	}

	.add-reaction-btn:hover {
		transform: translateY(-2px);
	}

	.add-reaction-btn:hover svg {
		transform: scale(1.1);
	}

	.add-reaction-btn svg {
		transition: transform 0.2s ease;
	}

	.tooltip {
		pointer-events: none;
	}

	@keyframes fade-in {
		from {
			opacity: 0;
			transform: translate(-50%, 4px);
		}
		to {
			opacity: 1;
			transform: translate(-50%, 0);
		}
	}

	.animate-fade-in {
		animation: fade-in 0.15s ease-out;
	}

	.reaction-btn:disabled {
		cursor: default;
	}
</style>
