<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import {
		getBuddyTodayView,
		getBuddyJournal,
		createBuddyJournalEntry,
		uploadBuddyJournalImage,
		addBuddyJournalReaction,
		removeBuddyJournalReaction
	} from '$lib/api/buddies';
	import { processMultipleImages, formatFileSize } from '$lib/utils/imageProcessing';
	import { getLocalDateString } from '$lib/utils/date';
	import TodayViewContent from '$lib/components/today/TodayViewContent.svelte';
	import JournalViewContent from '$lib/components/journal/JournalViewContent.svelte';
	import type { BuddyTodayViewResponse, BuddyJournalEntry, BuddyJournalImage } from '$lib/types';

	// Tab state
	let activeTab = $state<'today' | 'journal'>('today');

	// Today tab state
	let todayData = $state<BuddyTodayViewResponse | null>(null);
	let todayLoading = $state(true);
	let todayError = $state('');
	let currentDate = $state(getLocalDateString());

	// Journal tab state
	let journalEntries = $state<BuddyJournalEntry[]>([]);
	let journalLoading = $state(false);
	let journalLoaded = $state(false);
	let journalError = $state('');

	// Date picker state
	let showDatePicker = $state(false);

	// Create modal state
	let showModal = $state(false);
	let modalTitle = $state('');
	let modalDescription = $state('');
	let modalLoading = $state(false);
	let modalError = $state('');
	let pendingImages = $state<File[]>([]);
	let uploadingImages = $state(false);
	let processingImages = $state(false);
	let imageProcessWarnings = $state<string[]>([]);

	// Image lightbox state
	let lightboxImages = $state<BuddyJournalImage[]>([]);
	let lightboxIndex = $state(0);
	let showLightbox = $state(false);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadTodayData();
	});

	async function loadTodayData() {
		todayLoading = true;
		todayError = '';

		try {
			todayData = await getBuddyTodayView($page.params.userId!, currentDate);
		} catch (e) {
			if (e instanceof Error && e.message.includes('403')) {
				todayError = "You do not have permission to view this user's progress.";
			} else {
				todayError = e instanceof Error ? e.message : 'Failed to load data';
			}
		} finally {
			todayLoading = false;
		}
	}

	async function loadJournalData() {
		if (journalLoaded) return;

		journalLoading = true;
		journalError = '';

		try {
			journalEntries = await getBuddyJournal($page.params.userId!);
			journalLoaded = true;
		} catch (e) {
			if (e instanceof Error && e.message.includes('403')) {
				journalError = "You do not have permission to view this user's journal.";
			} else {
				journalError = e instanceof Error ? e.message : 'Failed to load journal';
			}
		} finally {
			journalLoading = false;
		}
	}

	function switchTab(tab: 'today' | 'journal') {
		activeTab = tab;
		if (tab === 'journal' && !journalLoaded) {
			loadJournalData();
		}
	}

	function goToPreviousDay() {
		const date = new Date(currentDate + 'T12:00:00');
		date.setDate(date.getDate() - 1);
		currentDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
		loadTodayData();
	}

	function goToNextDay() {
		const date = new Date(currentDate + 'T12:00:00');
		date.setDate(date.getDate() + 1);
		currentDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
		loadTodayData();
	}

	function goToToday() {
		currentDate = getLocalDateString();
		loadTodayData();
	}

	function handleDateChange(e: Event) {
		const input = e.target as HTMLInputElement;
		currentDate = input.value;
		showDatePicker = false;
		loadTodayData();
	}

	function formatDisplayDate(dateStr: string): string {
		const date = new Date(dateStr + 'T12:00:00');
		return date.toLocaleDateString('en-US', {
			weekday: 'long',
			month: 'long',
			day: 'numeric'
		});
	}

	function isToday(dateStr: string): boolean {
		return dateStr === getLocalDateString();
	}

	// Journal modal functions
	function openCreateModal() {
		modalTitle = '';
		modalDescription = '';
		modalError = '';
		pendingImages = [];
		imageProcessWarnings = [];
		showModal = true;
	}

	function closeModal() {
		showModal = false;
		modalError = '';
		pendingImages = [];
		imageProcessWarnings = [];
	}

	async function handleFileSelect(e: Event) {
		const input = e.target as HTMLInputElement;
		if (!input.files || input.files.length === 0) {
			return;
		}

		const newFiles = Array.from(input.files);
		const availableSlots = 5 - pendingImages.length;

		if (newFiles.length > availableSlots) {
			modalError = `Can only add ${availableSlots} more image(s)`;
			input.value = '';
			return;
		}

		processingImages = true;
		modalError = '';
		imageProcessWarnings = [];

		try {
			const { processed, errors } = await processMultipleImages(newFiles);

			if (errors.length > 0) {
				modalError = errors.map(e => `${e.fileName}: ${e.error}`).join('\n');
			}

			if (processed.length > 0) {
				// Collect warnings from processed images
				const warnings = processed
					.filter(p => p.warning)
					.map(p => p.warning!);
				
				if (warnings.length > 0) {
					imageProcessWarnings = warnings;
				}

				// Add successfully processed files
				const processedFiles = processed.map(p => p.file);
				pendingImages = [...pendingImages, ...processedFiles];

				// Image compression is handled silently
			}
		} catch (error) {
			modalError = error instanceof Error ? error.message : 'Failed to process images';
		} finally {
			processingImages = false;
			input.value = '';
		}
	}

	function removePendingImage(index: number) {
		pendingImages = pendingImages.filter((_, i) => i !== index);
	}

	async function handleSubmit() {
		if (!modalTitle.trim()) {
			modalError = 'Title is required';
			return;
		}

		modalLoading = true;
		modalError = '';

		try {
			let newEntry = await createBuddyJournalEntry($page.params.userId!, {
				title: modalTitle.trim(),
				description: modalDescription.trim() || null,
				entryDate: getLocalDateString()
			});

			// Upload pending images
			if (pendingImages.length > 0) {
				uploadingImages = true;
				for (const file of pendingImages) {
					try {
						const image = await uploadBuddyJournalImage($page.params.userId!, newEntry.id, file);
						newEntry = { ...newEntry, images: [...newEntry.images, image] };
					} catch {
						// Image upload failed silently - user can retry later
					}
				}
				uploadingImages = false;
			}

			journalEntries = [newEntry, ...journalEntries];
			closeModal();
		} catch (e) {
			modalError = e instanceof Error ? e.message : 'Failed to create entry';
		} finally {
			modalLoading = false;
		}
	}

	// Lightbox functions
	function openLightbox(images: BuddyJournalImage[], index: number, event: Event) {
		event.stopPropagation();
		lightboxImages = images;
		lightboxIndex = index;
		showLightbox = true;
	}

	function closeLightbox() {
		showLightbox = false;
		lightboxImages = [];
		lightboxIndex = 0;
	}

	function nextImage() {
		if (lightboxIndex < lightboxImages.length - 1) {
			lightboxIndex++;
		}
	}

	function prevImage() {
		if (lightboxIndex > 0) {
			lightboxIndex--;
		}
	}

	function handleLightboxKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			closeLightbox();
		} else if (event.key === 'ArrowRight') {
			nextImage();
		} else if (event.key === 'ArrowLeft') {
			prevImage();
		}
	}

	// Reaction handlers
	async function handleAddReaction(entryId: string, emoji: string) {
		try {
			const reaction = await addBuddyJournalReaction($page.params.userId!, entryId, emoji);
			// Update local state
			journalEntries = journalEntries.map(entry => {
				if (entry.id === entryId) {
					return {
						...entry,
						reactions: [...(entry.reactions || []), reaction]
					};
				}
				return entry;
			});
		} catch {
			// Failed to add reaction - silent failure
		}
	}

	async function handleRemoveReaction(entryId: string, reactionId: string) {
		try {
			await removeBuddyJournalReaction($page.params.userId!, entryId, reactionId);
			// Update local state
			journalEntries = journalEntries.map(entry => {
				if (entry.id === entryId) {
					return {
						...entry,
						reactions: (entry.reactions || []).filter(r => r.id !== reactionId)
					};
				}
				return entry;
			});
		} catch {
			// Failed to remove reaction - silent failure
		}
	}

	// Get display name from todayData
	const userDisplayName = $derived(todayData?.userDisplayName ?? 'User');
