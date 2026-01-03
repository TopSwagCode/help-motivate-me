<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getBuddyRelationships, inviteBuddy, removeBuddy, leaveBuddy } from '$lib/api/buddies';
	import type { BuddyRelationshipsResponse } from '$lib/types';

	let relationships = $state<BuddyRelationshipsResponse | null>(null);
	let loading = $state(true);
	let error = $state('');

	// Invite form
	let inviteEmail = $state('');
	let inviting = $state(false);
	let inviteError = $state('');
	let inviteSuccess = $state('');

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadRelationships();
	});

	async function loadRelationships() {
		try {
			relationships = await getBuddyRelationships();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load buddy relationships';
		} finally {
			loading = false;
		}
	}

	async function handleInvite() {
		if (!inviteEmail.trim()) {
			inviteError = 'Please enter an email address';
			return;
		}

		inviting = true;
		inviteError = '';
		inviteSuccess = '';

		try {
			const newBuddy = await inviteBuddy(inviteEmail.trim());
			if (relationships) {
				relationships = {
					...relationships,
					myBuddies: [...relationships.myBuddies, newBuddy]
				};
			}
			inviteEmail = '';
			inviteSuccess = 'Invitation sent successfully!';
			setTimeout(() => (inviteSuccess = ''), 3000);
		} catch (e) {
			inviteError = e instanceof Error ? e.message : 'Failed to send invitation';
		} finally {
			inviting = false;
		}
	}

	async function handleRemoveBuddy(id: string) {
		if (!confirm('Are you sure you want to remove this accountability buddy?')) return;

		try {
			await removeBuddy(id);
			if (relationships) {
				relationships = {
					...relationships,
					myBuddies: relationships.myBuddies.filter((b) => b.id !== id)
				};
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to remove buddy';
		}
	}

	async function handleLeaveBuddy(userId: string, userName: string) {
		if (!confirm(`Are you sure you want to stop being ${userName}'s accountability buddy?`)) return;

		try {
			await leaveBuddy(userId);
			if (relationships) {
				relationships = {
					...relationships,
					buddyingFor: relationships.buddyingFor.filter((b) => b.userId !== userId)
				};
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to leave buddy relationship';
		}
	}

	function formatDate(dateStr: string): string {
		return new Date(dateStr).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<h1 class="text-2xl font-bold text-gray-900 mb-6">Accountability Buddies</h1>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
				<button onclick={() => (error = '')} class="float-right text-red-500 hover:text-red-700"
					>&times;</button
				>
			</div>
		{:else if relationships}
			<!-- Invite New Buddy Section -->
			<div class="card p-6 mb-8">
				<h2 class="text-lg font-semibold text-gray-900 mb-4">Invite a New Accountability Buddy</h2>
				<p class="text-sm text-gray-600 mb-4">
					Enter the email address of someone you'd like to help keep you accountable. They'll receive
					an email invitation with instructions on how to view your progress.
				</p>

				<form onsubmit={(e) => { e.preventDefault(); handleInvite(); }} class="flex gap-3">
					<input
						type="email"
						bind:value={inviteEmail}
						placeholder="buddy@example.com"
						class="input flex-1"
						disabled={inviting}
					/>
					<button type="submit" class="btn-primary" disabled={inviting}>
						{inviting ? 'Sending...' : 'Send Invite'}
					</button>
				</form>

				{#if inviteError}
					<p class="text-red-600 text-sm mt-2">{inviteError}</p>
				{/if}
				{#if inviteSuccess}
					<p class="text-green-600 text-sm mt-2">{inviteSuccess}</p>
				{/if}
			</div>

			<!-- My Accountability Buddies -->
			<div class="card p-6 mb-8">
				<h2 class="text-lg font-semibold text-gray-900 mb-4">My Accountability Buddies</h2>
				<p class="text-sm text-gray-600 mb-4">
					These people can view your daily progress and leave encouraging notes in your journal.
				</p>

				{#if relationships.myBuddies.length === 0}
					<div class="text-center py-8 text-gray-500">
						<p>You haven't added any accountability buddies yet.</p>
						<p class="text-sm mt-2">Invite someone using the form above!</p>
					</div>
				{:else}
					<div class="space-y-3">
						{#each relationships.myBuddies as buddy (buddy.id)}
							<div
								class="flex items-center justify-between p-4 bg-gray-50 rounded-lg border border-gray-200"
							>
								<div>
									<p class="font-medium text-gray-900">{buddy.buddyDisplayName}</p>
									<p class="text-sm text-gray-500">{buddy.buddyEmail}</p>
									<p class="text-xs text-gray-400 mt-1">Added {formatDate(buddy.createdAt)}</p>
								</div>
								<button
									onclick={() => handleRemoveBuddy(buddy.id)}
									class="text-red-600 hover:text-red-700 text-sm font-medium"
								>
									Remove
								</button>
							</div>
						{/each}
					</div>
				{/if}
			</div>

			<!-- I'm Accountability Buddy For -->
			<div class="card p-6">
				<h2 class="text-lg font-semibold text-gray-900 mb-4">I'm Accountability Buddy For</h2>
				<p class="text-sm text-gray-600 mb-4">
					These people have added you as their accountability buddy. You can view their progress and
					encourage them!
				</p>

				{#if relationships.buddyingFor.length === 0}
					<div class="text-center py-8 text-gray-500">
						<p>No one has added you as their accountability buddy yet.</p>
					</div>
				{:else}
					<div class="space-y-3">
						{#each relationships.buddyingFor as buddyFor (buddyFor.id)}
							<div
								class="flex items-center justify-between p-4 bg-gray-50 rounded-lg border border-gray-200"
							>
								<div>
									<p class="font-medium text-gray-900">{buddyFor.userDisplayName}</p>
									<p class="text-sm text-gray-500">{buddyFor.userEmail}</p>
									<p class="text-xs text-gray-400 mt-1">Since {formatDate(buddyFor.createdAt)}</p>
								</div>
								<div class="flex gap-2">
									<a
										href="/buddies/{buddyFor.userId}"
										class="btn-primary text-sm"
									>
										Visit
									</a>
									<button
										onclick={() => handleLeaveBuddy(buddyFor.userId, buddyFor.userDisplayName)}
										class="text-gray-500 hover:text-red-600 text-sm"
									>
										Leave
									</button>
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>
		{/if}
	</main>
</div>
