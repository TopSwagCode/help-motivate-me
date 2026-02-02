<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		// Check if user needs onboarding
		if (!$auth.user.hasCompletedOnboarding) {
			goto('/onboarding');
			return;
		}

		// Redirect to Today as the default view
		goto('/today');
	});
</script>

<div class="min-h-screen bg-warm-cream flex items-center justify-center">
	<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
</div>
