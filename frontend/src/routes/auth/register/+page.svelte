<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	let username = $state('');
	let email = $state('');
	let password = $state('');
	let displayName = $state('');
	let error = $state('');
	let loading = $state(false);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		error = '';
		loading = true;

		const result = await auth.register({
			username,
			email,
			password,
			displayName: displayName || undefined
		});
		loading = false;

		if (result.success) {
			goto('/dashboard');
		} else if (result.code === 'signup_disabled') {
			// Redirect directly to waitlist page
			goto(`/waitlist?email=${encodeURIComponent(email)}&name=${encodeURIComponent(displayName || username)}`);
		} else {
			error = result.error || 'Registration failed';
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
			<h1 class="mt-6 text-3xl font-bold text-gray-900">Create your account</h1>
			<p class="mt-2 text-gray-600">
				Already have an account?
				<a href="/auth/login" class="text-primary-600 hover:text-primary-500 font-medium">Sign in</a>
			</p>
		</div>

		<div class="card p-8">
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
			</div>

			<div class="relative my-6">
				<div class="absolute inset-0 flex items-center">
					<div class="w-full border-t border-gray-300"></div>
				</div>
				<div class="relative flex justify-center text-sm">
					<span class="px-2 bg-white text-gray-500">Or continue with</span>
				</div>
			</div>

			<!-- Registration Form -->
			<form onsubmit={handleSubmit} class="space-y-4">
				{#if error}
					<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
						{error}
					</div>
				{/if}

				<div>
					<label for="username" class="label">Username</label>
					<input
						id="username"
						type="text"
						bind:value={username}
						required
						minlength="3"
						maxlength="50"
						autocomplete="username"
						class="input"
					/>
				</div>

				<div>
					<label for="email" class="label">Email</label>
					<input
						id="email"
						type="email"
						bind:value={email}
						required
						autocomplete="email"
						class="input"
					/>
				</div>

				<div>
					<label for="displayName" class="label">Display Name <span class="text-gray-400">(optional)</span></label>
					<input
						id="displayName"
						type="text"
						bind:value={displayName}
						maxlength="100"
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
						minlength="8"
						autocomplete="new-password"
						class="input"
					/>
					<p class="mt-1 text-xs text-gray-500">Must be at least 8 characters</p>
				</div>

				<button type="submit" disabled={loading} class="btn-primary w-full">
					{loading ? 'Creating account...' : 'Create account'}
				</button>
			</form>
		</div>

		<div class="mt-6 text-center text-sm text-gray-500">
			<p>
				Need help? Check out our <a href="/faq" class="text-primary-600 hover:text-primary-500 font-medium">FAQ</a>
			</p>
		</div>
	</div>
</div>
