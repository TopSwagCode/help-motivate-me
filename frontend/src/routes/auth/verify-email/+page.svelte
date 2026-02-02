<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { t } from 'svelte-i18n';
	import { auth } from '$lib/stores/auth';
	import { verifyEmail } from '$lib/api/auth';

	let verifying = $state(true);
	let error = $state('');
	let success = $state(false);

	onMount(async () => {
		const token = $page.url.searchParams.get('token');

		if (!token) {
			error = 'No verification token provided';
			verifying = false;
			return;
		}

		try {
			const user = await verifyEmail(token);
			auth.setUser(user);
			success = true;
			// Redirect to today after a brief moment to show success
			setTimeout(() => {
				goto('/today');
			}, 1500);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Verification failed';
		} finally {
			verifying = false;
		}
	});
</script>

<div class="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
	<div class="max-w-md w-full">
		<div class="text-center mb-8">
			<a href="/" class="text-2xl font-bold text-primary-600">Help Motivate Me</a>
		</div>

		<div class="card p-8 text-center">
			{#if verifying}
				<div class="py-8">
					<div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto mb-4"></div>
					<p class="text-cocoa-600">{$t('auth.verifyEmail.verifying')}</p>
				</div>
			{:else if success}
				<div class="py-8">
					<div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
						<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<h2 class="text-2xl font-bold text-cocoa-800 mb-2">{$t('auth.verifyEmail.success')}</h2>
					<p class="text-cocoa-600">{$t('auth.verifyEmail.redirecting')}</p>
				</div>
			{:else}
				<div class="py-8">
					<div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
						<svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</div>
					<h2 class="text-2xl font-bold text-cocoa-800 mb-2">{$t('auth.verifyEmail.failed')}</h2>
					<p class="text-red-600 mb-6">{error}</p>

					<div class="space-y-3">
						<a href="/auth/register" class="btn-primary w-full block text-center">
							{$t('auth.verifyEmail.createAccount')}
						</a>
						<a href="/auth/login" class="btn-secondary w-full block text-center">
							{$t('auth.verifyEmail.backToLogin')}
						</a>
					</div>
				</div>
			{/if}
		</div>
	</div>
</div>
