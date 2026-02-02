<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import type { WaitlistEntry, WhitelistEntry, SignupSettingsResponse } from '$lib/types/waitlist';
	import {
		getAdminSettings,
		getWaitlist,
		removeFromWaitlist,
		approveWaitlistEntry,
		getWhitelist,
		addToWhitelist,
		removeFromWhitelist
	} from '$lib/api/admin';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let waitlistEntries = $state<WaitlistEntry[]>([]);
	let whitelistEntries = $state<WhitelistEntry[]>([]);
	let allowSignups = $state(true);
	let loading = $state(true);
	let error = $state('');

	// Invite form
	let inviteEmail = $state('');
	let inviteLoading = $state(false);

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			const [settingsData, waitlistData, whitelistData] = await Promise.all([
				getAdminSettings(),
				getWaitlist(),
				getWhitelist()
			]);
			allowSignups = settingsData.allowSignups;
			waitlistEntries = waitlistData;
			whitelistEntries = whitelistData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load access control data';
		} finally {
			loading = false;
		}
	}

	async function handleApproveWaitlist(entry: WaitlistEntry) {
		try {
			const newWhitelistEntry = await approveWaitlistEntry(entry.id);
			waitlistEntries = waitlistEntries.filter((w) => w.id !== entry.id);
			whitelistEntries = [newWhitelistEntry, ...whitelistEntries];
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to approve waitlist entry';
		}
	}

	async function handleRemoveFromWaitlist(entry: WaitlistEntry) {
		try {
			await removeFromWaitlist(entry.id);
			waitlistEntries = waitlistEntries.filter((w) => w.id !== entry.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to remove from waitlist';
		}
	}

	async function handleRemoveFromWhitelist(entry: WhitelistEntry) {
		try {
			await removeFromWhitelist(entry.id);
			whitelistEntries = whitelistEntries.filter((w) => w.id !== entry.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to remove from whitelist';
		}
	}

	async function handleInviteUser(e: Event) {
		e.preventDefault();
		if (!inviteEmail.trim()) return;

		inviteLoading = true;
		try {
			const newEntry = await addToWhitelist(inviteEmail.trim());
			whitelistEntries = [newEntry, ...whitelistEntries];
			inviteEmail = '';
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to invite user';
		} finally {
			inviteLoading = false;
		}
	}

	function formatDate(dateStr: string): string {
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return new Date(dateStr).toLocaleDateString(currentLocale, {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}
</script>

{#if loading}
	<div class="flex justify-center py-12">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{:else if error}
	<div class="card">
		<ErrorState message={error} onRetry={loadData} size="md" />
	</div>
{:else}
	<!-- Access Control Status -->
	<div class="mb-6">
		<h2 class="text-lg font-semibold text-cocoa-800">{$t('admin.accessControl.title')}</h2>
		<p class="text-cocoa-500 mt-1">
			{#if allowSignups}
				<span class="inline-flex items-center gap-1.5">
					<span class="w-2 h-2 bg-green-500 rounded-full"></span>
					{$t('admin.accessControl.openSignups')}
				</span>
			{:else}
				<span class="inline-flex items-center gap-1.5">
					<span class="w-2 h-2 bg-yellow-500 rounded-full"></span>
					{$t('admin.accessControl.closedBeta')}
				</span>
			{/if}
		</p>
	</div>

	<!-- Invite User Section -->
	<div class="card p-6 mb-6">
		<h3 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('admin.invite.title')}</h3>
		<form onsubmit={handleInviteUser} class="flex gap-4">
			<input
				type="email"
				placeholder={$t('admin.invite.placeholder')}
				bind:value={inviteEmail}
				required
				class="input flex-1"
			/>
			<button
				type="submit"
				disabled={inviteLoading || !inviteEmail.trim()}
				class="btn-primary whitespace-nowrap"
			>
				{inviteLoading ? $t('admin.invite.sending') : $t('admin.invite.send')}
			</button>
		</form>
		<p class="text-sm text-cocoa-500 mt-2">
			{$t('admin.invite.description')}
		</p>
	</div>

	<!-- Waitlist and Whitelist Tables -->
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
		<!-- Waitlist Table -->
		<div class="card">
			<div class="p-6 border-b border-primary-100">
				<div class="flex items-center justify-between">
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('admin.waitlist.title')}</h3>
					<span class="px-2 py-1 text-xs font-medium rounded-full bg-yellow-100 text-yellow-700">
						{waitlistEntries.length} {$t('admin.waitlist.pending')}
					</span>
				</div>
				<p class="text-sm text-cocoa-500 mt-1">{$t('admin.waitlist.description')}</p>
			</div>

			<div class="overflow-x-auto max-h-96 overflow-y-auto">
				<table class="w-full">
					<thead class="bg-warm-cream sticky top-0">
						<tr>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.waitlist.email')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.waitlist.name')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.waitlist.date')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.waitlist.actions')}
							</th>
						</tr>
					</thead>
					<tbody class="bg-warm-paper divide-y divide-gray-200">
						{#each waitlistEntries as entry (entry.id)}
							<tr class="hover:bg-warm-cream">
								<td class="px-4 py-3 text-sm text-cocoa-800 max-w-[150px] truncate" title={entry.email}>
									{entry.email}
								</td>
								<td class="px-4 py-3 text-sm text-cocoa-500 max-w-[100px] truncate" title={entry.name}>
									{entry.name}
								</td>
								<td class="px-4 py-3 text-sm text-cocoa-500 whitespace-nowrap">
									{formatDate(entry.createdAt)}
								</td>
								<td class="px-4 py-3 text-sm whitespace-nowrap">
									<div class="flex gap-2">
										<button
											onclick={() => handleApproveWaitlist(entry)}
											class="text-green-600 hover:text-green-800 font-medium"
										>
											{$t('admin.waitlist.approve')}
										</button>
										<button
											onclick={() => handleRemoveFromWaitlist(entry)}
											class="text-red-600 hover:text-red-800 font-medium"
										>
											{$t('admin.waitlist.remove')}
										</button>
									</div>
								</td>
							</tr>
						{:else}
							<tr>
								<td colspan="4" class="px-4 py-8 text-center text-cocoa-500">
									{$t('admin.waitlist.empty')}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</div>

		<!-- Whitelist Table -->
		<div class="card">
			<div class="p-6 border-b border-primary-100">
				<div class="flex items-center justify-between">
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('admin.whitelist.title')}</h3>
					<span class="px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700">
						{whitelistEntries.length} {$t('admin.whitelist.approved')}
					</span>
				</div>
				<p class="text-sm text-cocoa-500 mt-1">{$t('admin.whitelist.description')}</p>
			</div>

			<div class="overflow-x-auto max-h-96 overflow-y-auto">
				<table class="w-full">
					<thead class="bg-warm-cream sticky top-0">
						<tr>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.whitelist.email')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.whitelist.added')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.whitelist.invited')}
							</th>
							<th class="px-4 py-3 text-left text-xs font-medium text-cocoa-500 uppercase tracking-wider">
								{$t('admin.whitelist.actions')}
							</th>
						</tr>
					</thead>
					<tbody class="bg-warm-paper divide-y divide-gray-200">
						{#each whitelistEntries as entry (entry.id)}
							<tr class="hover:bg-warm-cream">
								<td class="px-4 py-3 text-sm text-cocoa-800 max-w-[150px] truncate" title={entry.email}>
									{entry.email}
								</td>
								<td class="px-4 py-3 text-sm text-cocoa-500 whitespace-nowrap">
									<div>
										<p>{formatDate(entry.addedAt)}</p>
										{#if entry.addedByUsername}
											<p class="text-xs text-gray-400">{$t('admin.whitelist.by')} {entry.addedByUsername}</p>
										{/if}
									</div>
								</td>
								<td class="px-4 py-3 text-sm whitespace-nowrap">
									{#if entry.invitedAt}
										<span class="text-green-600" title={formatDate(entry.invitedAt)}>{$t('admin.whitelist.sent')}</span>
									{:else}
										<span class="text-gray-400">-</span>
									{/if}
								</td>
								<td class="px-4 py-3 text-sm whitespace-nowrap">
									<button
										onclick={() => handleRemoveFromWhitelist(entry)}
										class="text-red-600 hover:text-red-800 font-medium"
									>
										{$t('admin.whitelist.remove')}
									</button>
								</td>
							</tr>
						{:else}
							<tr>
								<td colspan="4" class="px-4 py-8 text-center text-cocoa-500">
									{$t('admin.whitelist.empty')}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</div>
	</div>
{/if}
