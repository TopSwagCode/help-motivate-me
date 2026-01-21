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

	async function handleReactionClick(reaction: ReactionData) {
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
		event.stopPropagation();
		showPicker = true;
	}

	function closePicker() {
		showPicker = false;
	}
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<div class="flex items-center gap-1.5 flex-wrap relative z-10" role="group" aria-label="Reactions">
	<!-- Individual reactions with tooltips -->
	{#each reactions as reaction (reaction.id)}
		<button
			type="button"
			onclick={(e) => { e.stopPropagation(); handleReactionClick(reaction); }}
			disabled={loading || !canRemove(reaction)}
			class="group relative inline-flex items-center justify-center w-7 h-7 rounded-full text-base transition-all duration-150 z-10
				{canRemove(reaction) 
					? 'bg-primary-100 border border-primary-300 hover:bg-primary-200 hover:scale-110 cursor-pointer' 
					: 'bg-gray-100 border border-gray-200 cursor-default'}
				{loading ? 'opacity-50' : ''}
				{compact ? 'w-6 h-6 text-sm' : ''}"
			title="{reaction.emoji} {$t('journal.reactions.from')} {reaction.userDisplayName}{canRemove(reaction) ? ` - ${$t('journal.reactions.clickToRemove')}` : ''}"
		>
			{reaction.emoji}
		</button>
	{/each}

	<!-- Add reaction button -->
	{#if currentUserId}
		<div class="relative z-20">
			<button
				type="button"
				onclick={openPicker}
				disabled={loading}
				class="inline-flex items-center justify-center w-7 h-7 rounded-full bg-white hover:bg-primary-50 
					border-2 border-dashed border-gray-300 hover:border-primary-400 transition-all hover:scale-110
					text-gray-400 hover:text-primary-600
					{compact ? 'w-6 h-6' : ''}"
				title={$t('journal.reactions.addReaction')}
				aria-label={$t('journal.reactions.addReaction')}
			>
				<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
				</svg>
			</button>

			{#if showPicker}
				<div class="absolute bottom-full left-0 mb-2 z-50">
					<EmojiPicker 
						onSelect={handleAddNewReaction}
						onClose={closePicker}
					/>
				</div>
			{/if}
		</div>
	{/if}
</div>
