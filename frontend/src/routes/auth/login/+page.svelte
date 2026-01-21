<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { requestLoginLink, loginWithToken } from '$lib/api/auth';
	import { ApiError } from '$lib/api/client';

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
				goto('/today');
			} catch (err: unknown) {
				if (err instanceof ApiError && err.code === 'not_whitelisted') {
					const userEmail = err.data?.email as string;
					goto(`/waitlist?email=${encodeURIComponent(userEmail || '')}`);
					return;
				}
				error = err instanceof Error ? err.message : 'Invalid or expired login link';
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
			goto('/today');
		} else if (result.code === 'email_not_verified') {
			// Redirect to verify-pending page
			goto(`/auth/verify-pending?email=${encodeURIComponent(result.email || username)}`);
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
		} catch (err: unknown) {
			if (err instanceof ApiError && err.code === 'not_whitelisted') {
				const userEmail = err.data?.email as string;
				goto(`/waitlist?email=${encodeURIComponent(userEmail || email)}`);
				return;
			}
			error = err instanceof Error ? err.message : 'Failed to send login link';
		} finally {
			emailLinkLoading = false;
		}
	}

	function handleGitHubLogin() {
		auth.loginWithGitHub();
	}

	function handleGoogleLogin() {
		auth.loginWithGoogle();
	}

	function handleLinkedInLogin() {
		auth.loginWithLinkedIn();
	}

	function handleFacebookLogin() {
		auth.loginWithFacebook();
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
					<button onclick={handleGoogleLogin} class="btn-secondary w-full flex items-center justify-center gap-3">
						<svg class="w-5 h-5" viewBox="0 0 24 24">
							<path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
							<path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
							<path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
							<path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
						</svg>
						Continue with Google
					</button>

					<button onclick={handleLinkedInLogin} class="btn-secondary w-full flex items-center justify-center gap-3">
						<svg class="w-5 h-5" viewBox="0 0 24 24" fill="#0A66C2">
							<path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
						</svg>
						Continue with LinkedIn
					</button>

					<button onclick={handleFacebookLogin} class="btn-secondary w-full flex items-center justify-center gap-3">
						<svg class="w-5 h-5" viewBox="0 0 24 24" fill="#1877F2">
							<path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
						</svg>
						Continue with Facebook
					</button>

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
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 0 002 2z" />
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

		<div class="mt-6 text-center text-sm text-gray-500">
			<p>
				Need help? Check out our <a href="/faq" class="text-primary-600 hover:text-primary-500 font-medium">FAQ</a>
			</p>
		</div>
	</div>
</div>
