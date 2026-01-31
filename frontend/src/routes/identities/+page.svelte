<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { commandBar } from '$lib/stores/commandBar';
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getIdentities, createIdentity, updateIdentity, deleteIdentity } from '$lib/api/identities';
	import IdentityCard from '$lib/components/identities/IdentityCard.svelte';
	import IdentityForm from '$lib/components/identities/IdentityForm.svelte';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';
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
		loading = true;
		error = '';
		try {
			identities = await getIdentities();
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('errors.generic');
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
		if (!confirm(get(t)('identities.deleteConfirm'))) return;

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
	<main class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
		<!-- Page Header -->
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4 sm:mb-6">
			<InfoOverlay 
				title={$t('identities.pageTitle')} 
				description={$t('identities.info.description')} 
			/>
			<button onclick={openCreateModal} class="btn-primary text-sm w-full sm:w-auto justify-center">
				{$t('identities.newIdentity')}
			</button>
		</div>
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="card">
				<ErrorState message={error} onRetry={loadIdentities} size="md" />
			</div>
		{:else}
			{#if identities.length > 0}
				<div class="grid gap-4 sm:grid-cols-2" data-tour="identities-list">
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
				<div class="card p-8 sm:p-12">
					<div class="max-w-md mx-auto text-center">
						<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
							<span class="text-3xl">🎯</span>
						</div>
						<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('identities.emptyTitle')}</h3>
						<p class="text-gray-500 mb-4">{$t('identities.emptyDescription')}</p>
						<p class="text-gray-500 text-sm mb-6 flex items-center justify-center gap-1 flex-wrap">
							{$t('identities.emptyHowTo')}
							<button
								type="button"
								onclick={() => commandBar.open()}
								class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
								title="Open AI Assistant"
							>
								<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
								</svg>
							</button>
						</p>
						<button onclick={openCreateModal} class="btn-primary">{$t('identities.createFirst')}</button>
					</div>
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
							{isEditing ? $t('identities.edit') : $t('identities.newIdentity')}
						</h2>
						<button onclick={closeModal} class="text-gray-400 hover:text-gray-600" aria-label={$t('common.close')}>
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
								{$t('identities.delete')}
							</button>
						</div>
					{/if}
				</div>
			</div>
		</div>
	{/if}
</div>
