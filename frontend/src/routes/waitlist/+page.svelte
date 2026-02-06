<script lang="ts">
	import { page } from '$app/stores';
	import { signupForWaitlist } from '$lib/api/waitlist';

	let email = $state($page.url.searchParams.get('email') || '');
	let name = $state($page.url.searchParams.get('name') || '');
	let provider = $state($page.url.searchParams.get('provider') || '');

	let loading = $state(false);
	let error = $state('');
	let success = $state(false);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		error = '';
		loading = true;

		try {
			await signupForWaitlist({ email, name });
			success = true;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Something went wrong. Please try again.';
		} finally {
			loading = false;
		}
	}
</script>

<svelte:head>
	<title>Join the Waitlist - Help Motivate Me</title>
</svelte:head>

<div class="min-h-screen flex items-center justify-center px-4 py-12 bg-warm-cream">
	<div class="max-w-md w-full">
		{#if success}
			<div class="card p-8 text-center">
				<div class="w-16 h-16 mx-auto mb-6 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
					</svg>
				</div>
				<h1 class="text-2xl font-bold text-cocoa-800 mb-4">You're on the list!</h1>
				<p class="text-cocoa-600 mb-6">
					Thank you for your interest in Help Motivate Me. We'll send you an email as soon as a spot opens up.
				</p>
				<a href="/" class="btn-primary inline-block">Back to Home</a>
			</div>
		{:else}
			<div class="card p-8">
				<div class="text-center mb-8">
					<div class="w-16 h-16 mx-auto mb-4 bg-primary-100 rounded-full flex items-center justify-center">
						<svg class="w-8 h-8 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
						</svg>
					</div>
					<h1 class="text-2xl font-bold text-cocoa-800 mb-2">We're in Closed Beta</h1>
					<p class="text-cocoa-600">
						Help Motivate Me is currently available to a limited group of users as we refine the experience.
					</p>
				</div>

				{#if provider}
					<div class="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-3 rounded-2xl mb-6">
						<p class="text-sm">
							It looks like you tried to sign in with {provider}. Since you don't have an account yet, please join our waitlist below.
						</p>
					</div>
				{/if}

				<form onsubmit={handleSubmit} class="space-y-4">
					<div>
						<label for="name" class="block text-sm font-medium text-cocoa-700 mb-1">Name</label>
						<input
							type="text"
							id="name"
							bind:value={name}
							required
							class="input w-full"
							placeholder="Your name"
						/>
					</div>

					<div>
						<label for="email" class="block text-sm font-medium text-cocoa-700 mb-1">Email</label>
						<input
							type="email"
							id="email"
							bind:value={email}
							required
							class="input w-full"
							placeholder="you@example.com"
						/>
					</div>

					{#if error}
						<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm">
							{error}
						</div>
					{/if}

					<button
						type="submit"
						disabled={loading}
						class="btn-primary w-full flex items-center justify-center gap-2"
					>
						{#if loading}
							<div class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
							Joining...
						{:else}
							Join the Waitlist
						{/if}
					</button>
				</form>

				<div class="mt-8 pt-6 border-t border-primary-100">
					<h2 class="text-sm font-medium text-cocoa-800 mb-3">What is Help Motivate Me?</h2>
					<ul class="space-y-2 text-sm text-cocoa-600">
						<li class="flex items-start gap-2">
							<svg class="w-5 h-5 text-primary-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
							</svg>
							Set meaningful goals and track your progress
						</li>
						<li class="flex items-start gap-2">
							<svg class="w-5 h-5 text-primary-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
							</svg>
							Break down tasks into manageable steps
						</li>
						<li class="flex items-start gap-2">
							<svg class="w-5 h-5 text-primary-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
							</svg>
							Build daily, weekly, and monthly habits
						</li>
					</ul>
				</div>

				<p class="mt-6 text-center text-sm text-cocoa-500">
					Already have access? <a href="/auth/login" class="text-primary-600 hover:text-primary-700 font-medium">Sign in</a>
				</p>
			</div>
		{/if}
	</div>
</div>
