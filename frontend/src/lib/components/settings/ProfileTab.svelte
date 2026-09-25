<script lang="ts">
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { auth } from '$lib/stores/auth';
	import { updateProfile } from '$lib/api/settings';

	let displayName = $state($auth.user?.displayName ?? '');
	let loading = $state(false);
	let error = $state('');
	let success = $state('');

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		loading = true;
		error = '';
		success = '';

		try {
			const updatedUser = await updateProfile({ displayName: displayName.trim() || null });
			auth.updateUser(updatedUser);
			success = get(t)('settings.profile.updated');
		} catch (caught) {
			error = caught instanceof Error ? caught.message : get(t)('errors.generic');
		} finally {
			loading = false;
		}
	}
</script>

<div>
	<h2 class="mb-4 text-lg font-semibold text-cocoa-800">{$t('settings.profile.title')}</h2>
	<form onsubmit={handleSubmit} class="max-w-md space-y-4">
		{#if error}
			<p class="rounded border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">{error}</p>
		{/if}
		{#if success}
			<p class="rounded border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">{success}</p>
		{/if}

		<div>
			<label for="username" class="label">{$t('auth.login.username')}</label>
			<input id="username" value={$auth.user?.username} disabled class="input cursor-not-allowed bg-warm-cream text-cocoa-500" />
		</div>
		<div>
			<label for="displayName" class="label">{$t('settings.profile.displayName')}</label>
			<input id="displayName" type="text" bind:value={displayName} maxlength="100" placeholder={$t('settings.profile.displayNamePlaceholder')} class="input" />
		</div>
		<button type="submit" disabled={loading} class="btn-primary">
			{loading ? $t('settings.profile.saving') : $t('settings.profile.save')}
		</button>
	</form>
</div>
