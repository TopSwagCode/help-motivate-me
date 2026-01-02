<script lang="ts">
	import { auth } from '$lib/stores/auth';
	import { updateProfile } from '$lib/api/settings';

	let displayName = $state($auth.user?.displayName ?? '');
	let loading = $state(false);
	let error = $state('');
	let success = $state('');

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
			success = 'Profile updated successfully';
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update profile';
		} finally {
			loading = false;
		}
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-gray-900 mb-4">Profile Settings</h2>

	<form onsubmit={handleSubmit} class="space-y-4 max-w-md">
		{#if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
				{error}
			</div>
		{/if}

		{#if success}
			<div class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg text-sm">
				{success}
			</div>
		{/if}

		<!-- Read-only fields -->
		<div>
			<label class="label">Username</label>
			<input
				type="text"
				value={$auth.user?.username}
				disabled
				class="input bg-gray-50 text-gray-500 cursor-not-allowed"
			/>
		</div>

		<div>
			<label class="label">Email</label>
			<input
				type="email"
				value={$auth.user?.email}
				disabled
				class="input bg-gray-50 text-gray-500 cursor-not-allowed"
			/>
		</div>

		<!-- Editable field -->
		<div>
			<label for="displayName" class="label">Display Name</label>
			<input
				id="displayName"
				type="text"
				bind:value={displayName}
				maxlength="100"
				placeholder="Enter a display name"
				class="input"
			/>
		</div>

		<button type="submit" disabled={loading} class="btn-primary">
			{loading ? 'Saving...' : 'Save Changes'}
		</button>
	</form>

	<!-- Linked Accounts Section -->
	<div class="mt-8 pt-6 border-t border-gray-200">
		<h3 class="text-md font-medium text-gray-900 mb-3">Linked Accounts</h3>
		{#if $auth.user?.linkedProviders.length}
			<ul class="space-y-2">
				{#each $auth.user.linkedProviders as provider}
					<li class="flex items-center gap-2 text-sm text-gray-600">
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
			<p class="text-sm text-gray-500">No external accounts linked</p>
		{/if}
	</div>
</div>
