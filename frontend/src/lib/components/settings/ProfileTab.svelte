<script lang="ts">
	import { goto } from '$app/navigation';
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { auth } from '$lib/stores/auth';
	import { updateProfile } from '$lib/api/settings';
	import { deleteAccount } from '$lib/api/auth';

	let displayName = $state($auth.user?.displayName ?? '');
	let loading = $state(false);
	let deleteLoading = $state(false);
	let showDeleteModal = $state(false);
	let deletePassword = $state('');
	let deleteConfirmation = $state('');
	let error = $state('');
	let success = $state('');
	let deleteError = $state('');

	async function handleSubmit(e: Event) {
		e.preventDefault();
		loading = true;
		error = '';
		success = '';

		try {
			const updatedUser = await updateProfile({
				displayName: displayName.trim() || undefined
			});
			auth.updateUser(updatedUser);
			success = get(t)('settings.profile.updated');
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('errors.generic');
		} finally {
			loading = false;
		}
	}

	function openDeleteModal() {
		showDeleteModal = true;
		deletePassword = '';
		deleteConfirmation = '';
		deleteError = '';
	}

	function closeDeleteModal() {
		showDeleteModal = false;
		deletePassword = '';
		deleteConfirmation = '';
		deleteError = '';
	}

	async function handleDeleteAccount() {
		if (deleteConfirmation !== 'DELETE') {
			deleteError = get(t)('settings.deleteAccount.confirmationRequired');
			return;
		}

		deleteLoading = true;
		deleteError = '';

		try {
			// Only send password if user has one set
			const password = $auth.user?.hasPassword ? deletePassword : undefined;
			await deleteAccount(password);
			// Redirect to home after deletion
			window.location.href = '/';
		} catch (e) {
			deleteError = e instanceof Error ? e.message : get(t)('settings.deleteAccount.failed');
			deleteLoading = false;
		}
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('settings.profile.title')}</h2>

	<form onsubmit={handleSubmit} class="space-y-4 max-w-md">
		{#if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm">
				{error}
			</div>
		{/if}

		{#if success}
			<div class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-2xl text-sm">
				{success}
			</div>
		{/if}

		<!-- Read-only fields -->
		<div>
			<label for="email" class="label">{$t('settings.profile.email')}</label>
			<input
				id="email"
				type="email"
				value={$auth.user?.email}
				disabled
				class="input bg-warm-cream text-cocoa-500 cursor-not-allowed"
			/>
		</div>

		<!-- Editable field -->
		<div>
			<label for="displayName" class="label">{$t('settings.profile.displayName')}</label>
			<input
				id="displayName"
				type="text"
				bind:value={displayName}
				maxlength="100"
				placeholder={$t('settings.profile.displayNamePlaceholder')}
				class="input"
			/>
		</div>

		<button type="submit" disabled={loading} class="btn-primary">
			{loading ? $t('settings.profile.saving') : $t('settings.profile.save')}
		</button>
	</form>

	<!-- Linked Accounts Section -->
	<div class="mt-8 pt-6 border-t border-primary-100">
		<h3 class="text-md font-medium text-cocoa-800 mb-3">{$t('settings.linkedAccounts.title')}</h3>
		{#if $auth.user?.linkedProviders.length}
			<ul class="space-y-2">
				{#each $auth.user.linkedProviders as provider}
					<li class="flex items-center gap-2 text-sm text-cocoa-600">
						{#if provider === 'GitHub'}
							<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
								<path
									d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
								/>
							</svg>
						{/if}
						{provider}
					</li>
				{/each}
			</ul>
		{:else}
			<p class="text-sm text-cocoa-500">{$t('settings.linkedAccounts.noAccounts')}</p>
		{/if}
	</div>

	<!-- Danger Zone -->
	<div class="mt-8 pt-6 border-t border-red-200">
		<h3 class="text-md font-medium text-red-600 mb-2">{$t('settings.deleteAccount.title')}</h3>
		<p class="text-sm text-cocoa-500 mb-3">
			{$t('settings.deleteAccount.description')}
		</p>
		<button
			onclick={openDeleteModal}
			class="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-md transition-colors"
		>
			{$t('settings.deleteAccount.button')}
		</button>
	</div>
</div>

<!-- Delete Account Modal -->
{#if showDeleteModal}
	<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
		<div class="bg-warm-paper rounded-2xl shadow-xl max-w-md w-full p-6">
			<h3 class="text-lg font-bold text-red-600 mb-4">{$t('settings.deleteAccount.modalTitle')}</h3>

			<div class="bg-red-50 border border-red-200 rounded-2xl p-4 mb-4">
				<p class="text-sm text-red-700">
					{$t('settings.deleteAccount.warning')}
				</p>
			</div>

			{#if deleteError}
				<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-4">
					{deleteError}
				</div>
			{/if}

			<div class="space-y-4">
				{#if $auth.user?.hasPassword}
					<div>
						<label for="deletePassword" class="label">{$t('settings.deleteAccount.passwordLabel')}</label>
						<input
							id="deletePassword"
							type="password"
							bind:value={deletePassword}
							class="input"
							placeholder={$t('settings.deleteAccount.passwordPlaceholder')}
						/>
					</div>
				{/if}

				<div>
					<label for="deleteConfirmation" class="label">
						{$t('settings.deleteAccount.confirmLabel')}
					</label>
					<input
						id="deleteConfirmation"
						type="text"
						bind:value={deleteConfirmation}
						class="input"
						placeholder="DELETE"
					/>
				</div>
			</div>

			<div class="mt-6 flex gap-3 justify-end">
				<button
					onclick={closeDeleteModal}
					disabled={deleteLoading}
					class="btn-secondary"
				>
					{$t('common.cancel')}
				</button>
				<button
					onclick={handleDeleteAccount}
					disabled={deleteLoading || deleteConfirmation !== 'DELETE'}
					class="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 disabled:bg-red-300 rounded-md transition-colors"
				>
					{deleteLoading ? $t('settings.deleteAccount.deleting') : $t('settings.deleteAccount.confirmButton')}
				</button>
			</div>
		</div>
	</div>
{/if}
