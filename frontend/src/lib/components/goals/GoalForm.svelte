<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from 'svelte-i18n';
	import { getIdentities } from '$lib/api/identities';
	import type { Goal, CreateGoalRequest, Identity } from '$lib/types';

	interface Props {
		goal?: Goal;
		onsubmit: (data: CreateGoalRequest) => Promise<void>;
		oncancel: () => void;
	}

	let { goal, onsubmit, oncancel }: Props = $props();

	let title = $state('');
	let description = $state('');
	let targetDate = $state('');
	let identityId = $state('');
	let identities = $state<Identity[]>([]);
	let loading = $state(false);
	let error = $state('');

	// Initialize form fields from goal prop
	$effect(() => {
		title = goal?.title ?? '';
		description = goal?.description ?? '';
		targetDate = goal?.targetDate ?? '';
		identityId = goal?.identityId ?? '';
	});

	onMount(async () => {
		try {
			identities = await getIdentities();
		} catch (e) {
			console.error('Failed to load identities', e);
		}
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!title.trim()) return;

		loading = true;
		error = '';

		try {
			await onsubmit({
				title: title.trim(),
				description: description.trim() || undefined,
				targetDate: targetDate || undefined,
				identityId: identityId || undefined
			});
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to save goal';
		} finally {
			loading = false;
		}
	}
</script>

<form onsubmit={handleSubmit} class="space-y-4">
	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
			{error}
		</div>
	{/if}

	<div>
		<label for="title" class="label">{$t('goals.form.title')} *</label>
		<input
			id="title"
			type="text"
			bind:value={title}
			required
			maxlength="255"
			placeholder={$t('goals.form.titlePlaceholder')}
			class="input"
			disabled={loading}
		/>
	</div>

	<div>
		<label for="description" class="label">{$t('goals.form.description')}</label>
		<textarea
			id="description"
			bind:value={description}
			rows="3"
			placeholder={$t('goals.form.descriptionPlaceholder')}
			class="input"
			disabled={loading}
		></textarea>
	</div>

	<div>
		<label for="identityId" class="label">{$t('goals.form.identity')} <span class="text-gray-500 text-sm">({$t('common.optional')})</span></label>
		<select
			id="identityId"
			bind:value={identityId}
			class="input"
			disabled={loading}
		>
			<option value="">{$t('goals.noIdentity')}</option>
			{#each identities as identity (identity.id)}
				<option value={identity.id}>
					{identity.icon ? `${identity.icon} ` : ''}{identity.name}
				</option>
			{/each}
		</select>
		{#if identities.length > 0}
			<p class="text-xs text-gray-500 mt-1">{$t('goals.form.identityHint')}</p>
		{:else}
			<p class="text-xs text-gray-500 mt-1">
				<a href="/identities" class="text-primary-600 hover:text-primary-700">{$t('goals.form.createIdentity')}</a>
			</p>
		{/if}
	</div>

	<div>
		<label for="targetDate" class="label">{$t('goals.form.targetDate')}</label>
		<input
			id="targetDate"
			type="date"
			bind:value={targetDate}
			class="input"
			disabled={loading}
		/>
	</div>

	<div class="flex gap-3 pt-4">
		<button type="submit" disabled={loading || !title.trim()} class="btn-primary flex-1">
			{loading ? $t('common.saving') : goal ? $t('common.update') : $t('goals.create')}
		</button>
		<button type="button" onclick={oncancel} class="btn-secondary" disabled={loading}>
			{$t('common.cancel')}
		</button>
	</div>
</form>
