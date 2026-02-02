<script lang="ts">
	import { changePassword } from '$lib/api/settings';

	let currentPassword = $state('');
	let newPassword = $state('');
	let confirmPassword = $state('');
	let loading = $state(false);
	let error = $state('');
	let success = $state('');

	const passwordsMatch = $derived(newPassword === confirmPassword);
	const isValid = $derived(
		currentPassword.length > 0 && newPassword.length >= 8 && passwordsMatch
	);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!isValid) return;

		loading = true;
		error = '';
		success = '';

		try {
			await changePassword({
				currentPassword,
				newPassword
			});
			success = 'Password changed successfully';
			currentPassword = '';
			newPassword = '';
			confirmPassword = '';
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to change password';
		} finally {
			loading = false;
		}
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-cocoa-800 mb-4">Change Password</h2>

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

		<div>
			<label for="currentPassword" class="label">Current Password</label>
			<input
				id="currentPassword"
				type="password"
				bind:value={currentPassword}
				required
				autocomplete="current-password"
				class="input"
			/>
		</div>

		<div>
			<label for="newPassword" class="label">New Password</label>
			<input
				id="newPassword"
				type="password"
				bind:value={newPassword}
				required
				minlength="8"
				autocomplete="new-password"
				class="input"
			/>
			<p class="mt-1 text-xs text-cocoa-500">Must be at least 8 characters</p>
		</div>

		<div>
			<label for="confirmPassword" class="label">Confirm New Password</label>
			<input
				id="confirmPassword"
				type="password"
				bind:value={confirmPassword}
				required
				autocomplete="new-password"
				class="input {confirmPassword && !passwordsMatch ? 'border-red-300' : ''}"
			/>
			{#if confirmPassword && !passwordsMatch}
				<p class="mt-1 text-xs text-red-600">Passwords do not match</p>
			{/if}
		</div>

		<button type="submit" disabled={loading || !isValid} class="btn-primary">
			{loading ? 'Changing...' : 'Change Password'}
		</button>
	</form>
</div>
