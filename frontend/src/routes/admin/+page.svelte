<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from 'svelte-i18n';
	import { auth } from '$lib/stores/auth';
	import StatsTab from '$lib/components/admin/StatsTab.svelte';
	import UsersTab from '$lib/components/admin/UsersTab.svelte';
	import AccessControlTab from '$lib/components/admin/AccessControlTab.svelte';
	import PushNotificationsTab from '$lib/components/admin/PushNotificationsTab.svelte';
	import AnalyticsTab from '$lib/components/admin/AnalyticsTab.svelte';
	import AiUsageTab from '$lib/components/admin/AiUsageTab.svelte';

	type Tab = 'stats' | 'users' | 'access' | 'push' | 'analytics' | 'ai-usage';

	let activeTab = $state<Tab>('stats');
	let loading = $state(true);

	// Read tab from URL hash
	$effect(() => {
		const hash = $page.url.hash.slice(1) as Tab;
		if (['stats', 'users', 'access', 'push', 'analytics', 'ai-usage'].includes(hash)) {
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

		// Check if user is admin
		if ($auth.user.role !== 'Admin') {
			goto('/today');
			return;
		}

		loading = false;
	});

	function setTab(tab: Tab) {
		activeTab = tab;
		window.location.hash = tab;
	}

	const tabs: { id: Tab; label: string }[] = [
		{ id: 'stats', label: 'Statistics' },
		{ id: 'users', label: 'Users' },
		{ id: 'access', label: 'Access Control' },
		{ id: 'push', label: 'Push Notifications' },
		{ id: 'analytics', label: 'Analytics Events' },
		{ id: 'ai-usage', label: 'AI Usage' }
	];
</script>

<svelte:head>
	<title>{$t('admin.title')} - {$t('common.appName')}</title>
</svelte:head>

<div class="bg-warm-cream">
	<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
		<h1 class="text-xl sm:text-2xl font-bold text-cocoa-800 mb-4 sm:mb-6">{$t('admin.title')}</h1>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else}
			<!-- Tab Navigation -->
			<div class="border-b border-primary-100 mb-4 sm:mb-6 -mx-4 sm:mx-0 px-4 sm:px-0">
				<nav class="flex gap-2 sm:gap-4 overflow-x-auto scrollbar-hide">
					{#each tabs as tab}
						<button
							onclick={() => setTab(tab.id)}
							class="px-2 sm:px-1 py-2 sm:py-3 text-xs sm:text-sm font-medium border-b-2 transition-colors whitespace-nowrap flex-shrink-0 {activeTab ===
							tab.id
								? 'border-primary-600 text-primary-600'
								: 'border-transparent text-cocoa-500 hover:text-cocoa-700 hover:border-primary-200'}"
						>
							{tab.label}
						</button>
					{/each}
				</nav>
			</div>

			<!-- Tab Content -->
			<div>
				{#if activeTab === 'stats'}
					<StatsTab />
				{:else if activeTab === 'users'}
					<UsersTab />
				{:else if activeTab === 'access'}
					<AccessControlTab />
				{:else if activeTab === 'push'}
					<PushNotificationsTab />
				{:else if activeTab === 'analytics'}
					<AnalyticsTab />
				{:else if activeTab === 'ai-usage'}
					<AiUsageTab />
				{/if}
			</div>
		{/if}
	</main>
</div>
