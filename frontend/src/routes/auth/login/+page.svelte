<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { t } from 'svelte-i18n';

	let username = $state('');
	let password = $state('');
	let error = $state('');
	let loading = $state(false);

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		error = '';
		loading = true;

		const result = await auth.login({ username, password });
		loading = false;

		if (result.success) {
			const returnTo = $page.url.searchParams.get('returnTo');
			await goto(returnTo?.startsWith('/') ? returnTo : '/today');
		} else {
			error = result.error || $t('auth.login.failed');
		}
	}
</script>

<svelte:head>
	<title>{$t('auth.login.heading')} | {$t('common.appName')}</title>
</svelte:head>

<main class="min-h-screen flex items-center justify-center px-4 py-12">
	<section class="w-full max-w-sm" aria-labelledby="login-heading">
		<header class="mb-8 text-center">
			<h1 id="login-heading" class="text-3xl font-bold text-cocoa-800">{$t('common.appName')}</h1>
			<p class="mt-2 text-cocoa-600">{$t('auth.login.heading')}</p>
		</header>

		<form onsubmit={handleSubmit} class="card space-y-5 p-8">
			{#if error}
				<p class="rounded border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">{error}</p>
			{/if}

			<div>
				<label for="username" class="label">{$t('auth.login.username')}</label>
				<input
					id="username"
					type="text"
					bind:value={username}
					required
					autocomplete="username"
					class="input"
				/>
			</div>

			<div>
				<label for="password" class="label">{$t('auth.login.password')}</label>
				<input
					id="password"
					type="password"
					bind:value={password}
					required
					autocomplete="current-password"
					class="input"
				/>
			</div>

			<button type="submit" disabled={loading} class="btn-primary w-full">
				{loading ? $t('auth.login.signingInBtn') : $t('auth.login.signInBtn')}
			</button>
		</form>
	</section>
</main>
