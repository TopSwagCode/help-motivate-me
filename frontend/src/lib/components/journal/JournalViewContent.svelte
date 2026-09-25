<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { commandBar } from '$lib/stores/commandBar';
	import { aiConfig } from '$lib/stores/aiConfig';
	import type { JournalEntry, JournalImage } from '$lib/types';

	// Unified type to accept both JournalEntry and BuddyJournalEntry
	type EntryData = JournalEntry;
	type ImageData = JournalImage;

	interface Props {
		entries: EntryData[];
		currentUserId?: string | null;
		// Mode: 'own' = viewing own journal, 'buddy' = viewing buddy's journal, 'feed' = combined feed view
		// Event handlers
		onCreateEntry?: () => void;
		onEditEntry?: (entry: EntryData) => void;
		onOpenLightbox?: (images: ImageData[], index: number, event: Event) => void;
	}

	let {
		entries,
		currentUserId = null,
		onCreateEntry,
		onEditEntry,
		onOpenLightbox
	}: Props = $props();

	// Type guard to check if entry has linking fields (JournalEntry)
	function hasLinkingFields(entry: EntryData): entry is JournalEntry {
		return 'habitStackId' in entry;
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

		const diffDays = Math.round(
			(today.getTime() - entryDateNormalized.getTime()) / (1000 * 60 * 60 * 24)
		);

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
		onEditEntry?.(entry);
	}

	function canEdit(entry: EntryData): boolean {
		return true;
	}

	function handleLightbox(images: ImageData[], index: number, event: Event) {
		if (onOpenLightbox) {
			onOpenLightbox(images, index, event);
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

<!-- Header with Create Button -->
<div class="flex justify-end mb-4">
	<button onclick={onCreateEntry} class="btn-primary text-sm">
		{$t('journal.newEntry')}
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
		<div class="max-w-md mx-auto">
			<h3 class="text-lg font-medium text-cocoa-800 mb-2">{$t('journal.emptyTitle')}</h3>
			<p class="text-cocoa-500 mb-4">{$t('journal.emptyDescription')}</p>
			{#if $aiConfig.isEnabled}<p
					class="text-cocoa-500 text-sm mb-6 flex items-center justify-center gap-1 flex-wrap"
				>
					{$t('journal.emptyHowTo')}
					<button
						type="button"
						onclick={() => commandBar.open()}
						class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
						title="Open AI Assistant"
					>
						<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M13 10V3L4 14h7v7l9-11h-7z"
							/>
						</svg>
					</button>
				</p>{/if}
			<button onclick={onCreateEntry} class="btn-primary">{$t('journal.createFirst')}</button>
		</div>
	</div>
{:else}
	<div class="space-y-5">
		{#each entries as entry (entry.id)}
			<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
			<article
				class="card overflow-hidden {canEdit(entry)
					? 'cursor-pointer'
					: ''} hover:shadow-md transition-shadow"
				onclick={() => canEdit(entry) && handleEntryClick(entry)}
				role={canEdit(entry) ? 'button' : 'article'}
				tabindex={canEdit(entry) ? 0 : -1}
				onkeydown={(e) => canEdit(entry) && e.key === 'Enter' && handleEntryClick(entry)}
			>
				<!-- Entry Header - Clear visual separation -->
				<div
					class="px-5 py-4 bg-gradient-to-r from-slate-100 via-gray-50 to-slate-100 border-b-2 border-primary-100"
				>
					<div class="flex items-center justify-between gap-3">
						<div class="min-w-0">
							<div class="font-semibold text-cocoa-800">{getAuthorDisplay(entry)}</div>
							<div class="text-cocoa-500 text-sm" title={formatFullDate(entry.entryDate)}>
								{formatRelativeDate(entry.entryDate)}
							</div>
						</div>
						{#if hasLinkingFields(entry) && (entry.habitStackName || entry.taskItemTitle)}
							<div class="flex-shrink-0">
								<span
									class="inline-flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-full bg-primary-100 text-primary-700 font-medium border border-primary-200"
								>
									<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
										/>
									</svg>
									{entry.habitStackName || entry.taskItemTitle}
								</span>
							</div>
						{/if}
					</div>
				</div>

				<!-- Entry Content -->
				<div class="p-5 bg-warm-paper">
					<!-- Title -->
					<h3 class="font-bold text-cocoa-800 text-lg mb-2">{entry.title}</h3>

					<!-- Description -->
					{#if entry.description}
						<p class="text-cocoa-600 whitespace-pre-wrap leading-relaxed">{entry.description}</p>
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
			</article>
		{/each}
	</div>
{/if}
