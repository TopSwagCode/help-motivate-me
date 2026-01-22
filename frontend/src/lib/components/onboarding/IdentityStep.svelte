<script lang="ts">
	import { t } from 'svelte-i18n';
	import { createIdentity } from '$lib/api/identities';
	import IdentityForm from '$lib/components/identities/IdentityForm.svelte';
	import type { CreateIdentityRequest } from '$lib/types';

	interface Props {
		onnext: () => void;
		onskip: () => void;
	}

	let { onnext, onskip }: Props = $props();

	let showForm = $state(false);
	let createdIdentity = $state<string | null>(null);

	async function handleCreateIdentity(data: CreateIdentityRequest) {
		const identity = await createIdentity(data);
		createdIdentity = identity.name;
		showForm = false;
	}

	function handleCancel() {
		showForm = false;
	}
</script>

<div>
	<!-- Intro section -->
	<div class="mb-8">
		<div class="flex items-center gap-3 mb-4">
			<div class="w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
				<span class="text-2xl">🎯</span>
			</div>
			<h2 class="text-xl font-semibold text-gray-900">{$t('onboarding.manual.identity.title')}</h2>
		</div>

		<div class="prose prose-sm text-gray-600">
			<p class="mb-3">
				{@html $t('onboarding.manual.identity.intro')}
			</p>
			<p class="mb-3">
				{@html $t('onboarding.manual.identity.explanation')}
			</p>
			<ul class="list-disc list-inside space-y-1 text-sm">
				<li>{$t('onboarding.manual.identity.examples.healthy')}</li>
				<li>{$t('onboarding.manual.identity.examples.writer')}</li>
				<li>{$t('onboarding.manual.identity.examples.earlyRiser')}</li>
			</ul>
			<p class="mt-3 text-sm">
				{$t('onboarding.manual.identity.everyAction')}
			</p>
		</div>
	</div>

	<!-- Created identity feedback -->
	{#if createdIdentity}
		<div class="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
			<div class="flex items-center gap-2">
				<svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 24 24">
					<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
				</svg>
				<span class="text-green-700">
					{@html $t('onboarding.manual.identity.created', { values: { name: `<strong>${createdIdentity}</strong>` } })}
				</span>
			</div>
		</div>
	{/if}

	<!-- Form or action buttons -->
	{#if showForm}
		<IdentityForm onsubmit={handleCreateIdentity} oncancel={handleCancel} />
	{:else}
		<div class="space-y-4">
			{#if !createdIdentity}
				<button onclick={() => (showForm = true)} class="btn-primary w-full">
					<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					{$t('onboarding.manual.identity.createFirst')}
				</button>
			{:else}
				<button onclick={() => (showForm = true)} class="btn-secondary w-full">
					<svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					{$t('onboarding.manual.identity.createAnother')}
				</button>
			{/if}

			<div class="flex gap-3">
				<button onclick={onnext} class="btn-primary flex-1">
					{createdIdentity ? $t('onboarding.manual.identity.continue') : $t('onboarding.manual.identity.skipContinue')}
					<svg class="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M9 5l7 7-7 7"
						/>
					</svg>
				</button>
			</div>
		</div>
	{/if}
</div>
