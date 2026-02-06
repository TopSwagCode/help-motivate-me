<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { milestoneStore } from '$lib/stores/milestones';
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import {
		getJournalEntries,
		createJournalEntry,
		updateJournalEntry,
		deleteJournalEntry,
		uploadJournalImage,
		deleteJournalImage,
		getLinkableHabitStacks,
		getLinkableTasks,
		addJournalReaction,
		removeJournalReaction,
		type JournalFilter
	} from '$lib/api/journal';
	import { processMultipleImages, formatFileSize } from '$lib/utils/imageProcessing';
	import { getLocalDateString } from '$lib/utils/date';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import JournalViewContent from '$lib/components/journal/JournalViewContent.svelte';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';
	import type {
		JournalEntry,
		JournalImage,
		LinkableHabitStack,
		LinkableTask
	} from '$lib/types';

	let entries = $state<JournalEntry[]>([]);
	let loading = $state(true);
	let error = $state('');
	let activeFilter = $state<JournalFilter>('all');

	// Linkable items for dropdowns
	let habitStacks = $state<LinkableHabitStack[]>([]);
	let tasks = $state<LinkableTask[]>([]);

	// Create/Edit modal state
	let showModal = $state(false);
	let editingEntry = $state<JournalEntry | null>(null);
	let modalTitle = $state('');
	let modalDescription = $state('');
	let modalEntryDate = $state('');
	let modalLinkType = $state<'none' | 'habitStack' | 'task'>('none');
	let modalHabitStackId = $state('');
	let modalTaskId = $state('');
	let modalLoading = $state(false);
	let modalError = $state('');

	// Image upload state
	let pendingImages = $state<File[]>([]);
	let uploadingImages = $state(false);
	let processingImages = $state(false);
	let imageProcessWarnings = $state<string[]>([]);

	// Image lightbox state
	let lightboxImages = $state<JournalImage[]>([]);
	let lightboxIndex = $state(0);
	let showLightbox = $state(false);

	const isEditing = $derived(editingEntry !== null);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			const [entriesData, stacksData, tasksData] = await Promise.all([
				getJournalEntries(activeFilter),
				getLinkableHabitStacks(),
				getLinkableTasks()
			]);
			entries = entriesData;
			habitStacks = stacksData;
			tasks = tasksData;
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.loadFailed');
		} finally {
			loading = false;
		}
	}

	async function handleFilterChange(filter: JournalFilter) {
		activeFilter = filter;
		loading = true;
		error = '';
		try {
			entries = await getJournalEntries(filter);
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.loadFailed');
		} finally {
			loading = false;
		}
	}

	async function handleAddReaction(entryId: string, emoji: string) {
		try {
			const reaction = await addJournalReaction(entryId, emoji);
			// Update local state
			entries = entries.map(entry => {
				if (entry.id === entryId) {
					return {
						...entry,
						reactions: [...(entry.reactions || []), reaction]
					};
				}
				return entry;
			});
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.addReactionFailed');
		}
	}

	async function handleRemoveReaction(entryId: string, reactionId: string) {
		try {
			await removeJournalReaction(entryId, reactionId);
			// Update local state
			entries = entries.map(entry => {
				if (entry.id === entryId) {
					return {
						...entry,
						reactions: (entry.reactions || []).filter(r => r.id !== reactionId)
					};
				}
				return entry;
			});
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.removeReactionFailed');
		}
	}

	function openCreateModal() {
		editingEntry = null;
		modalTitle = '';
		modalDescription = '';
		modalEntryDate = getLocalDateString();
		modalLinkType = 'none';
		modalHabitStackId = '';
		modalTaskId = '';
		pendingImages = [];
		modalError = '';
		showModal = true;
	}

	function openEditModal(entry: JournalEntry) {
		editingEntry = entry;
		modalTitle = entry.title;
		modalDescription = entry.description || '';
		modalEntryDate = entry.entryDate;

		if (entry.habitStackId) {
			modalLinkType = 'habitStack';
			modalHabitStackId = entry.habitStackId;
			modalTaskId = '';
		} else if (entry.taskItemId) {
			modalLinkType = 'task';
			modalTaskId = entry.taskItemId;
			modalHabitStackId = '';
		} else {
			modalLinkType = 'none';
			modalHabitStackId = '';
			modalTaskId = '';
		}

		pendingImages = [];
		modalError = '';
		showModal = true;
	}

	function closeModal() {
		showModal = false;
		editingEntry = null;
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
		const currentCount = (editingEntry?.images.length || 0) + pendingImages.length;
		const availableSlots = 5 - currentCount;

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
			modalError = get(t)('journal.errors.titleRequired');
			return;
		}

		modalLoading = true;
		modalError = '';

		try {
			const data = {
				title: modalTitle.trim(),
				description: modalDescription.trim() || undefined,
				entryDate: modalEntryDate,
				habitStackId:
					modalLinkType === 'habitStack' && modalHabitStackId ? modalHabitStackId : undefined,
				taskItemId: modalLinkType === 'task' && modalTaskId ? modalTaskId : undefined
			};

			let savedEntry: JournalEntry;

			if (isEditing && editingEntry) {
				savedEntry = await updateJournalEntry(editingEntry.id, data);
				entries = entries.map((e) => (e.id === savedEntry.id ? savedEntry : e));
			} else {
				savedEntry = await createJournalEntry(data);
				entries = [savedEntry, ...entries];
				// Check for milestones that may have been unlocked
				milestoneStore.checkForNew();
			}

			// Upload pending images
			if (pendingImages.length > 0) {
				uploadingImages = true;
				for (const file of pendingImages) {
					try {
						const image = await uploadJournalImage(savedEntry.id, file);
						savedEntry = { ...savedEntry, images: [...savedEntry.images, image] };
					} catch {
						// Image upload failed silently - user can retry later
					}
				}
				entries = entries.map((e) => (e.id === savedEntry.id ? savedEntry : e));
				uploadingImages = false;
			}

			closeModal();
		} catch (e) {
			modalError = e instanceof Error ? e.message : 'Failed to save entry';
		} finally {
			modalLoading = false;
		}
	}

	async function handleDeleteImage(entry: JournalEntry, image: JournalImage) {
		if (!confirm(get(t)('journal.deleteImageConfirm'))) return;

		try {
			await deleteJournalImage(entry.id, image.id);
			const updatedEntry = {
				...entry,
				images: entry.images.filter((i) => i.id !== image.id)
			};
			entries = entries.map((e) => (e.id === entry.id ? updatedEntry : e));
			if (editingEntry?.id === entry.id) {
				editingEntry = updatedEntry;
			}
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.deleteImageFailed');
		}
	}

	async function handleDelete(id: string) {
		if (!confirm(get(t)('journal.deleteConfirm'))) return;

		try {
			await deleteJournalEntry(id);
			entries = entries.filter((e) => e.id !== id);
			if (editingEntry?.id === id) {
				closeModal();
			}
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('journal.errors.deleteFailed');
		}
	}

	function openLightbox(images: JournalImage[], index: number, event: Event) {
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
</script>

<div class="bg-warm-cream">
	<main class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4 sm:mb-6">
			<InfoOverlay 
				title={$t('journal.pageTitle')} 
				description={$t('journal.info.description')} 
			/>
			
		</div>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="card">
				<ErrorState message={error} onRetry={loadData} size="md" />
			</div>
		{:else}
			<div data-tour="journal-feed">
			<JournalViewContent
				{entries}
				mode="feed"
				{activeFilter}
				currentUserId={$auth.user?.id}
				onCreateEntry={openCreateModal}
				onEditEntry={(entry) => openEditModal(entry as JournalEntry)}
				onOpenLightbox={openLightbox}
				onFilterChange={handleFilterChange}
				onAddReaction={handleAddReaction}
				onRemoveReaction={handleRemoveReaction}
			/>
			</div>
		{/if}
	</main>

	<!-- Create/Edit Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-3 sm:p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-warm-paper rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
				<div class="p-4 sm:p-6">
					<div class="flex items-center justify-between mb-4 sm:mb-6">
						<h2 class="text-lg sm:text-xl font-semibold text-cocoa-800">
							{isEditing ? $t('journal.modal.editTitle') : $t('journal.modal.createTitle')}
						</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-cocoa-600 p-1" aria-label={$t('common.close')}>
							<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
						<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-4 whitespace-pre-line">
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
							<label for="title" class="block text-sm font-medium text-cocoa-700 mb-1">{$t('journal.form.title')} *</label>
							<input
								type="text"
								id="title"
								bind:value={modalTitle}
								placeholder={$t('journal.form.titlePlaceholder')}
								maxlength="255"
								class="input"
							/>
						</div>

						{#if isEditing}
						<div>
							<label for="entryDate" class="block text-sm font-medium text-cocoa-700 mb-1">{$t('journal.form.date')}</label>
							<input type="date" id="entryDate" bind:value={modalEntryDate} class="input" />
						</div>
						{/if}

						<div>
							<label for="description" class="block text-sm font-medium text-cocoa-700 mb-1"
								>{$t('journal.form.description')}</label
							>
							<textarea
								id="description"
								bind:value={modalDescription}
								rows="4"
								placeholder={$t('journal.form.descriptionPlaceholder')}
								class="input"
							></textarea>
						</div>

						<div>
							<span class="block text-sm font-medium text-cocoa-700 mb-2">{$t('journal.form.linkTo')}</span>
							<div class="flex gap-2 mb-2">
								<button
									type="button"
									onclick={() => {
										modalLinkType = 'none';
										modalHabitStackId = '';
										modalTaskId = '';
									}}
									class="px-3 py-1.5 rounded-full text-sm font-medium border-2 transition-colors {modalLinkType ===
									'none'
										? 'border-primary-500 bg-primary-50 text-primary-700'
										: 'border-primary-100 bg-warm-paper text-cocoa-700 hover:border-primary-200'}"
								>
									{$t('journal.form.linkNone')}
								</button>
								<button
									type="button"
									onclick={() => {
										modalLinkType = 'habitStack';
										modalTaskId = '';
									}}
									class="px-3 py-1.5 rounded-full text-sm font-medium border-2 transition-colors {modalLinkType ===
									'habitStack'
										? 'border-primary-500 bg-primary-50 text-primary-700'
										: 'border-primary-100 bg-warm-paper text-cocoa-700 hover:border-primary-200'}"
								>
									{$t('journal.form.linkHabitStack')}
								</button>
								<button
									type="button"
									onclick={() => {
										modalLinkType = 'task';
										modalHabitStackId = '';
									}}
									class="px-3 py-1.5 rounded-full text-sm font-medium border-2 transition-colors {modalLinkType ===
									'task'
										? 'border-primary-500 bg-primary-50 text-primary-700'
										: 'border-primary-100 bg-warm-paper text-cocoa-700 hover:border-primary-200'}"
								>
									{$t('journal.form.linkTask')}
								</button>
							</div>

							{#if modalLinkType === 'habitStack'}
								<select bind:value={modalHabitStackId} class="input">
									<option value="">{$t('journal.form.selectHabitStack')}</option>
									{#each habitStacks as stack (stack.id)}
										<option value={stack.id}>{stack.name}</option>
									{/each}
								</select>
							{:else if modalLinkType === 'task'}
								<select bind:value={modalTaskId} class="input">
									<option value="">{$t('journal.form.selectTask')}</option>
									{#each tasks as task (task.id)}
										<option value={task.id}>{task.goalTitle} - {task.title}</option>
									{/each}
								</select>
							{/if}
						</div>

						<!-- Images Section -->
						<div>
							<span class="block text-sm font-medium text-cocoa-700 mb-2">{$t('journal.images.title')}</span>

							<!-- Existing images (edit mode) -->
							{#if isEditing && editingEntry && editingEntry.images.length > 0}
								<div class="flex flex-wrap gap-2 mb-3">
									{#each editingEntry.images as image (image.id)}
										<div class="relative group">
											<img
												src={image.url}
												alt={image.fileName}
												class="w-20 h-20 object-cover rounded-2xl"
											/>
											<button
												type="button"
												onclick={() => handleDeleteImage(editingEntry!, image)}
												class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
												aria-label={$t('journal.images.delete')}
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
												aria-label={$t('journal.images.delete')}
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
											<span
												class="absolute bottom-1 left-1 text-xs bg-black bg-opacity-50 text-white px-1 rounded"
												>New</span
											>
										</div>
									{/each}
								</div>
							{/if}

							<!-- Upload button -->
							{#if (editingEntry?.images.length || 0) + pendingImages.length < 5}
								<label
									class="flex items-center justify-center w-full h-20 border-2 border-dashed border-primary-200 rounded-2xl cursor-pointer hover:border-gray-400 transition-colors {processingImages ? 'opacity-50 cursor-wait' : ''}"
								>
									<div class="text-center">
										{#if processingImages}
											<div class="animate-spin w-6 h-6 mx-auto border-2 border-primary-600 border-t-transparent rounded-full"></div>
											<span class="text-xs text-cocoa-500 mt-1">{$t('journal.images.processing')}</span>
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
											<span class="text-xs text-cocoa-500">{$t('journal.images.add')}</span>
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
								{$t('journal.images.hint')}
							</p>
						</div>
					</div>

					<div class="flex justify-between gap-3 mt-6 pt-4 border-t border-primary-100">
						<div>
							{#if isEditing && editingEntry}
								<button
									type="button"
									onclick={() => handleDelete(editingEntry!.id)}
									class="text-red-600 hover:text-red-700 text-sm"
								>
									{$t('common.delete')}
								</button>
							{/if}
						</div>
						<div class="flex gap-3">
							<button
								type="button"
								onclick={closeModal}
								class="btn-secondary"
								disabled={modalLoading || uploadingImages}
							>
								{$t('common.cancel')}
							</button>
							<button
								type="button"
								onclick={handleSubmit}
								class="btn-primary"
								disabled={modalLoading || uploadingImages}
							>
								{#if modalLoading}
									{$t('common.saving')}
								{:else if uploadingImages}
									{$t('journal.images.uploading')}
								{:else}
									{isEditing ? $t('common.saveChanges') : $t('journal.createFirst')}
								{/if}
							</button>
						</div>
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
