<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	// Redirect to /today if already logged in
	$effect(() => {
		if ($auth.initialized && $auth.user) {
			goto('/today');
		}
	});

	function handleGetStarted() {
		if ($auth.user) {
			goto('/today');
		} else {
			goto('/auth/login');
		}
	}
</script>

<div class="min-h-screen flex flex-col">
	<!-- Hero Section -->
	<main class="flex-1 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
		<div class="max-w-3xl mx-auto text-center">
			<h1 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-cocoa-800 tracking-tight">
				Stay Focused.
				<span class="text-primary-600">Stay Motivated.</span>
			</h1>
			<p class="mt-6 text-lg sm:text-xl text-cocoa-600 max-w-2xl mx-auto">
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

	<!-- Meet Milo - Your Gentle Motivator -->
	<section class="py-12 px-4 sm:px-6 lg:px-8 bg-primary-50 border-t border-primary-100">
		<div class="max-w-2xl mx-auto text-center">
			<h2 class="text-2xl font-bold text-cocoa-800 mb-2">Meet Milo</h2>
			<p class="text-primary-600 font-medium mb-6">Your gentle motivator</p>
			
			<div class="mb-6 rounded-2xl overflow-hidden mx-auto max-w-md">
				<video 
					autoplay 
					loop 
					muted 
					playsinline
					class="w-full h-auto"
					style="clip-path: inset(10% 0 10% 0);"
				>
					<source src="/test.webm" type="video/webm" />
					Your browser does not support the video tag.
				</video>
			</div>
			
			<p class="text-cocoa-600 leading-relaxed max-w-lg mx-auto">
				This sloth isn't here to rush you or yell at you. It's here to remind you that showing up — even a little — counts. One habit at a time, one day at a time, you're making progress. And your sloth is right there with you.
			</p>
			
			<p class="text-sm text-cocoa-400 mt-4 italic">This is our mascot</p>
		</div>
	</section>

	<!-- Features Preview -->
	<section class="py-16 px-4 sm:px-6 lg:px-8 bg-warm-paper border-t border-primary-100">
		<div class="max-w-6xl mx-auto">
			<div class="grid grid-cols-1 md:grid-cols-3 gap-8">
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">Set Goals</h3>
					<p class="mt-2 text-cocoa-600">Define clear, meaningful goals and track your progress toward achieving them.</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">Break Down Tasks</h3>
					<p class="mt-2 text-cocoa-600">Divide your goals into manageable subtasks that keep you moving forward.</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">Build Habits</h3>
					<p class="mt-2 text-cocoa-600">Create repeatable tasks that become daily, weekly, or monthly habits.</p>
				</div>
			</div>
		</div>
	</section>

	<!-- Footer -->
	<footer class="py-8 px-4 text-center text-sm text-cocoa-500 border-t border-primary-100">
		<div class="mb-4 flex flex-wrap justify-center gap-4">
			<a href="/about" class="text-primary-600 hover:text-primary-700 font-medium">About</a>
			<a href="/pricing" class="text-primary-600 hover:text-primary-700 font-medium">Pricing</a>
			<a href="/faq" class="text-primary-600 hover:text-primary-700 font-medium">FAQ</a>
			<a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium">Contact</a>
		</div>
		<div class="mb-4 flex flex-wrap justify-center gap-4 text-gray-400">
			<a href="/privacy" class="hover:text-cocoa-600">Privacy Policy</a>
			<span class="hidden sm:inline">|</span>
			<a href="/terms" class="hover:text-cocoa-600">Terms of Service</a>
		</div>
		<p>&copy; {new Date().getFullYear()} Help Motivate Me. All rights reserved.</p>
	</footer>
</div>
