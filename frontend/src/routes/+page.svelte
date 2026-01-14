<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { signupForWaitlist } from '$lib/api/waitlist';

	let waitlistEmail = $state('');
	let waitlistName = $state('');
	let waitlistLoading = $state(false);
	let waitlistSuccess = $state(false);
	let waitlistError = $state('');

	function handleGetStarted() {
		if ($auth.user) {
			goto('/dashboard');
		} else {
			goto('/auth/login');
		}
	}

	async function handleWaitlistSubmit(e: Event) {
		e.preventDefault();
		if (!waitlistEmail.trim() || !waitlistName.trim()) return;

		waitlistLoading = true;
		waitlistError = '';

		try {
			await signupForWaitlist({ email: waitlistEmail.trim(), name: waitlistName.trim() });
			waitlistSuccess = true;
		} catch (err) {
			waitlistError = err instanceof Error ? err.message : 'Failed to join waitlist';
		} finally {
			waitlistLoading = false;
		}
	}
</script>

<div class="min-h-screen flex flex-col">
	<!-- Hero Section -->
	<main class="flex-1 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
		<div class="max-w-3xl mx-auto text-center">
			<h1 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 tracking-tight">
				Stay Focused.
				<span class="text-primary-600">Stay Motivated.</span>
			</h1>
			<p class="mt-6 text-lg sm:text-xl text-gray-600 max-w-2xl mx-auto">
				Set meaningful goals, break them into actionable tasks, and build the habits that lead to success.
				Your journey to productivity starts here.
			</p>
			<div class="mt-10 flex flex-col sm:flex-row gap-4 justify-center">
				<button onclick={handleGetStarted} class="btn-primary text-base px-8 py-3">
					Get Started
				</button>
				<a href="/auth/login" class="btn-secondary text-base px-8 py-3">
					Sign In
				</a>
			</div>
		</div>
	</main>

	<!-- Waitlist Signup Section -->
	<section class="py-12 px-4 sm:px-6 lg:px-8 bg-primary-50 border-t border-primary-100">
		<div class="max-w-xl mx-auto text-center">
			<div class="inline-flex items-center gap-2 px-3 py-1 bg-primary-100 rounded-full text-primary-700 text-sm font-medium mb-4">
				<span class="w-2 h-2 bg-primary-500 rounded-full"></span>
				Closed Beta
			</div>
			<h2 class="text-2xl font-bold text-gray-900 mb-2">Join the Waitlist</h2>
			<p class="text-gray-600 mb-6">
				We're currently in closed beta. Sign up to get notified when we open to the public.
			</p>

			{#if waitlistSuccess}
				<div class="bg-green-50 border border-green-200 text-green-700 px-6 py-4 rounded-lg">
					<div class="flex items-center justify-center gap-2 mb-1">
						<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
						<span class="font-semibold">You're on the list!</span>
					</div>
					<p class="text-sm">We'll send you an email when access becomes available.</p>
				</div>
			{:else}
				<form onsubmit={handleWaitlistSubmit} class="flex flex-col sm:flex-row gap-3">
					<input
						type="text"
						placeholder="Your name"
						bind:value={waitlistName}
						required
						class="input flex-1"
					/>
					<input
						type="email"
						placeholder="Your email"
						bind:value={waitlistEmail}
						required
						class="input flex-1"
					/>
					<button
						type="submit"
						disabled={waitlistLoading}
						class="btn-primary whitespace-nowrap px-6"
					>
						{waitlistLoading ? 'Joining...' : 'Join Waitlist'}
					</button>
				</form>
				{#if waitlistError}
					<p class="text-red-600 text-sm mt-2">{waitlistError}</p>
				{/if}
			{/if}
		</div>
	</section>

	<!-- Features Preview -->
	<section class="py-16 px-4 sm:px-6 lg:px-8 bg-white border-t border-gray-200">
		<div class="max-w-6xl mx-auto">
			<div class="grid grid-cols-1 md:grid-cols-3 gap-8">
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-lg flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-900">Set Goals</h3>
					<p class="mt-2 text-gray-600">Define clear, meaningful goals and track your progress toward achieving them.</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-lg flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-900">Break Down Tasks</h3>
					<p class="mt-2 text-gray-600">Divide your goals into manageable subtasks that keep you moving forward.</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-lg flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-900">Build Habits</h3>
					<p class="mt-2 text-gray-600">Create repeatable tasks that become daily, weekly, or monthly habits.</p>
				</div>
			</div>
		</div>
	</section>

	<!-- Footer -->
	<footer class="py-8 px-4 text-center text-sm text-gray-500 border-t border-gray-200">
		<div class="mb-4 flex flex-wrap justify-center gap-4">
			<a href="/about" class="text-primary-600 hover:text-primary-700 font-medium">About</a>
			<a href="/pricing" class="text-primary-600 hover:text-primary-700 font-medium">Pricing</a>
			<a href="/faq" class="text-primary-600 hover:text-primary-700 font-medium">FAQ</a>
			<a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium">Contact</a>
		</div>
		<div class="mb-4 flex flex-wrap justify-center gap-4 text-gray-400">
			<a href="/privacy" class="hover:text-gray-600">Privacy Policy</a>
			<span class="hidden sm:inline">|</span>
			<a href="/terms" class="hover:text-gray-600">Terms of Service</a>
		</div>
		<p>&copy; {new Date().getFullYear()} Help Motivate Me. All rights reserved.</p>
	</footer>
</div>
