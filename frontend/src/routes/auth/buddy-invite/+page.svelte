<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { loginWithBuddyToken } from '$lib/api/buddies';

	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		const token = $page.url.searchParams.get('token');

		if (!token) {
			error = 'Invalid invitation link. No token provided.';
			loading = false;
			return;
		}

		try {
			const response = await loginWithBuddyToken(token);

			// Update auth store with the user
			auth.setUser(response.user);

			// Redirect to the inviter's buddy page
			goto(`/buddies/${response.inviterUserId}`);
		} catch (e) {
			if (e instanceof Error) {
				error = e.message;
			} else {
				error = 'Failed to process invitation. The link may be invalid or expired.';
			}
			loading = false;
		}
	});
</script>

<div class="min-h-screen bg-warm-cream flex items-center justify-center p-4">
	<div class="max-w-md w-full">
		{#if loading}
			<div class="card p-8 text-center">
				<div
					class="animate-spin w-12 h-12 border-4 border-primary-600 border-t-transparent rounded-full mx-auto mb-4"
				></div>
				<h2 class="text-xl font-semibold text-cocoa-800 mb-2">Processing Invitation</h2>
				<p class="text-cocoa-600">Please wait while we set up your account...</p>
			</div>
		{:else if error}
			<div class="card p-8 text-center">
				<div
					class="w-16 h-16 mx-auto mb-4 bg-red-100 rounded-full flex items-center justify-center"
				>
					<svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					</svg>
				</div>
				<h2 class="text-xl font-semibold text-cocoa-800 mb-2">Invitation Error</h2>
				<p class="text-cocoa-600 mb-6">{error}</p>
				<div class="space-y-3">
					<a href="/auth/login" class="btn-primary block w-full">Go to Login</a>
					<a href="/" class="btn-secondary block w-full">Go to Home</a>
				</div>
			</div>
		{/if}
	</div>
</div>
