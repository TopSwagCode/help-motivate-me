<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { get } from 'svelte/store';
	import { auth } from '$lib/stores/auth';

	let error = $state('');

	onMount(async () => {
		const errorParam = $page.url.searchParams.get('error');

		if (errorParam) {
			error = errorParam === 'auth_failed'
				? 'Authentication failed. Please try again.'
				: `An error occurred: ${errorParam}`;
			return;
		}

		// Refresh auth state and redirect
		await auth.init();
		
		// Check if user needs onboarding
		const authState = get(auth);
		if (authState.user && !authState.user.hasCompletedOnboarding) {
			goto('/onboarding');
		} else {
			goto('/today');
		}
	});
</script>

<div class="min-h-screen flex items-center justify-center">
	{#if error}
		<div class="card p-8 max-w-md text-center">
			<div class="text-red-600 mb-4">
				<svg class="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
				</svg>
			</div>
			<h2 class="text-xl font-semibold text-cocoa-800 mb-2">Authentication Error</h2>
			<p class="text-cocoa-600 mb-6">{error}</p>
			<a href="/auth/login" class="btn-primary">Back to Login</a>
		</div>
	{:else}
		<div class="text-center">
			<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full mx-auto mb-4"></div>
			<p class="text-cocoa-600">Completing sign in...</p>
		</div>
	{/if}
</div>
