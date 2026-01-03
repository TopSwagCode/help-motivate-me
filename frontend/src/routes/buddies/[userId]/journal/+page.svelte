<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { getBuddyJournal, createBuddyJournalEntry, getBuddyTodayView } from '$lib/api/buddies';
	import type { BuddyJournalEntry, BuddyTodayViewResponse } from '$lib/types';

	let entries = $state<BuddyJournalEntry[]>([]);
	let userInfo = $state<{ displayName: string } | null>(null);
	let loading = $state(true);
	let error = $state('');

	// Create modal state
	let showModal = $state(false);
	let modalTitle = $state('');
	let modalDescription = $state('');
	let modalLoading = $state(false);
	let modalError = $state('');

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
		try {
			// Get user info from today view
			const todayView = await getBuddyTodayView($page.params.userId!);
			userInfo = { displayName: todayView.userDisplayName };

			// Get journal entries
			entries = await getBuddyJournal($page.params.userId!);
		} catch (e) {
			if (e instanceof Error && e.message.includes('403')) {
				error = "You do not have permission to view this user's journal.";
			} else {
				error = e instanceof Error ? e.message : 'Failed to load journal';
			}
		} finally {
			loading = false;
		}
	}

	function openCreateModal() {
		modalTitle = '';
		modalDescription = '';
		modalError = '';
		showModal = true;
	}

	function closeModal() {
		showModal = false;
		modalError = '';
	}

	async function handleSubmit() {
		if (!modalTitle.trim()) {
			modalError = 'Title is required';
			return;
		}

		modalLoading = true;
		modalError = '';

		try {
			const newEntry = await createBuddyJournalEntry($page.params.userId!, {
				title: modalTitle.trim(),
				description: modalDescription.trim() || undefined,
				entryDate: new Date().toISOString().split('T')[0]
			});

			entries = [newEntry, ...entries];
			closeModal();
		} catch (e) {
			modalError = e instanceof Error ? e.message : 'Failed to create entry';
		} finally {
			modalLoading = false;
		}
	}

	function formatDate(dateStr: string): string {
		return new Date(dateStr + 'T12:00:00').toLocaleDateString('en-US', {
			weekday: 'long',
			year: 'numeric',
			month: 'long',
			day: 'numeric'
		});
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="card p-8 text-center">
				<p class="text-red-600 mb-4">{error}</p>
				<a href="/buddies" class="btn-primary">Back to Buddies</a>
			</div>
		{:else}
			<!-- Header -->
			<div class="flex items-center justify-between mb-6">
				<div>
					<a
						href="/buddies/{$page.params.userId}"
						class="text-sm text-gray-500 hover:text-gray-700 mb-1 inline-block"
					>
						&larr; Back to {userInfo?.displayName}'s Progress
					</a>
					<h1 class="text-2xl font-bold text-gray-900">{userInfo?.displayName}'s Journal</h1>
				</div>
				<button onclick={openCreateModal} class="btn-primary text-sm">
					Write Encouragement
				</button>
			</div>

			<!-- Info Banner -->
			<div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
				<p class="text-sm text-blue-800">
					As an accountability buddy, you can leave encouraging notes and messages here. Your entries
					will be visible to {userInfo?.displayName} and will help keep them motivated!
				</p>
			</div>

			<!-- Entries List -->
			{#if entries.length === 0}
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
						Be the first to leave an encouraging note for {userInfo?.displayName}!
					</p>
					<button onclick={openCreateModal} class="btn-primary">Write Encouragement</button>
				</div>
			{:else}
				<div class="space-y-4">
					{#each entries as entry (entry.id)}
						<div class="card p-5">
							<div class="flex items-start justify-between mb-2">
								<div>
									<h3 class="font-semibold text-gray-900 text-lg">{entry.title}</h3>
									<p class="text-sm text-gray-500">{formatDate(entry.entryDate)}</p>
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
	</main>

	<!-- Create Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-white rounded-xl shadow-xl max-w-lg w-full">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">Write Encouragement</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-gray-600">
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
					</div>

					<div class="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200">
						<button
							type="button"
							onclick={closeModal}
							class="btn-secondary"
							disabled={modalLoading}
						>
							Cancel
						</button>
						<button
							type="button"
							onclick={handleSubmit}
							class="btn-primary"
							disabled={modalLoading}
						>
							{modalLoading ? 'Sending...' : 'Send'}
						</button>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>