</script>

<div class="bg-warm-cream">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		{#if todayLoading && !todayData}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if todayError && !todayData}
			<div class="card p-8 text-center">
				<p class="text-red-600 mb-4">{todayError}</p>
				<a href="/buddies" class="btn-primary">Back to Buddies</a>
			</div>
		{:else}
			<!-- Header -->
			<div class="mb-6">
				<a href="/buddies" class="text-sm text-cocoa-500 hover:text-cocoa-700 mb-1 inline-block">
					&larr; Back to Buddies
				</a>
				<h1 class="text-2xl font-bold text-cocoa-800">{userDisplayName}'s Progress</h1>
			</div>

			<!-- Tabs -->
			<div class="flex border-b border-primary-100 mb-6">
				<button
					onclick={() => switchTab('today')}
					class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'today'
						? 'border-primary-500 text-primary-600'
						: 'border-transparent text-cocoa-500 hover:text-cocoa-700 hover:border-primary-200'}"
				>
					Today
				</button>
				<button
					onclick={() => switchTab('journal')}
					class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'journal'
						? 'border-primary-500 text-primary-600'
						: 'border-transparent text-cocoa-500 hover:text-cocoa-700 hover:border-primary-200'}"
				>
					Journal
				</button>
			</div>

			<!-- Today Tab -->
			{#if activeTab === 'today'}
				{#if todayLoading}
					<div class="flex justify-center py-12">
						<div
							class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
						></div>
					</div>
				{:else if todayData}
					<!-- Date Navigation -->
					<div class="bg-warm-paper border border-gray-100 rounded-2xl mb-6">
						<div class="flex items-center justify-center py-3 gap-4">
							<button
								onclick={goToPreviousDay}
								class="p-2 text-cocoa-500 hover:text-cocoa-700 hover:bg-primary-50 rounded-2xl flex-shrink-0"
								title="Previous day"
							>
								<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
								</svg>
							</button>

							<button
								onclick={() => (showDatePicker = !showDatePicker)}
								class="text-lg font-semibold text-cocoa-800 hover:text-primary-600 px-3 py-1 rounded-2xl hover:bg-primary-50 min-w-[280px] text-center"
								style="width: 280px;"
							>
								{formatDisplayDate(currentDate)}
							</button>

							<button
								onclick={goToNextDay}
								class="p-2 text-cocoa-500 hover:text-cocoa-700 hover:bg-primary-50 rounded-2xl flex-shrink-0"
								title="Next day"
							>
								<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
								</svg>
							</button>
						</div>

						<!-- Jump to Today Button -->
						{#if !isToday(currentDate)}
							<div class="flex justify-center pb-3">
								<button
									onclick={goToToday}
									class="px-4 py-1.5 text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-2xl font-medium transition-colors"
								>
									Jump to Today
								</button>
							</div>
						{/if}

						<!-- Date Picker Dropdown -->
						{#if showDatePicker}
							<div class="pb-4 px-4">
								<input
									type="date"
									value={currentDate}
									onchange={handleDateChange}
									class="w-full px-3 py-2 border border-primary-200 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
								/>
							</div>
						{/if}
					</div>

					<TodayViewContent
						todayData={todayData}
						readonly={true}
					/>
				{/if}
			{/if}

			<!-- Journal Tab -->
			{#if activeTab === 'journal'}
				{#if journalLoading}
					<div class="flex justify-center py-12">
						<div
							class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
						></div>
					</div>
				{:else if journalError}
					<div class="card p-8 text-center">
						<p class="text-red-600 mb-4">{journalError}</p>
					</div>
				{:else}
					<JournalViewContent
						entries={journalEntries}
						mode="buddy"
						buddyDisplayName={userDisplayName}
						currentUserId={$auth.user?.id}
						onCreateEntry={openCreateModal}
						onOpenLightbox={openLightbox}
						onAddReaction={handleAddReaction}
						onRemoveReaction={handleRemoveReaction}
					/>
				{/if}
			{/if}
		{/if}
	</main>

	<!-- Create Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-warm-paper rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-cocoa-800">Write Encouragement</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-cocoa-600" aria-label="Close">
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</button>
					</div>

					{#if modalError}
						<div
							class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-4 whitespace-pre-line"
						>
							{modalError}
						</div>
					{/if}

					{#if imageProcessWarnings.length > 0}
						<div class="bg-yellow-50 border border-yellow-200 text-yellow-800 px-4 py-3 rounded-2xl text-sm mb-4">
							{#each imageProcessWarnings as warning}
								<p class="mb-1 last:mb-0">{warning}</p>
							{/each}
						</div>
					{/if}

					<div class="space-y-4">
						<div>
							<label for="title" class="block text-sm font-medium text-cocoa-700 mb-1">Title *</label>
							<input
								type="text"
								id="title"
								bind:value={modalTitle}
								placeholder="Great job today!"
								maxlength="255"
								class="input"
							/>
						</div>

						<div>
							<label for="description" class="block text-sm font-medium text-cocoa-700 mb-1"
								>Message</label
							>
							<textarea
								id="description"
								bind:value={modalDescription}
								rows="4"
								placeholder="Write an encouraging message..."
								class="input"
							></textarea>
						</div>

						<!-- Images Section -->
						<div>
							<span class="block text-sm font-medium text-cocoa-700 mb-2">Images</span>

							<!-- Pending images -->
							{#if pendingImages.length > 0}
								<div class="flex flex-wrap gap-2 mb-3">
									{#each pendingImages as file, index (index)}
										<div class="relative group">
											<img
												src={URL.createObjectURL(file)}
												alt={file.name}
												class="w-20 h-20 object-cover rounded-2xl opacity-70"
											/>
											<button
												type="button"
												onclick={() => removePendingImage(index)}
												class="absolute -top-2 -right-2 w-6 h-6 bg-warm-cream0 text-white rounded-full flex items-center justify-center"
												aria-label="Remove image"
											>
												<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M6 18L18 6M6 6l12 12"
													/>
												</svg>
											</button>
										</div>
									{/each}
								</div>
							{/if}

							<!-- Upload button -->
							{#if pendingImages.length < 5}
								<label
									class="flex items-center justify-center w-full h-20 border-2 border-dashed border-primary-200 rounded-2xl cursor-pointer hover:border-gray-400 transition-colors {processingImages ? 'opacity-50 cursor-wait' : ''}"
								>
									<div class="text-center">
										{#if processingImages}
											<div class="animate-spin w-6 h-6 mx-auto border-2 border-primary-600 border-t-transparent rounded-full"></div>
											<span class="text-xs text-cocoa-500 mt-1">Processing...</span>
										{:else}
											<svg
												class="w-6 h-6 mx-auto text-gray-400"
												fill="none"
												stroke="currentColor"
												viewBox="0 0 24 24"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													stroke-width="2"
													d="M12 4v16m8-8H4"
												/>
											</svg>
											<span class="text-xs text-cocoa-500">Add images</span>
										{/if}
									</div>
									<input
										type="file"
										accept="image/jpeg,image/png,image/gif,image/webp"
										multiple
										onchange={handleFileSelect}
										disabled={processingImages}
										class="hidden"
									/>
								</label>
							{/if}
							<p class="text-xs text-cocoa-500 mt-1">
								Images are automatically compressed to WebP. Max 5 images, 5MB each. GIFs kept as-is.
							</p>
						</div>
					</div>

					<div class="flex justify-end gap-3 mt-6 pt-4 border-t border-primary-100">
						<button
							type="button"
							onclick={closeModal}
							class="btn-secondary"
							disabled={modalLoading || uploadingImages}
						>
							Cancel
						</button>
						<button
							type="button"
							onclick={handleSubmit}
							class="btn-primary"
							disabled={modalLoading || uploadingImages}
						>
							{#if modalLoading}
								Saving...
							{:else if uploadingImages}
								Uploading...
							{:else}
								Send
							{/if}
						</button>
					</div>
				</div>
			</div>
		</div>
	{/if}

	<!-- Image Lightbox -->
	{#if showLightbox && lightboxImages.length > 0}
		<div
			class="fixed inset-0 bg-black bg-opacity-90 flex items-center justify-center z-[60]"
			role="dialog"
			aria-modal="true"
			aria-label="Image viewer"
			onclick={closeLightbox}
			onkeydown={handleLightboxKeydown}
			tabindex="-1"
		>
			<!-- Close button -->
			<button
				onclick={closeLightbox}
				class="absolute top-4 right-4 text-white hover:text-gray-300 z-10"
				aria-label="Close"
			>
				<svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
				</svg>
			</button>

			<!-- Previous button -->
			{#if lightboxIndex > 0}
				<button
					onclick={(e) => { e.stopPropagation(); prevImage(); }}
					class="absolute left-4 top-1/2 -translate-y-1/2 text-white hover:text-gray-300 p-2 rounded-full bg-black bg-opacity-50 hover:bg-opacity-70"
					aria-label="Previous image"
				>
					<svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>
			{/if}

			<!-- Image -->
			<div
				class="max-w-[90vw] max-h-[90vh] flex items-center justify-center"
				onclick={(e) => e.stopPropagation()}
				onkeydown={(e) => e.stopPropagation()}
				role="presentation"
			>
				<img
					src={lightboxImages[lightboxIndex].url}
					alt={lightboxImages[lightboxIndex].fileName}
					class="max-w-full max-h-[90vh] object-contain rounded-2xl"
				/>
			</div>

			<!-- Next button -->
			{#if lightboxIndex < lightboxImages.length - 1}
				<button
					onclick={(e) => { e.stopPropagation(); nextImage(); }}
					class="absolute right-4 top-1/2 -translate-y-1/2 text-white hover:text-gray-300 p-2 rounded-full bg-black bg-opacity-50 hover:bg-opacity-70"
					aria-label="Next image"
				>
					<svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</button>
			{/if}

			<!-- Image counter -->
			{#if lightboxImages.length > 1}
				<div class="absolute bottom-4 left-1/2 -translate-x-1/2 text-white text-sm bg-black bg-opacity-50 px-3 py-1 rounded-full">
					{lightboxIndex + 1} / {lightboxImages.length}
				</div>
			{/if}
		</div>
	{/if}
</div>
