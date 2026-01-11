<script lang="ts">
	import { tiers } from '$lib/config/tiers';

	let billingPeriod = $state<'monthly' | 'annual'>('monthly');

	function getPrice(price: string): string {
		if (price === '$0') return '$0';

		// Extract number from price string
		const match = price.match(/\$(\d+)/);
		if (!match) return price;

		const monthlyPrice = parseInt(match[1]);

		if (billingPeriod === 'annual') {
			const annualPrice = Math.floor(monthlyPrice * 12 * 0.8); // 20% discount
			return `$${Math.floor(annualPrice / 12)}/mo`;
		}
		return price;
	}

	function getAnnualTotal(price: string): string | null {
		if (price === '$0' || billingPeriod !== 'annual') return null;

		const match = price.match(/\$(\d+)/);
		if (!match) return null;

		const monthlyPrice = parseInt(match[1]);
		const annualPrice = Math.floor(monthlyPrice * 12 * 0.8);
		return `$${annualPrice}/year`;
	}
</script>

<svelte:head>
	<title>Pricing - Help Motivate Me</title>
	<meta
		name="description"
		content="Simple, transparent pricing for Help Motivate Me - no tracking, no hidden fees"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-white shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
				&larr; Back to Home
			</a>
			<h1 class="text-3xl font-bold text-gray-900">Pricing</h1>
			<p class="mt-2 text-gray-600">
				Simple, transparent pricing. No tracking. No hidden fees.
			</p>
		</div>
	</div>

	<!-- Privacy Callout -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="bg-green-50 border border-green-200 rounded-xl p-4 text-center">
			<p class="text-green-800 font-medium">
				No tracking cookies. No data selling. Just tools to help you grow.
			</p>
		</div>
	</div>

	<!-- Billing Toggle -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
		<div class="flex justify-center items-center gap-4">
			<span class="text-sm font-medium {billingPeriod === 'monthly' ? 'text-gray-900' : 'text-gray-500'}">
				Monthly
			</span>
			<button
				onclick={() => billingPeriod = billingPeriod === 'monthly' ? 'annual' : 'monthly'}
				class="relative w-14 h-7 bg-gray-200 rounded-full transition-colors {billingPeriod === 'annual' ? 'bg-primary-600' : ''}"
			>
				<span
					class="absolute top-0.5 left-0.5 w-6 h-6 bg-white rounded-full shadow transition-transform {billingPeriod === 'annual' ? 'translate-x-7' : ''}"
				></span>
			</button>
			<span class="text-sm font-medium {billingPeriod === 'annual' ? 'text-gray-900' : 'text-gray-500'}">
				Annual
				<span class="text-green-600 font-semibold ml-1">Save 20%</span>
			</span>
		</div>
	</div>

	<!-- Pricing Cards -->
	<div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="grid grid-cols-1 md:grid-cols-3 gap-8">
			{#each tiers as tier}
				<div
					class="relative bg-white rounded-2xl shadow-sm border-2 p-6 flex flex-col {tier.popular ? 'border-primary-500 shadow-lg' : 'border-gray-200'}"
				>
					{#if tier.popular}
						<span
							class="absolute -top-3 left-1/2 -translate-x-1/2 bg-primary-600 text-white text-xs font-semibold px-4 py-1 rounded-full"
						>
							Most Popular
						</span>
					{/if}

					<div class="text-center">
						<h3 class="text-xl font-bold text-gray-900">{tier.name}</h3>
						<div class="mt-4">
							<span class="text-4xl font-bold text-gray-900">{getPrice(tier.price)}</span>
							{#if tier.price !== '$0'}
								<span class="text-gray-500 ml-1">
									{billingPeriod === 'annual' ? 'billed annually' : ''}
								</span>
							{/if}
						</div>
						{#if getAnnualTotal(tier.price)}
							<p class="text-sm text-gray-500 mt-1">{getAnnualTotal(tier.price)}</p>
						{/if}
						<p class="text-gray-600 mt-2">{tier.description}</p>
					</div>

					<ul class="mt-6 space-y-3 flex-grow">
						{#each tier.features as feature}
							<li class="flex items-start gap-3">
								<svg
									class="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M5 13l4 4L19 7"
									/>
								</svg>
								<span class="text-gray-600">{feature}</span>
							</li>
						{/each}
					</ul>

					<div class="mt-8">
						{#if tier.id === 'Free'}
							<a
								href="/auth/register"
								class="btn-secondary w-full text-center block py-3"
							>
								Get Started Free
							</a>
						{:else}
							<a
								href="/auth/register"
								class="{tier.popular ? 'btn-primary' : 'btn-secondary'} w-full text-center block py-3"
							>
								Get {tier.name}
							</a>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	</div>

	<!-- FAQ Section -->
	<div class="bg-white border-y border-gray-200">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
			<h2 class="text-2xl font-bold text-gray-900 mb-8 text-center">Frequently Asked Questions</h2>
			<div class="space-y-6">
				<div>
					<h3 class="font-semibold text-gray-900">Can I change plans later?</h3>
					<p class="text-gray-600 mt-1">Yes, you can upgrade or downgrade your plan at any time from your account settings.</p>
				</div>
				<div>
					<h3 class="font-semibold text-gray-900">What payment methods do you accept?</h3>
					<p class="text-gray-600 mt-1">We accept major credit cards. Payment processing is handled securely.</p>
				</div>
				<div>
					<h3 class="font-semibold text-gray-900">Is there a free trial for paid plans?</h3>
					<p class="text-gray-600 mt-1">The Free plan lets you experience the core features. You can upgrade when you're ready for more.</p>
				</div>
				<div>
					<h3 class="font-semibold text-gray-900">What happens if I cancel?</h3>
					<p class="text-gray-600 mt-1">You'll retain access until the end of your billing period. Your data stays safe, and you can always resubscribe.</p>
				</div>
				<div>
					<h3 class="font-semibold text-gray-900">Do you offer refunds?</h3>
					<p class="text-gray-600 mt-1">Yes, we offer refunds on a case-by-case basis. Contact us within 14 days of a charge to request one.</p>
				</div>
			</div>
		</div>
	</div>

	<!-- CTA Section -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="text-center">
			<h2 class="text-xl font-bold text-gray-900 mb-4">Still have questions?</h2>
			<p class="text-gray-600 mb-6">We're happy to help you find the right plan for your needs.</p>
			<a
				href="/contact"
				class="inline-flex items-center justify-center px-6 py-3 border border-gray-300 text-base font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 transition-colors"
			>
				Contact Us
			</a>
		</div>
	</div>

	<!-- Note -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
		<p class="text-xs text-gray-500 text-center">
			Note: Payment integration will be added in a future update. Currently, all features are available during beta.
		</p>
	</div>
</div>
