<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	function handleGetStarted() {
		if ($auth.user) {
			goto('/dashboard');
		} else {
			goto('/auth/login');
		}
	}
</script>

<div class="min-h-screen flex flex-col">
	<!-- Hero Section -->
	<main class="flex-1 flex items-center justify-center px-4 sm:px-6 lg:px-8">
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
	<footer class="py-6 px-4 text-center text-sm text-gray-500 border-t border-gray-200">
		<p>&copy; {new Date().getFullYear()} Help Motivate Me. All rights reserved.</p>
	</footer>
</div>
