<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from 'svelte-i18n';
	import { auth } from '$lib/stores/auth';
	import ProfileTab from '$lib/components/settings/ProfileTab.svelte';
	import PasswordTab from '$lib/components/settings/PasswordTab.svelte';
	import MembershipTab from '$lib/components/settings/MembershipTab.svelte';
	import LanguageTab from '$lib/components/settings/LanguageTab.svelte';

	type Tab = 'profile' | 'password' | 'membership' | 'language';

	let activeTab = $state<Tab>('profile');
	let loading = $state(true);

	// Read tab from URL hash
	$effect(() => {
		const hash = $page.url.hash.slice(1) as Tab;
		if (['profile', 'password', 'membership', 'language'].includes(hash)) {
			activeTab = hash;
		}
	});

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}
		if (!$auth.user) {
			goto('/auth/login');
			return;
		}
		loading = false;
	});

	function setTab(tab: Tab) {
		activeTab = tab;
		window.location.hash = tab;
	}

	const tabs: { id: Tab; labelKey: string; show: boolean }[] = $derived([
		{ id: 'profile', labelKey: 'settings.profile.title', show: true },
		{ id: 'password', labelKey: 'settings.password.title', show: $auth.user?.hasPassword ?? false },
		{ id: 'membership', labelKey: 'settings.membership.title', show: true },
		{ id: 'language', labelKey: 'settings.language.title', show: true }
	]);
</script>

<svelte:head>
	<title>Settings - HelpMotivateMe</title>
</svelte:head>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<h1 class="text-2xl font-bold text-gray-900 mb-6">{$t('settings.title')}</h1>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else}
			<!-- Tab Navigation -->
			<div class="border-b border-gray-200 mb-6">
				<nav class="flex gap-4">
					{#each tabs.filter((tab) => tab.show) as tab}
						<button
							onclick={() => setTab(tab.id)}
							class="px-1 py-3 text-sm font-medium border-b-2 transition-colors {activeTab ===
							tab.id
								? 'border-primary-600 text-primary-600'
								: 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}"
						>
							{$t(tab.labelKey)}
						</button>
					{/each}
				</nav>
			</div>

			<!-- Tab Content -->
			<div class="card p-6">
				{#if activeTab === 'profile'}
					<ProfileTab />
				{:else if activeTab === 'password' && $auth.user?.hasPassword}
					<PasswordTab />
				{:else if activeTab === 'membership'}
					<MembershipTab />
				{:else if activeTab === 'language'}
					<LanguageTab />
				{/if}
			</div>
		{/if}
	</main>
</div>
