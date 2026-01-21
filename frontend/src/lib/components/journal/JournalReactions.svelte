<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { JournalReaction, JournalReactionSummary } from '$lib/types';
	import EmojiPicker from './EmojiPicker.svelte';

	interface Props {
		reactions: JournalReaction[];
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
	let pickerAnchor = $state<HTMLElement | null>(null);
	let loading = $state(false);

	// Group reactions by emoji and calculate summaries
	const reactionSummaries = $derived.by(() => {
		const grouped = new Map<string, JournalReaction[]>();
		
		for (const reaction of reactions) {
			const existing = grouped.get(reaction.emoji) || [];
			existing.push(reaction);
			grouped.set(reaction.emoji, existing);
		}

		const summaries: JournalReactionSummary[] = Array.from(grouped.entries()).map(([emoji, reactionList]) => ({
			emoji,
			count: reactionList.length,
			userIds: reactionList.map(r => r.userId),
			hasReacted: currentUserId ? reactionList.some(r => r.userId === currentUserId) : false
		}));
		
		return summaries;
	});

	// Find current user's reaction for a specific emoji
	function findUserReaction(emoji: string): JournalReaction | undefined {
		if (!currentUserId) return undefined;
		return reactions.find(r => r.emoji === emoji && r.userId === currentUserId);
	}

	async function handleReactionClick(summary: JournalReactionSummary) {
		if (loading || !currentUserId) return;
		
		loading = true;
		try {
			if (summary.hasReacted) {
				// Remove reaction
				const userReaction = findUserReaction(summary.emoji);
				if (userReaction) {
					await onRemoveReaction(userReaction.id);
				}
			} else {
				// Add reaction
				await onAddReaction(summary.emoji);
			}
		} finally {
			loading = false;
		}
	}

	async function handleAddNewReaction(emoji: string) {
		if (loading || !currentUserId) return;
		
		showPicker = false;
		
		// Check if user already reacted with this emoji
		const existingReaction = findUserReaction(emoji);
		if (existingReaction) {
			// Already reacted with this emoji, do nothing
			return;
		}
		
		loading = true;
		try {
			await onAddReaction(emoji);
		} finally {
			loading = false;
		}
	}

	function openPicker(event: MouseEvent) {
		event.stopPropagation();
		pickerAnchor = event.currentTarget as HTMLElement;
		showPicker = true;
	}

	function closePicker() {
		showPicker = false;
	}

	// Get tooltip text for reaction
	function getReactionTooltip(summary: JournalReactionSummary): string {
		const names = reactions
			.filter(r => r.emoji === summary.emoji)
			.map(r => r.userDisplayName)
			.slice(0, 3);
		
		if (names.length === 0) return '';
		if (names.length === summary.count) return names.join(', ');
		return `${names.join(', ')} +${summary.count - names.length}`;
	}
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<div class="flex items-center gap-2 flex-wrap" role="group" aria-label="Reactions" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
	<!-- Existing reactions first -->
	{#each reactionSummaries as summary (summary.emoji)}
		<button
			type="button"
			onclick={() => handleReactionClick(summary)}
			disabled={loading || !currentUserId}
			class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-sm transition-all duration-150 shadow-sm
				{summary.hasReacted 
					? 'bg-primary-100 border border-primary-400 text-primary-700 shadow-primary-100' 
					: 'bg-white border border-gray-200 hover:bg-gray-50 hover:border-gray-300 text-gray-700'}
				{loading ? 'opacity-50' : ''}
				{compact ? 'text-xs px-2 py-0.5' : ''}"
			title={getReactionTooltip(summary)}
		>
			<span class={compact ? 'text-sm' : 'text-lg'}>{summary.emoji}</span>
			<span class="font-semibold">{summary.count}</span>
		</button>
	{/each}

	<!-- Add reaction button - Clear and visible -->
	{#if currentUserId}
		<div class="relative">
			<button
				type="button"
				onclick={openPicker}
				disabled={loading}
				class="inline-flex items-center justify-center w-8 h-8 rounded-full bg-white hover:bg-primary-50 
					border-2 border-gray-300 hover:border-primary-400 transition-all shadow-sm hover:shadow
					text-gray-500 hover:text-primary-600"
				title={$t('journal.reactions.addReaction')}
				aria-label={$t('journal.reactions.addReaction')}
			>
				<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
				</svg>
			</button>

			{#if showPicker && pickerAnchor}
				<div class="absolute bottom-full left-0 mb-2" style="z-index: 50;">
					<EmojiPicker 
						onSelect={handleAddNewReaction}
						onClose={closePicker}
					/>
				</div>
			{/if}
		</div>
	{/if}
</div>
