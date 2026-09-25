<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { t } from 'svelte-i18n';
	import { auth } from '$lib/stores/auth';
	import {
		getMyStats,
		getMilestoneDefinitions,
		toggleMilestoneDefinition
	} from '$lib/api/milestones';
	import type { MilestoneDefinition, UserStats } from '$lib/types';

	let stats = $state<UserStats | null>(null);
	let definitions = $state<MilestoneDefinition[]>([]);
	let loading = $state(true);
	let error = $state('');
	let updatingId = $state<string | null>(null);

	const activityCards = $derived(stats ? [
		{ label: $t('advanced.logins'), value: stats.loginCount },
		{ label: $t('advanced.wins'), value: stats.totalWins },
		{ label: $t('advanced.habits'), value: stats.totalHabitsCompleted },
		{ label: $t('advanced.tasks'), value: stats.totalTasksCompleted },
		{ label: $t('advanced.proofs'), value: stats.totalIdentityProofs },
		{ label: $t('advanced.journalEntries'), value: stats.totalJournalEntries }
	] : []);

	onMount(async () => {
		if (!$auth.initialized) await auth.init();
		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			[stats, definitions] = await Promise.all([getMyStats(), getMilestoneDefinitions()]);
		} catch (caughtError) {
			error = caughtError instanceof Error ? caughtError.message : $t('advanced.loadFailed');
		} finally {
			loading = false;
		}
	}

	async function toggleDefinition(definition: MilestoneDefinition) {
		updatingId = definition.id;
		try {
			await toggleMilestoneDefinition(definition.id, !definition.isActive);
			definitions = definitions.map((item) =>
				item.id === definition.id ? { ...item, isActive: !item.isActive } : item
			);
		} catch (caughtError) {
			error = caughtError instanceof Error ? caughtError.message : $t('advanced.loadFailed');
		} finally {
			updatingId = null;
		}
	}

	function formatDate(value: string | null): string {
		return value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '-';
	}
</script>

<svelte:head>
	<title>{$t('advanced.title')} - {$t('common.appName')}</title>
</svelte:head>

<main class="min-h-full bg-warm-cream px-4 py-6 sm:px-6 sm:py-8">
	<div class="mx-auto max-w-5xl">
		<header class="mb-8">
			<h1 class="text-2xl font-bold text-cocoa-800">{$t('advanced.title')}</h1>
			<p class="mt-1 text-sm text-cocoa-500">{$t('advanced.subtitle')}</p>
		</header>

		{#if loading}
			<div class="flex justify-center py-16">
				<div class="h-8 w-8 animate-spin rounded-full border-4 border-primary-600 border-t-transparent"></div>
			</div>
		{:else if error && !stats}
			<div class="border-l-4 border-red-500 bg-red-50 px-4 py-3 text-sm text-red-700">
				{error}
				<button class="ml-3 font-semibold underline" onclick={loadData}>{$t('common.tryAgain')}</button>
			</div>
		{:else}
			<section class="mb-10">
				<div class="mb-4 flex flex-wrap items-end justify-between gap-2">
					<h2 class="text-lg font-semibold text-cocoa-800">{$t('advanced.yourActivity')}</h2>
					<p class="text-xs text-cocoa-500">{$t('advanced.lastActivity')}: {formatDate(stats?.lastActivityAt ?? null)}</p>
				</div>
				<div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
					{#each activityCards as card}
						<div class="rounded-lg border border-primary-100 bg-warm-paper p-4">
							<div class="text-2xl font-bold text-primary-700">{card.value}</div>
							<div class="mt-1 text-xs text-cocoa-500">{card.label}</div>
						</div>
					{/each}
				</div>
			</section>

			<section>
				<div class="mb-4">
					<h2 class="text-lg font-semibold text-cocoa-800">{$t('advanced.milestones')}</h2>
					<p class="mt-1 text-sm text-cocoa-500">{$t('advanced.milestonesDescription')}</p>
				</div>
				{#if error}
					<p class="mb-3 border-l-4 border-red-500 bg-red-50 px-4 py-2 text-sm text-red-700">{error}</p>
				{/if}
				<div class="divide-y divide-primary-100 overflow-hidden rounded-lg border border-primary-100 bg-warm-paper">
					{#each definitions as definition}
						<div class="flex items-center gap-3 px-4 py-3">
							<span class="text-2xl" aria-hidden="true">{definition.icon}</span>
							<div class="min-w-0 flex-1">
								<div class="truncate font-medium text-cocoa-800">{$t(definition.titleKey)}</div>
								<div class="truncate text-xs text-cocoa-500">{$t(definition.descriptionKey)}</div>
							</div>
							<span class="hidden text-xs text-cocoa-500 sm:block">
								{definition.isActive ? $t('advanced.active') : $t('advanced.inactive')}
							</span>
							<button
								type="button"
								role="switch"
								aria-checked={definition.isActive}
								aria-label={`${definition.code}: ${definition.isActive ? $t('advanced.active') : $t('advanced.inactive')}`}
								disabled={updatingId === definition.id}
								onclick={() => toggleDefinition(definition)}
								class="relative h-6 w-11 flex-shrink-0 rounded-full transition-colors disabled:opacity-50 {definition.isActive ? 'bg-green-500' : 'bg-cocoa-300'}"
							>
								<span class="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform {definition.isActive ? 'translate-x-5' : ''}"></span>
							</button>
						</div>
					{/each}
				</div>
			</section>
		{/if}
	</div>
</main>