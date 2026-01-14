<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import {
		getBuddyTodayView,
		getBuddyJournal,
		createBuddyJournalEntry,
		uploadBuddyJournalImage
	} from '$lib/api/buddies';
	import { processMultipleImages, formatFileSize } from '$lib/utils/imageProcessing';
	import TodayViewContent from '$lib/components/today/TodayViewContent.svelte';
	import type { BuddyTodayViewResponse, BuddyJournalEntry, BuddyJournalImage } from '$lib/types/buddy';

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

	function getLocalDateString(): string {
		const now = new Date();
		return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
	}

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

	function formatJournalDate(dateStr: string): string {
		return new Date(dateStr + 'T12:00:00').toLocaleDateString('en-US', {
			weekday: 'long',
			year: 'numeric',
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

				// Show compression info for significantly compressed images
				const significantlySaved = processed.filter(
					p => p.wasProcessed && p.originalSize - p.newSize > 100 * 1024 // > 100KB saved
				);

				if (significantlySaved.length > 0) {
					console.log('Image compression results:');
					significantlySaved.forEach(p => {
						const savedKB = ((p.originalSize - p.newSize) / 1024).toFixed(0);
						console.log(`  ${p.file.name}: saved ${savedKB} KB (${formatFileSize(p.originalSize)} → ${formatFileSize(p.newSize)})`);
					});
				}
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
				description: modalDescription.trim() || undefined,
				entryDate: new Date().toISOString().split('T')[0]
			});

			// Upload pending images
			if (pendingImages.length > 0) {
				uploadingImages = true;
				for (const file of pendingImages) {
					try {
						const image = await uploadBuddyJournalImage($page.params.userId!, newEntry.id, file);
						newEntry = { ...newEntry, images: [...newEntry.images, image] };
					} catch (e) {
						console.error('Failed to upload image:', e);
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

	// Get display name from todayData
	const userDisplayName = $derived(todayData?.userDisplayName ?? 'User');
</script>

<div class="min-h-screen bg-gray-50">
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
				<a href="/buddies" class="text-sm text-gray-500 hover:text-gray-700 mb-1 inline-block">
					&larr; Back to Buddies
				</a>
				<h1 class="text-2xl font-bold text-gray-900">{userDisplayName}'s Progress</h1>
			</div>

			<!-- Tabs -->
			<div class="flex border-b border-gray-200 mb-6">
				<button
					onclick={() => switchTab('today')}
					class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'today'
						? 'border-primary-500 text-primary-600'
						: 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
				>
					Today
				</button>
				<button
					onclick={() => switchTab('journal')}
					class="px-4 py-2 text-sm font-medium border-b-2 transition-colors {activeTab === 'journal'
						? 'border-primary-500 text-primary-600'
						: 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
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
					<div class="bg-white border border-gray-100 rounded-lg mb-6">
						<div class="flex items-center justify-center py-3 gap-4">
							<button
								onclick={goToPreviousDay}
								class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg flex-shrink-0"
								title="Previous day"
							>
								<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
								</svg>
							</button>

							<button
								onclick={() => (showDatePicker = !showDatePicker)}
								class="text-lg font-semibold text-gray-900 hover:text-primary-600 px-3 py-1 rounded-lg hover:bg-gray-100 min-w-[280px] text-center"
								style="width: 280px;"
							>
								{formatDisplayDate(currentDate)}
							</button>

							<button
								onclick={goToNextDay}
								class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg flex-shrink-0"
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
									class="px-4 py-1.5 text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-lg font-medium transition-colors"
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
									class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
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
					<!-- Info Banner -->
					<div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
						<p class="text-sm text-blue-800">
							As an accountability buddy, you can leave encouraging notes and messages here. Your entries
							will be visible to {userDisplayName} and will help keep them motivated!
						</p>
					</div>

					<!-- Write Button -->
					<div class="flex justify-end mb-4">
						<button onclick={openCreateModal} class="btn-primary text-sm">
							Write Encouragement
						</button>
					</div>

					<!-- Entries List -->
					{#if journalEntries.length === 0}
						<div class="card p-12 text-center">
							<div
								class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center"
							>
								<svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
									/>
								</svg>
							</div>
							<h3 class="text-lg font-medium text-gray-900 mb-2">No journal entries yet</h3>
							<p class="text-gray-500 mb-6">
								Be the first to leave an encouraging note for {userDisplayName}!
							</p>
							<button onclick={openCreateModal} class="btn-primary">Write Encouragement</button>
						</div>
					{:else}
						<div class="space-y-4">
							{#each journalEntries as entry (entry.id)}
								<div class="card p-5">
									<div class="flex items-start justify-between mb-2">
										<div>
											<h3 class="font-semibold text-gray-900 text-lg">{entry.title}</h3>
											<p class="text-sm text-gray-500">{formatJournalDate(entry.entryDate)}</p>
										</div>
									</div>

									{#if entry.description}
										<p class="text-gray-600 text-sm mb-3 whitespace-pre-wrap">{entry.description}</p>
									{/if}

									{#if entry.images.length > 0}
										<div class="flex gap-2 mt-3 overflow-x-auto pb-2">
											{#each entry.images as image, idx (image.id)}
												<button
													type="button"
													onclick={(e) => openLightbox(entry.images, idx, e)}
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

									<!-- Author Attribution -->
									{#if entry.authorDisplayName}
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
			<div class="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">Write Encouragement</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-gray-600" aria-label="Close">
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
							class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4 whitespace-pre-line"
						>
							{modalError}
						</div>
					{/if}

					{#if imageProcessWarnings.length > 0}
						<div class="bg-yellow-50 border border-yellow-200 text-yellow-800 px-4 py-3 rounded-lg text-sm mb-4">
							{#each imageProcessWarnings as warning}
								<p class="mb-1 last:mb-0">{warning}</p>
							{/each}
						</div>
					{/if}

					<div class="space-y-4">
						<div>
							<label for="title" class="block text-sm font-medium text-gray-700 mb-1">Title *</label>
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
							<label for="description" class="block text-sm font-medium text-gray-700 mb-1"
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
							<label class="block text-sm font-medium text-gray-700 mb-2">Images</label>

							<!-- Pending images -->
							{#if pendingImages.length > 0}
								<div class="flex flex-wrap gap-2 mb-3">
									{#each pendingImages as file, index (index)}
										<div class="relative group">
											<img
												src={URL.createObjectURL(file)}
												alt={file.name}
												class="w-20 h-20 object-cover rounded-lg opacity-70"
											/>
											<button
												type="button"
												onclick={() => removePendingImage(index)}
												class="absolute -top-2 -right-2 w-6 h-6 bg-gray-500 text-white rounded-full flex items-center justify-center"
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
									class="flex items-center justify-center w-full h-20 border-2 border-dashed border-gray-300 rounded-lg cursor-pointer hover:border-gray-400 transition-colors {processingImages ? 'opacity-50 cursor-wait' : ''}"
								>
									<div class="text-center">
										{#if processingImages}
											<div class="animate-spin w-6 h-6 mx-auto border-2 border-primary-600 border-t-transparent rounded-full"></div>
											<span class="text-xs text-gray-500 mt-1">Processing...</span>
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
											<span class="text-xs text-gray-500">Add images</span>
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
							<p class="text-xs text-gray-500 mt-1">
								Images are automatically compressed to WebP. Max 5 images, 5MB each. GIFs kept as-is.
							</p>
						</div>
					</div>

					<div class="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200">
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
			>
				<img
					src={lightboxImages[lightboxIndex].url}
					alt={lightboxImages[lightboxIndex].fileName}
					class="max-w-full max-h-[90vh] object-contain rounded-lg"
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
