<script lang="ts">
	import { page } from '$app/stores';
	import { t } from 'svelte-i18n';
	import { resendVerification } from '$lib/api/auth';

	let email = $derived($page.url.searchParams.get('email') || '');
	let resending = $state(false);
	let resent = $state(false);
	let error = $state('');

	async function handleResend() {
		if (!email) return;

		resending = true;
		error = '';

		try {
			await resendVerification(email);
			resent = true;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to resend verification email';
		} finally {
			resending = false;
		}
	}
</script>

<div class="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
	<div class="max-w-md w-full">
		<div class="text-center mb-8">
			<a href="/" class="text-2xl font-bold text-primary-600">Help Motivate Me</a>
		</div>

		<div class="card p-8 text-center">
			<div class="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
				<svg class="w-8 h-8 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
				</svg>
			</div>

			<h1 class="text-2xl font-bold text-cocoa-800 mb-2">{$t('auth.verifyPending.title')}</h1>

			<p class="text-cocoa-600 mb-4">
				{$t('auth.verifyPending.description')}
			</p>

			{#if email}
				<p class="text-gray-800 font-medium mb-6">
					{email}
				</p>
			{/if}

			{#if error}
				<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-4">
					{error}
				</div>
			{/if}

			{#if resent}
				<div class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-2xl text-sm mb-4">
					{$t('auth.verifyPending.resent')}
				</div>
			{/if}

			<div class="space-y-4">
				<button
					onclick={handleResend}
					disabled={resending || !email}
					class="btn-secondary w-full"
				>
					{resending ? $t('auth.verifyPending.resending') : $t('auth.verifyPending.resend')}
				</button>

				<a href="/auth/login" class="block text-primary-600 hover:text-primary-500 font-medium">
					{$t('auth.verifyPending.backToLogin')}
				</a>
			</div>
		</div>

		<p class="mt-6 text-center text-sm text-cocoa-500">
			{$t('auth.verifyPending.checkSpam')}
		</p>
	</div>
</div>
