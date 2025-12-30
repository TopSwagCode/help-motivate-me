<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { requestLoginLink, loginWithToken } from '$lib/api/auth';

	let username = $state('');
	let password = $state('');
	let email = $state('');
	let error = $state('');
	let loading = $state(false);
	let emailLinkLoading = $state(false);
	let emailLinkSent = $state(false);
	let showEmailLogin = $state(false);

	// Handle token from URL (magic link callback)
	onMount(async () => {
		const token = $page.url.searchParams.get('token');
		if (token) {
			loading = true;
			try {
				const user = await loginWithToken(token);
				auth.setUser(user);
				goto('/dashboard');
			} catch (err: any) {
				error = err.message || 'Invalid or expired login link';
				loading = false;
			}
		}
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		error = '';
		loading = true;

		const result = await auth.login({ username, password });
		loading = false;

		if (result.success) {
			goto('/dashboard');
		} else {
			error = result.error || 'Login failed';
		}
	}

	async function handleEmailLinkSubmit(e: Event) {
		e.preventDefault();
		error = '';
		emailLinkLoading = true;

		try {
			await requestLoginLink(email);
			emailLinkSent = true;
		} catch (err: any) {
			error = err.message || 'Failed to send login link';
		} finally {
			emailLinkLoading = false;
		}
	}

	function handleGitHubLogin() {
		auth.loginWithGitHub();
	}
</script>

<div class="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
	<div class="max-w-md w-full">
		<div class="text-center mb-8">
			<a href="/" class="text-2xl font-bold text-primary-600">Help Motivate Me</a>
			<h1 class="mt-6 text-3xl font-bold text-gray-900">Sign in to your account</h1>
			<p class="mt-2 text-gray-600">
				Or
				<a href="/auth/register" class="text-primary-600 hover:text-primary-500 font-medium">create a new account</a>
			</p>
		</div>

		<div class="card p-8">
			{#if error}
				<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
					{error}
				</div>
			{/if}

			{#if loading && $page.url.searchParams.get('token')}
				<div class="text-center py-8">
					<div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto mb-4"></div>
					<p class="text-gray-600">Signing you in...</p>
				</div>
			{:else if emailLinkSent}
				<div class="text-center py-8">
					<div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
						<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
						</svg>
					</div>
					<h2 class="text-xl font-semibold text-gray-900 mb-2">Check your email</h2>
					<p class="text-gray-600 mb-4">
						We sent a login link to <strong>{email}</strong>
					</p>
					<p class="text-sm text-gray-500">
						The link will expire in 24 hours.
					</p>
					<button
						onclick={() => { emailLinkSent = false; email = ''; }}
						class="mt-6 text-primary-600 hover:text-primary-500 font-medium text-sm"
					>
						Use a different email
					</button>
				</div>
			{:else if showEmailLogin}
				<!-- Email Link Login Form -->
				<form onsubmit={handleEmailLinkSubmit} class="space-y-4">
					<div>
						<label for="email" class="label">Email address</label>
						<input
							id="email"
							type="email"
							bind:value={email}
							required
							autocomplete="email"
							placeholder="you@example.com"
							class="input"
						/>
					</div>

					<button type="submit" disabled={emailLinkLoading} class="btn-primary w-full">
						{emailLinkLoading ? 'Sending...' : 'Send login link'}
					</button>
				</form>

				<button
					onclick={() => showEmailLogin = false}
					class="mt-4 w-full text-center text-sm text-gray-500 hover:text-gray-700"
				>
					Back to other login options
				</button>
			{:else}
				<!-- Social Login -->
				<div class="space-y-3">
					<button onclick={handleGitHubLogin} class="btn-secondary w-full flex items-center justify-center gap-3">
						<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
							<path
								d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
							/>
						</svg>
						Continue with GitHub
					</button>

					<button onclick={() => showEmailLogin = true} class="btn-secondary w-full flex items-center justify-center gap-3">
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
						</svg>
						Continue with Email
					</button>
				</div>

				<div class="relative my-6">
					<div class="absolute inset-0 flex items-center">
						<div class="w-full border-t border-gray-300"></div>
					</div>
					<div class="relative flex justify-center text-sm">
						<span class="px-2 bg-white text-gray-500">Or sign in with password</span>
					</div>
				</div>

				<!-- Email/Password Form -->
				<form onsubmit={handleSubmit} class="space-y-4">
					<div>
						<label for="username" class="label">Username or Email</label>
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
						<label for="password" class="label">Password</label>
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
						{loading ? 'Signing in...' : 'Sign in'}
					</button>
				</form>
			{/if}
		</div>
	</div>
</div>
