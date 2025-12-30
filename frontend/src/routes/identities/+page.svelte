<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getIdentities, createIdentity, updateIdentity, deleteIdentity } from '$lib/api/identities';
	import IdentityCard from '$lib/components/identities/IdentityCard.svelte';
	import IdentityForm from '$lib/components/identities/IdentityForm.svelte';
	import type { Identity, CreateIdentityRequest, UpdateIdentityRequest } from '$lib/types';

	let identities = $state<Identity[]>([]);
	let loading = $state(true);
	let error = $state('');
	let showModal = $state(false);
	let editingIdentity = $state<Identity | null>(null);

	const isEditing = $derived(editingIdentity !== null);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadIdentities();
	});

	async function loadIdentities() {
		try {
			identities = await getIdentities();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load identities';
		} finally {
			loading = false;
		}
	}

	function openCreateModal() {
		editingIdentity = null;
		showModal = true;
	}

	function openEditModal(identity: Identity) {
		editingIdentity = identity;
		showModal = true;
	}

	function closeModal() {
		showModal = false;
		editingIdentity = null;
	}

	async function handleSubmit(data: CreateIdentityRequest | UpdateIdentityRequest) {
		if (isEditing && editingIdentity) {
			const updated = await updateIdentity(editingIdentity.id, data);
			identities = identities.map((i) => (i.id === updated.id ? updated : i));
		} else {
			const identity = await createIdentity(data as CreateIdentityRequest);
			identities = [identity, ...identities];
		}
		closeModal();
	}

	async function handleDelete(id: string) {
		if (!confirm('Are you sure you want to delete this identity?')) return;

		try {
			await deleteIdentity(id);
			identities = identities.filter((i) => i.id !== id);
			if (editingIdentity?.id === id) {
				closeModal();
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete identity';
		}
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<!-- Page Header -->
		<div class="flex items-center justify-between mb-6">
			<h1 class="text-2xl font-bold text-gray-900">My Identities</h1>
			<button onclick={openCreateModal} class="btn-primary text-sm">
				New Identity
			</button>
		</div>
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
			</div>
		{:else}
			<!-- Info Card -->
			<div class="card p-6 mb-6 bg-gradient-to-r from-primary-50 to-indigo-50">
  <h2 class="font-semibold text-gray-900 mb-2">Identity-Based Habits</h2>
  <p class="text-sm text-gray-600 mb-4">
    Identity-based habits focus on who you are becoming, not just what you’re doing.
    Each small action is a vote for the type of person you want to be, and consistency
    slowly reshapes how you see yourself. When your habits align with your identity,
    progress feels natural instead of forced.
  </p>

  <div class="mt-4 border-t border-indigo-100 pt-4">
    <h3 class="text-sm font-medium text-gray-800 mb-1">Create Your Own Identities</h3>
    <p class="text-sm text-gray-600">
      Instead of chasing outcomes, define the identities you want to live by—such as
      “I am someone who learns daily” or “I am someone who keeps promises to myself.”
      Let your habits become evidence that these identities are true.
    </p>
  </div>
</div>

			{#if identities.length > 0}
				<div class="grid gap-4 sm:grid-cols-2">
					{#each identities as identity (identity.id)}
						<button
							type="button"
							onclick={() => openEditModal(identity)}
							class="relative group text-left w-full"
						>
							<IdentityCard {identity} />
						</button>
					{/each}
				</div>
			{:else}
				<div class="card p-12 text-center">
					<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
						<span class="text-3xl">🎯</span>
					</div>
					<h3 class="text-lg font-medium text-gray-900 mb-2">No identities yet</h3>
					<p class="text-gray-500 mb-6">Create your first identity to start building habits.</p>
					<button onclick={openCreateModal} class="btn-primary">Create Identity</button>
				</div>
			{/if}
		{/if}
	</main>

	<!-- Create/Edit Modal -->
	{#if showModal}
		<div
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
			role="dialog"
			aria-modal="true"
		>
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">
							{isEditing ? 'Edit Identity' : 'New Identity'}
						</h2>
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

					<IdentityForm
						identity={editingIdentity ?? undefined}
						onsubmit={handleSubmit}
						oncancel={closeModal}
					/>

					{#if isEditing && editingIdentity}
						<div class="mt-4 pt-4 border-t border-gray-200">
							<button
								type="button"
								onclick={() => handleDelete(editingIdentity!.id)}
								class="text-red-600 hover:text-red-700 text-sm"
							>
								Delete Identity
							</button>
						</div>
					{/if}
				</div>
			</div>
		</div>
	{/if}
</div>
