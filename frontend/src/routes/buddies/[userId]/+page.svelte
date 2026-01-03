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
	import type { BuddyTodayViewResponse, BuddyJournalEntry } from '$lib/types';

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

	function getStreakEmoji(streak: number): string {
		if (streak >= 30) return '🏆';
		if (streak >= 14) return '🔥';
		if (streak >= 7) return '💪';
		if (streak >= 3) return '⚡';
		return '';
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
		showModal = true;
	}

	function closeModal() {
		showModal = false;
		modalError = '';
		pendingImages = [];
	}

	function handleFileSelect(e: Event) {
		const input = e.target as HTMLInputElement;
		if (input.files) {
			const newFiles = Array.from(input.files);
			const availableSlots = 5 - pendingImages.length;

			if (newFiles.length > availableSlots) {
				modalError = `Can only add ${availableSlots} more image(s)`;
				pendingImages = [...pendingImages, ...newFiles.slice(0, availableSlots)];
			} else {
				pendingImages = [...pendingImages, ...newFiles];
			}
		}
		input.value = '';
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

					<!-- Identity Feedback -->
					{#if todayData.identityFeedback.length > 0}
						<div class="card p-4 mb-6 bg-gradient-to-r from-primary-50 to-purple-50">
							<div class="flex items-center gap-2 mb-2">
								<span class="text-lg">🎯</span>
								<h2 class="font-semibold text-gray-900">Identity Votes Today</h2>
							</div>
							<div class="space-y-2">
								{#each todayData.identityFeedback as feedback (feedback.id)}
									<p class="text-sm text-gray-700">{feedback.reinforcementMessage}</p>
								{/each}
							</div>
						</div>
					{/if}

					<!-- Habit Stacks -->
					<div class="card p-5 mb-6">
						<div class="flex items-center justify-between mb-4">
							<div class="flex items-center gap-2">
								<span class="text-lg">🔗</span>
								<h2 class="font-semibold text-gray-900">Habit Stacks</h2>
							</div>
						</div>

						{#if todayData.habitStacks.length === 0}
							<p class="text-gray-500 text-sm text-center py-4">No habit stacks</p>
						{:else}
							<div class="space-y-4">
								{#each todayData.habitStacks as stack (stack.id)}
									<div class="border border-gray-200 rounded-lg p-4">
										<div class="flex items-center justify-between mb-3">
											<div>
												<h3 class="font-medium text-gray-900">{stack.name}</h3>
												{#if stack.triggerCue}
													<p class="text-xs text-gray-500">{stack.triggerCue}</p>
												{/if}
											</div>
											<div class="text-sm text-gray-600">
												{stack.completedCount}/{stack.totalCount}
											</div>
										</div>

										<!-- Progress bar -->
										<div class="h-2 bg-gray-100 rounded-full mb-3 overflow-hidden">
											<div
												class="h-full bg-primary-500 rounded-full transition-all"
												style="width: {stack.totalCount > 0 ? (stack.completedCount / stack.totalCount) * 100 : 0}%"
											></div>
										</div>

										<!-- Habit items -->
										<div class="space-y-2">
											{#each stack.items as item (item.id)}
												<div class="flex items-center gap-3 py-1">
													<div
														class="w-5 h-5 rounded-full border-2 flex items-center justify-center {item.isCompletedToday
															? 'bg-primary-500 border-primary-500'
															: 'border-gray-300'}"
													>
														{#if item.isCompletedToday}
															<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
																<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
															</svg>
														{/if}
													</div>
													<span class="text-sm {item.isCompletedToday ? 'text-gray-900' : 'text-gray-500'}">
														{item.habitDescription}
													</span>
													{#if item.currentStreak > 0}
														<span class="text-xs">{getStreakEmoji(item.currentStreak)} {item.currentStreak}</span>
													{/if}
												</div>
											{/each}
										</div>
									</div>
								{/each}
							</div>
						{/if}
					</div>

					<!-- Upcoming Tasks -->
					<div class="card p-5 mb-6">
						<div class="flex items-center gap-2 mb-4">
							<span class="text-lg">📋</span>
							<h2 class="font-semibold text-gray-900">Upcoming Tasks</h2>
						</div>

						{#if todayData.upcomingTasks.length === 0}
							<p class="text-gray-500 text-sm text-center py-4">No upcoming tasks</p>
						{:else}
							<div class="space-y-2">
								{#each todayData.upcomingTasks as task (task.id)}
									<div class="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
										<div class="w-5 h-5 rounded-full border-2 border-gray-300 mt-0.5"></div>
										<div class="flex-1 min-w-0">
											<p class="font-medium text-gray-900">{task.title}</p>
											<p class="text-xs text-gray-500">{task.goalTitle}</p>
											{#if task.dueDate}
												<p class="text-xs text-gray-400 mt-1">Due: {task.dueDate}</p>
											{/if}
										</div>
									</div>
								{/each}
							</div>
						{/if}
					</div>

					<!-- Completed Tasks -->
					{#if todayData.completedTasks.length > 0}
						<div class="card p-5">
							<div class="flex items-center gap-2 mb-4">
								<span class="text-lg">✅</span>
								<h2 class="font-semibold text-gray-900">Completed Today</h2>
							</div>

							<div class="space-y-2">
								{#each todayData.completedTasks as task (task.id)}
									<div class="flex items-start gap-3 p-3 bg-green-50 rounded-lg">
										<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center mt-0.5">
											<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
											</svg>
										</div>
										<div class="flex-1 min-w-0">
											<p class="font-medium text-gray-900 line-through">{task.title}</p>
											<p class="text-xs text-gray-500">{task.goalTitle}</p>
										</div>
									</div>
								{/each}
							</div>
						</div>
					{/if}
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
											{#each entry.images as image (image.id)}
												<img
													src={image.url}
													alt={image.fileName}
													class="w-20 h-20 object-cover rounded-lg"
												/>
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
							class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4"
						>
							{modalError}
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
									class="flex items-center justify-center w-full h-20 border-2 border-dashed border-gray-300 rounded-lg cursor-pointer hover:border-gray-400 transition-colors"
								>
									<div class="text-center">
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
									</div>
									<input
										type="file"
										accept="image/jpeg,image/png,image/gif,image/webp"
										multiple
										onchange={handleFileSelect}
										class="hidden"
									/>
								</label>
							{/if}
							<p class="text-xs text-gray-500 mt-1">Max 5 images, 5MB each</p>
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
</div>
