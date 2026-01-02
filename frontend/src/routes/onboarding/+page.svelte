<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { completeOnboarding } from '$lib/api/onboarding';
	import OnboardingWizard from '$lib/components/onboarding/OnboardingWizard.svelte';

	let loading = $state(true);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		// If user already completed onboarding, redirect to today
		if ($auth.user.hasCompletedOnboarding) {
			goto('/today');
			return;
		}

		loading = false;
	});

	async function handleComplete() {
		try {
			const updatedUser = await completeOnboarding();
			auth.updateUser(updatedUser);
			goto('/today?welcome=true');
		} catch (e) {
			console.error('Failed to complete onboarding:', e);
			// Still redirect even if the API call fails
			goto('/today?welcome=true');
		}
	}

	async function handleSkip() {
		try {
			const updatedUser = await completeOnboarding();
			auth.updateUser(updatedUser);
			goto('/today');
		} catch (e) {
			console.error('Failed to complete onboarding:', e);
			goto('/today');
		}
	}
</script>

<svelte:head>
	<title>Get Started - HelpMotivateMe</title>
</svelte:head>

{#if loading}
	<div class="min-h-screen bg-gray-50 flex items-center justify-center">
		<div
			class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
		></div>
	</div>
{:else}
	<OnboardingWizard oncomplete={handleComplete} onskip={handleSkip} />
{/if}
