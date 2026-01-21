<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import type { JournalEntry, JournalImage, JournalReaction } from '$lib/types';
	import type { BuddyJournalEntry, BuddyJournalImage, BuddyJournalReaction } from '$lib/types/buddy';
	import JournalReactions from './JournalReactions.svelte';
	import type { JournalFilter } from '$lib/api/journal';

	// Unified type to accept both JournalEntry and BuddyJournalEntry
	type EntryData = JournalEntry | BuddyJournalEntry;
	type ImageData = JournalImage | BuddyJournalImage;
	type ReactionData = JournalReaction | BuddyJournalReaction;

	interface Props {
		entries: EntryData[];
		currentUserId?: string | null;
		// Mode: 'own' = viewing own journal, 'buddy' = viewing buddy's journal, 'feed' = combined feed view
		mode?: 'own' | 'buddy' | 'feed';
		// Current filter (for feed mode)
		activeFilter?: JournalFilter;
		// Buddy-specific props
		buddyDisplayName?: string;
		// Event handlers
		onCreateEntry?: () => void;
		onEditEntry?: (entry: EntryData) => void;
		onOpenLightbox?: (images: ImageData[], index: number, event: Event) => void;
		onFilterChange?: (filter: JournalFilter) => void;
		onAddReaction?: (entryId: string, emoji: string) => Promise<void>;
		onRemoveReaction?: (entryId: string, reactionId: string) => Promise<void>;
	}

	let {
		entries,
		currentUserId = null,
		mode = 'own',
		activeFilter = 'all',
		buddyDisplayName = 'User',
		onCreateEntry,
		onEditEntry,
		onOpenLightbox,
		onFilterChange,
		onAddReaction,
		onRemoveReaction
	}: Props = $props();

	// Type guard to check if entry has linking fields (JournalEntry)
	function hasLinkingFields(entry: EntryData): entry is JournalEntry {
		return 'habitStackId' in entry;
	}

	// Get reactions from entry (works for both JournalEntry and BuddyJournalEntry)
	function getReactions(entry: EntryData): ReactionData[] {
		if ('reactions' in entry && Array.isArray(entry.reactions)) {
			return entry.reactions;
		}
		return [];
	}

	function formatRelativeDate(dateStr: string): string {
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		const entryDate = new Date(dateStr + 'T12:00:00');
		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const yesterday = new Date(today);
		yesterday.setDate(yesterday.getDate() - 1);
		
		const entryDateNormalized = new Date(entryDate);
		entryDateNormalized.setHours(0, 0, 0, 0);
		
		const diffDays = Math.round((today.getTime() - entryDateNormalized.getTime()) / (1000 * 60 * 60 * 24));
		
		if (diffDays === 0) {
			return get(t)('today.dates.today');
		} else if (diffDays === 1) {
			return get(t)('today.dates.yesterday');
		} else if (diffDays < 7) {
			return entryDate.toLocaleDateString(currentLocale, { weekday: 'long' });
		} else {
			return entryDate.toLocaleDateString(currentLocale, { 
				month: 'short', 
				day: 'numeric'
			});
		}
	}

	function formatFullDate(dateStr: string): string {
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return new Date(dateStr + 'T12:00:00').toLocaleDateString(currentLocale, {
			weekday: 'long',
			year: 'numeric',
			month: 'long',
			day: 'numeric'
		});
	}

	function handleEntryClick(entry: EntryData) {
		// In buddy mode, only allow editing if the current user is the author
		if (mode === 'buddy' || mode === 'feed') {
			if (entry.authorUserId === currentUserId && onEditEntry) {
				onEditEntry(entry);
			}
			// Otherwise, do nothing (read-only for other authors' entries)
		} else {
			// In own mode, always allow editing
			if (onEditEntry) {
				onEditEntry(entry);
			}
		}
	}

	function canEdit(entry: EntryData): boolean {
		if (mode === 'own') return true;
		// In buddy or feed mode, only the author can edit their own entries
		return entry.authorUserId === currentUserId;
	}

	function handleLightbox(images: ImageData[], index: number, event: Event) {
		if (onOpenLightbox) {
			onOpenLightbox(images, index, event);
		}
	}

	function handleFilterClick(filter: JournalFilter) {
		if (onFilterChange) {
			onFilterChange(filter);
		}
	}

	async function handleAddReaction(entryId: string, emoji: string) {
		if (onAddReaction) {
			await onAddReaction(entryId, emoji);
		}
	}

	async function handleRemoveReaction(entryId: string, reactionId: string) {
		if (onRemoveReaction) {
			await onRemoveReaction(entryId, reactionId);
		}
	}

	// Get author display for header
	function getAuthorDisplay(entry: EntryData): string {
		if (entry.authorUserId === currentUserId) {
			return get(t)('journal.feed.you');
		}
		return entry.authorDisplayName || get(t)('journal.feed.anonymous');
	}
</script>

