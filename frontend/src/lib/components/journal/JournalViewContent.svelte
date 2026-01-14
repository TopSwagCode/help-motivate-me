<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { auth } from '$lib/stores/auth';
	import type { JournalEntry, JournalImage } from '$lib/types';
	import type { BuddyJournalEntry, BuddyJournalImage } from '$lib/types/buddy';

	// Unified type to accept both JournalEntry and BuddyJournalEntry
	type EntryData = JournalEntry | BuddyJournalEntry;
	type ImageData = JournalImage | BuddyJournalImage;

	interface Props {
		entries: EntryData[];
		currentUserId?: string | null;
		// Mode: 'own' = viewing own journal, 'buddy' = viewing buddy's journal
		mode?: 'own' | 'buddy';
		// Buddy-specific props
		buddyDisplayName?: string;
		// Event handlers
		onCreateEntry?: () => void;
		onEditEntry?: (entry: EntryData) => void;
		onOpenLightbox?: (images: ImageData[], index: number, event: Event) => void;
	}

	let {
		entries,
		currentUserId = null,
		mode = 'own',
		buddyDisplayName = 'User',
		onCreateEntry,
		onEditEntry,
		onOpenLightbox
	}: Props = $props();

	// Type guard to check if entry has linking fields (JournalEntry)
	function hasLinkingFields(entry: EntryData): entry is JournalEntry {
		return 'habitStackId' in entry;
	}

	function formatDate(dateStr: string): string {
		return new Date(dateStr + 'T12:00:00').toLocaleDateString(get(locale) === 'da' ? 'da-DK' : 'en-US', {
			weekday: 'long',
			year: 'numeric',
			month: 'long',
			day: 'numeric'
		});
	}

	function handleEntryClick(entry: EntryData) {
		// In buddy mode, only allow editing if the current user is the author
		if (mode === 'buddy') {
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
		// In buddy mode, only the author can edit their own entries
		return entry.authorUserId === currentUserId;
	}

	function handleLightbox(images: ImageData[], index: number, event: Event) {
		if (onOpenLightbox) {
			onOpenLightbox(images, index, event);
		}
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
		{:else}
			<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('journal.emptyTitle')}</h3>
			<p class="text-gray-500 mb-6">{$t('journal.emptyDescription')}</p>
			<button onclick={onCreateEntry} class="btn-primary">{$t('journal.createFirst')}</button>
		{/if}
	</div>
{:else}
	<div class="space-y-4">
		{#each entries as entry (entry.id)}
			<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
			<div 
				class="card p-5 {canEdit(entry) ? 'cursor-pointer hover:shadow-md transition-shadow' : ''}"
				onclick={() => canEdit(entry) && handleEntryClick(entry)}
				role={canEdit(entry) ? 'button' : undefined}
				tabindex={canEdit(entry) ? 0 : undefined}
				onkeydown={(e) => canEdit(entry) && e.key === 'Enter' && handleEntryClick(entry)}
			>
				<div class="flex items-start justify-between mb-2">
					<div>
						<h3 class="font-semibold text-gray-900 text-lg">{entry.title}</h3>
						<p class="text-sm text-gray-500">{formatDate(entry.entryDate)}</p>
					</div>
					{#if hasLinkingFields(entry) && (entry.habitStackName || entry.taskItemTitle)}
						<span class="text-xs px-2 py-1 rounded-full bg-primary-50 text-primary-700">
							{entry.habitStackName || entry.taskItemTitle}
						</span>
					{/if}
				</div>

				{#if entry.description}
					<p class="text-gray-600 text-sm line-clamp-3 mb-3 whitespace-pre-wrap">{entry.description}</p>
				{/if}

				{#if entry.images.length > 0}
					<div class="flex gap-2 mt-3 overflow-x-auto pb-2">
						{#each entry.images as image, idx (image.id)}
							<button
								type="button"
								onclick={(e) => handleLightbox(entry.images, idx, e)}
								class="flex-shrink-0 focus:outline-none focus:ring-2 focus:ring-primary-500 rounded-lg"
							>
								<img
									src={image.url}
									alt={image.fileName}
									class="w-20 h-20 object-cover rounded-lg hover:opacity-80 transition-opacity"
								/>
							</button>
						{/each}
					</div>
				{/if}

				<!-- Author Attribution (for buddy entries or entries from buddies) -->
				{#if entry.authorDisplayName && entry.authorUserId && entry.authorUserId !== currentUserId}
					<div class="flex justify-end mt-3 pt-3 border-t border-gray-100">
						<span class="text-xs text-gray-500 italic">
							— {entry.authorDisplayName}
						</span>
					</div>
				{/if}
			</div>
		{/each}
	</div>
{/if}
