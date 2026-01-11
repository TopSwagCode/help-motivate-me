<script lang="ts">
	const values = [
		{
			title: 'Privacy First',
			description: 'No tracking cookies. No data selling. No advertising partners. Your information stays yours.',
			icon: 'shield'
		},
		{
			title: 'User Respect',
			description: 'We build features that help you, not features that extract value from you. You are not the product.',
			icon: 'heart'
		},
		{
			title: 'Transparency',
			description: 'Clear pricing. Honest policies. We tell you exactly what we do with your data and why.',
			icon: 'eye'
		},
		{
			title: 'Simplicity',
			description: 'Tools should help, not overwhelm. We focus on what matters for your growth.',
			icon: 'sparkles'
		}
	];
</script>

<svelte:head>
	<title>About - Help Motivate Me</title>
	<meta
		name="description"
		content="Learn about Help Motivate Me - a privacy-first productivity app built to help you achieve your goals"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-white shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
				&larr; Back to Home
			</a>
			<h1 class="text-3xl font-bold text-gray-900">About Us</h1>
			<p class="mt-2 text-gray-600">
				Building tools that respect users while helping them grow
			</p>
		</div>
	</div>

	<!-- Hero Section -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="text-center">
			<h2 class="text-2xl sm:text-3xl font-bold text-gray-900">
				Productivity tools that respect your privacy
			</h2>
			<p class="mt-4 text-lg text-gray-600 max-w-2xl mx-auto">
				Help Motivate Me is a goal-setting and habit-building platform designed for people who care about their personal data as much as their personal growth.
			</p>
		</div>
	</div>

	<!-- Our Story -->
	<div class="bg-white border-y border-gray-200">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
			<h2 class="text-2xl font-bold text-gray-900 mb-6">Our Story</h2>
			<div class="prose prose-lg max-w-none text-gray-600">
				<p>
					We built Help Motivate Me because we were tired of productivity apps that treat users as products. Too many apps track your every move, sell your data to advertisers, and optimize for engagement over actual results.
				</p>
				<p class="mt-4">
					We wanted something different: a tool that genuinely helps people achieve their goals without compromising their privacy. No tracking cookies. No data brokers. No dark patterns designed to keep you scrolling.
				</p>
				<p class="mt-4">
					Our approach combines proven behavioral science (identity-based habits, habit stacking, goal decomposition) with AI assistance that respects your boundaries. When you use AI features, we only send the text you explicitly provide - never your personal information.
				</p>
				<p class="mt-4">
					This is productivity software built on trust, not surveillance.
				</p>
			</div>
		</div>
	</div>

	<!-- Our Values -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<h2 class="text-2xl font-bold text-gray-900 mb-8 text-center">Our Values</h2>
		<div class="grid grid-cols-1 md:grid-cols-2 gap-6">
			{#each values as value}
				<div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
					<div class="flex items-start gap-4">
						<div class="w-12 h-12 bg-primary-100 rounded-lg flex items-center justify-center flex-shrink-0">
							{#if value.icon === 'shield'}
								<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
								</svg>
							{:else if value.icon === 'heart'}
								<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
								</svg>
							{:else if value.icon === 'eye'}
								<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
								</svg>
							{:else if value.icon === 'sparkles'}
								<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
								</svg>
							{/if}
						</div>
						<div>
							<h3 class="text-lg font-semibold text-gray-900">{value.title}</h3>
							<p class="mt-1 text-gray-600">{value.description}</p>
						</div>
					</div>
				</div>
			{/each}
		</div>
	</div>

	<!-- How We're Different -->
	<div class="bg-white border-y border-gray-200">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
			<h2 class="text-2xl font-bold text-gray-900 mb-6">How We're Different</h2>
			<div class="space-y-6">
				<div class="flex items-start gap-4">
					<div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
						<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<h3 class="font-semibold text-gray-900">No tracking cookies</h3>
						<p class="text-gray-600">We use only essential session cookies. No analytics tracking, no advertising pixels, no behavioral profiling.</p>
					</div>
				</div>
				<div class="flex items-start gap-4">
					<div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
						<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<h3 class="font-semibold text-gray-900">Your data stays yours</h3>
						<p class="text-gray-600">We never sell, rent, or share your personal information with third parties. Period.</p>
					</div>
				</div>
				<div class="flex items-start gap-4">
					<div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
						<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<h3 class="font-semibold text-gray-900">Responsible AI</h3>
						<p class="text-gray-600">AI features use only the text you explicitly provide. We never send your email, age, address, or other personal info to AI services.</p>
					</div>
				</div>
				<div class="flex items-start gap-4">
					<div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
						<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<h3 class="font-semibold text-gray-900">Sustainable business model</h3>
						<p class="text-gray-600">We make money from subscriptions, not from selling your attention to advertisers. Your success is our success.</p>
					</div>
				</div>
			</div>
		</div>
	</div>

	<!-- CTA Section -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="bg-gradient-to-r from-indigo-600 to-purple-600 rounded-xl p-8 text-center">
			<h2 class="text-2xl font-bold text-white mb-4">Ready to Start Your Journey?</h2>
			<p class="text-indigo-100 mb-6 max-w-2xl mx-auto">
				Join a community of people building better habits while keeping their privacy intact.
			</p>
			<div class="flex flex-col sm:flex-row gap-4 justify-center">
				<a
					href="/auth/register"
					class="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-lg text-indigo-600 bg-white hover:bg-gray-50 transition-colors"
				>
					Create Free Account
				</a>
				<a
					href="/pricing"
					class="inline-flex items-center justify-center px-6 py-3 border-2 border-white text-base font-medium rounded-lg text-white hover:bg-white hover:text-indigo-600 transition-colors"
				>
					View Pricing
				</a>
			</div>
		</div>
	</div>

	<!-- Footer Links -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
		<div class="flex flex-col sm:flex-row gap-4 justify-center text-center">
			<a href="/privacy" class="text-primary-600 hover:text-primary-700 font-medium">
				Privacy Policy
			</a>
			<span class="hidden sm:inline text-gray-300">|</span>
			<a href="/terms" class="text-primary-600 hover:text-primary-700 font-medium">
				Terms of Service
			</a>
			<span class="hidden sm:inline text-gray-300">|</span>
			<a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium">
				Contact Us
			</a>
		</div>
	</div>
</div>