{#if mode === 'buddy'}
	<!-- Info Banner for buddy mode -->
	<div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
		<p class="text-sm text-blue-800">
			{$t('buddies.journal.infoBanner', { values: { name: buddyDisplayName } })}
		</p>
	</div>
{/if}

<!-- Filter Tabs (for feed mode or own mode with filtering enabled) -->
{#if (mode === 'own' || mode === 'feed') && onFilterChange}
	<div class="flex items-center justify-center gap-1 mb-6">
		<div class="inline-flex bg-gray-100 rounded-lg p-1">
			<button
				type="button"
				onclick={() => handleFilterClick('all')}
				class="px-4 py-2 text-sm font-medium rounded-md transition-all duration-200
					{activeFilter === 'all' 
						? 'bg-white text-gray-900 shadow-sm' 
						: 'text-gray-600 hover:text-gray-900'}"
			>
				{$t('journal.filter.all')}
			</button>
			<button
				type="button"
				onclick={() => handleFilterClick('own')}
				class="px-4 py-2 text-sm font-medium rounded-md transition-all duration-200
					{activeFilter === 'own' 
						? 'bg-white text-gray-900 shadow-sm' 
						: 'text-gray-600 hover:text-gray-900'}"
			>
				{$t('journal.filter.you')}
			</button>
			<button
				type="button"
				onclick={() => handleFilterClick('buddies')}
				class="px-4 py-2 text-sm font-medium rounded-md transition-all duration-200
					{activeFilter === 'buddies' 
						? 'bg-white text-gray-900 shadow-sm' 
						: 'text-gray-600 hover:text-gray-900'}"
			>
				{$t('journal.filter.friends')}
			</button>
		</div>
	</div>
{/if}

<!-- Header with Create Button -->
<div class="flex justify-end mb-4">
	<button onclick={onCreateEntry} class="btn-primary text-sm">
		{#if mode === 'buddy'}
			{$t('buddies.journal.writeEncouragement')}
		{:else}
			{$t('journal.newEntry')}
		{/if}
	</button>
</div>

<!-- Entries List -->
{#if entries.length === 0}
	<div class="card p-12 text-center">
		<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
			<svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
				/>
			</svg>
		</div>
		{#if mode === 'buddy'}
			<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('buddies.journal.emptyTitle')}</h3>
			<p class="text-gray-500 mb-6">
				{$t('buddies.journal.emptyDescription', { values: { name: buddyDisplayName } })}
			</p>
			<button onclick={onCreateEntry} class="btn-primary">{$t('buddies.journal.writeEncouragement')}</button>
		{:else if activeFilter === 'buddies'}
			<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('journal.emptyFriendsTitle')}</h3>
			<p class="text-gray-500">{$t('journal.emptyFriendsDescription')}</p>
		{:else}
			<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('journal.emptyTitle')}</h3>
			<p class="text-gray-500 mb-6">{$t('journal.emptyDescription')}</p>
			<button onclick={onCreateEntry} class="btn-primary">{$t('journal.createFirst')}</button>
		{/if}
	</div>
{:else}
	<div class="space-y-5">
		{#each entries as entry (entry.id)}
			<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
			<article 
				class="card overflow-hidden {canEdit(entry) ? 'cursor-pointer' : ''} hover:shadow-md transition-shadow"
				onclick={() => canEdit(entry) && handleEntryClick(entry)}
				role={canEdit(entry) ? 'button' : 'article'}
				tabindex={canEdit(entry) ? 0 : -1}
				onkeydown={(e) => canEdit(entry) && e.key === 'Enter' && handleEntryClick(entry)}
			>
				<!-- Entry Header - Clear visual separation -->
				<div class="px-5 py-4 bg-gradient-to-r from-slate-100 via-gray-50 to-slate-100 border-b-2 border-gray-200">
					<div class="flex items-center justify-between gap-3">
						<div class="min-w-0">
							<div class="font-semibold text-gray-900">{getAuthorDisplay(entry)}</div>
							<div class="text-gray-500 text-sm" title={formatFullDate(entry.entryDate)}>
								{formatRelativeDate(entry.entryDate)}
							</div>
						</div>
						{#if hasLinkingFields(entry) && (entry.habitStackName || entry.taskItemTitle)}
							<div class="flex-shrink-0">
								<span class="inline-flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-full bg-primary-100 text-primary-700 font-medium border border-primary-200">
									<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
									</svg>
									{entry.habitStackName || entry.taskItemTitle}
								</span>
							</div>
						{/if}
					</div>
				</div>

				<!-- Entry Content -->
				<div class="p-5 bg-white">
					<!-- Title -->
					<h3 class="font-bold text-gray-900 text-lg mb-2">{entry.title}</h3>

					<!-- Description -->
					{#if entry.description}
						<p class="text-gray-600 whitespace-pre-wrap leading-relaxed">{entry.description}</p>
					{/if}

					<!-- Images -->
					{#if entry.images.length > 0}
						<div class="mt-4 flex gap-2 overflow-x-auto pb-1 -mx-1 px-1">
							{#each entry.images as image, idx (image.id)}
								<button
									type="button"
									onclick={(e) => handleLightbox(entry.images, idx, e)}
									class="flex-shrink-0 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 rounded-xl overflow-hidden"
								>
									<img
										src={image.url}
										alt={image.fileName}
										class="w-24 h-24 sm:w-28 sm:h-28 object-cover hover:opacity-90 transition-opacity"
									/>
								</button>
							{/each}
						</div>
					{/if}
				</div>

				<!-- Reactions Section - Always visible with clear add button -->
				{#if onAddReaction && onRemoveReaction}
					<div class="px-5 py-3 border-t-2 border-gray-100 bg-gray-50">
						<JournalReactions
							reactions={getReactions(entry)}
							{currentUserId}
							onAddReaction={(emoji) => handleAddReaction(entry.id, emoji)}
							onRemoveReaction={(reactionId) => handleRemoveReaction(entry.id, reactionId)}
						/>
					</div>
				{/if}
			</article>
		{/each}
	</div>
{/if}
