<script lang="ts">
	import { tiers, featureComparison } from '$lib/config/tiers';
	import { t } from 'svelte-i18n';

	let billingPeriod = $state<'monthly' | 'yearly'>('yearly');

	function formatPrice(tier: typeof tiers[0]): string {
		if (tier.monthlyPrice === 0) return '$0';
		
		if (billingPeriod === 'yearly') {
			return `$${tier.yearlyPrice}`;
		}
		return `$${tier.monthlyPrice.toFixed(2)}`;
	}

	function getPriceSubtext(tier: typeof tiers[0]): string {
		if (tier.monthlyPrice === 0) return 'Forever free';
		
		if (billingPeriod === 'yearly') {
			const monthlyEquivalent = (tier.yearlyPrice / 12).toFixed(2);
			return `$${monthlyEquivalent}/mo billed yearly`;
		}
		return 'per month';
	}

	function getEarlyBirdSavings(tier: typeof tiers[0]): string | null {
		if (billingPeriod === 'yearly' && tier.earlyBirdYearlyPrice) {
			return `Early bird: $${tier.earlyBirdYearlyPrice}`;
		}
		return null;
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
	<div class="bg-warm-paper shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
				&larr; Back to Home
			</a>
			<h1 class="text-3xl font-bold text-cocoa-800">Choose the plan that fits your journey</h1>
			<p class="mt-2 text-cocoa-600">
				Simple, transparent pricing. No tracking. No hidden fees.
			</p>
		</div>
	</div>

	<!-- Privacy Callout -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="bg-green-50 border border-green-200 rounded-xl p-4 text-center">
			<p class="text-green-800 font-medium">
				🌱 No tracking cookies. No data selling. Just tools to help you grow.
			</p>
		</div>
	</div>

	<!-- Billing Toggle -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
		<div class="flex justify-center items-center gap-4">
			<span class="text-sm font-medium {billingPeriod === 'monthly' ? 'text-cocoa-800' : 'text-cocoa-500'}">
				Monthly
			</span>
			<button
				onclick={() => billingPeriod = billingPeriod === 'monthly' ? 'yearly' : 'monthly'}
				aria-label="Toggle between monthly and yearly billing"
				class="relative w-14 h-7 bg-gray-200 rounded-full transition-colors {billingPeriod === 'yearly' ? 'bg-primary-600' : ''}"
			>
				<span
					class="absolute top-0.5 left-0.5 w-6 h-6 bg-warm-paper rounded-full shadow transition-transform {billingPeriod === 'yearly' ? 'translate-x-7' : ''}"
				></span>
			</button>
			<span class="text-sm font-medium {billingPeriod === 'yearly' ? 'text-cocoa-800' : 'text-cocoa-500'}">
				Yearly
				<span class="text-green-600 font-semibold ml-1">Save more</span>
			</span>
		</div>
		<p class="text-center text-sm text-cocoa-500 mt-2">
			Most people choose yearly to stay consistent and save money.
		</p>
	</div>

	<!-- Pricing Cards -->
	<div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="grid grid-cols-1 md:grid-cols-3 gap-6">
			{#each tiers as tier}
				<div
					class="relative bg-warm-paper rounded-2xl shadow-sm border-2 p-6 flex flex-col {tier.popular ? 'border-primary-500 shadow-xl scale-105 z-10' : 'border-primary-100'}"
				>
					{#if tier.popular}
						<span
							class="absolute -top-3 left-1/2 -translate-x-1/2 bg-primary-600 text-white text-xs font-semibold px-4 py-1 rounded-full"
						>
							Most Popular
						</span>
					{/if}

					<div class="text-center">
						<h3 class="text-xl font-bold text-cocoa-800">{$t(tier.nameKey)}</h3>
						<p class="text-sm text-cocoa-500 mt-1">{$t(tier.bestForKey)}</p>
						
						<div class="mt-4">
							<span class="text-4xl font-bold text-cocoa-800">{formatPrice(tier)}</span>
							{#if billingPeriod === 'yearly' && tier.monthlyPrice > 0}
								<span class="text-cocoa-500">/year</span>
							{/if}
						</div>
						<p class="text-sm text-cocoa-500 mt-1">{getPriceSubtext(tier)}</p>
						
						{#if getEarlyBirdSavings(tier)}
							<p class="text-sm text-green-600 font-semibold mt-2 bg-green-50 rounded-2xl py-1 px-2 inline-block">
								✨ {getEarlyBirdSavings(tier)}
							</p>
						{/if}
						
						<p class="text-cocoa-600 mt-4 italic">"{$t(tier.descriptionKey)}"</p>
					</div>

					<div class="mt-6 flex-grow">
						<a
							href="/auth/register"
							class="{tier.popular ? 'btn-primary' : 'btn-secondary'} w-full text-center block py-3"
						>
							{tier.id === 'Free' ? 'Get Started Free' : `Get ${$t(tier.nameKey)}`}
						</a>
					</div>
				</div>
			{/each}
		</div>
	</div>

	<!-- Feature Comparison Table -->
	<div class="bg-warm-paper border-y border-primary-100">
		<div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
			<h2 class="text-2xl font-bold text-cocoa-800 mb-8 text-center">Compare Plans</h2>
			
			<!-- Desktop Table -->
			<div class="hidden md:block overflow-x-auto">
				<table class="w-full">
					<thead>
						<tr class="border-b-2 border-primary-100">
							<th class="py-4 px-4 text-left text-sm font-semibold text-cocoa-800">Feature</th>
							{#each tiers as tier}
								<th class="py-4 px-4 text-center text-sm font-semibold {tier.popular ? 'text-primary-600 bg-primary-50 rounded-t-lg' : 'text-cocoa-800'}">
									{$t(tier.nameKey)}
								</th>
							{/each}
						</tr>
					</thead>
					<tbody class="divide-y divide-gray-100">
						{#each featureComparison as feature}
							<tr class="hover:bg-warm-cream">
								<td class="py-4 px-4 text-sm text-cocoa-700 font-medium">{$t(feature.nameKey)}</td>
								<td class="py-4 px-4 text-center">
									{#if typeof feature.free === 'boolean'}
										{#if feature.free}
											<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
											</svg>
										{:else}
											<span class="text-gray-300">—</span>
										{/if}
									{:else}
										<span class="text-sm text-cocoa-600">{feature.free}</span>
									{/if}
								</td>
								<td class="py-4 px-4 text-center {tiers[1].popular ? 'bg-primary-50' : ''}">
									{#if typeof feature.plus === 'boolean'}
										{#if feature.plus}
											<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
											</svg>
										{:else}
											<span class="text-gray-300">—</span>
										{/if}
									{:else}
										<span class="text-sm text-cocoa-600 font-medium">{feature.plus}</span>
									{/if}
								</td>
								<td class="py-4 px-4 text-center">
									{#if typeof feature.pro === 'boolean'}
										{#if feature.pro}
											<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
											</svg>
										{:else}
											<span class="text-gray-300">—</span>
										{/if}
									{:else}
										<span class="text-sm text-cocoa-600 font-medium">{feature.pro}</span>
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

			<!-- Mobile Cards -->
			<div class="md:hidden space-y-6">
				{#each featureComparison as feature}
					<div class="bg-warm-cream rounded-2xl p-4">
						<h4 class="font-medium text-cocoa-800 mb-3">{$t(feature.nameKey)}</h4>
						<div class="grid grid-cols-3 gap-2 text-center text-sm">
							<div>
								<span class="text-xs text-cocoa-500 block mb-1">Free</span>
								{#if typeof feature.free === 'boolean'}
									{#if feature.free}
										<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
										</svg>
									{:else}
										<span class="text-gray-300">—</span>
									{/if}
								{:else}
									<span class="text-cocoa-700">{feature.free}</span>
								{/if}
							</div>
							<div class="bg-primary-100 rounded-2xl py-1">
								<span class="text-xs text-primary-700 block mb-1">Plus</span>
								{#if typeof feature.plus === 'boolean'}
									{#if feature.plus}
										<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
										</svg>
									{:else}
										<span class="text-gray-300">—</span>
									{/if}
								{:else}
									<span class="text-cocoa-700 font-medium">{feature.plus}</span>
								{/if}
							</div>
							<div>
								<span class="text-xs text-cocoa-500 block mb-1">Pro</span>
								{#if typeof feature.pro === 'boolean'}
									{#if feature.pro}
										<svg class="w-5 h-5 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
										</svg>
									{:else}
										<span class="text-gray-300">—</span>
									{/if}
								{:else}
									<span class="text-cocoa-700 font-medium">{feature.pro}</span>
								{/if}
							</div>
						</div>
					</div>
				{/each}
			</div>
		</div>
	</div>

	<!-- Pricing Summary Table -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<h2 class="text-2xl font-bold text-cocoa-800 mb-6 text-center">Pricing Summary</h2>
		<div class="bg-warm-paper rounded-xl shadow-sm border border-primary-100 overflow-hidden">
			<table class="w-full">
				<thead class="bg-warm-cream">
					<tr>
						<th class="py-4 px-6 text-left text-sm font-semibold text-cocoa-800"></th>
						<th class="py-4 px-6 text-center text-sm font-semibold text-cocoa-800">Monthly</th>
						<th class="py-4 px-6 text-center text-sm font-semibold text-cocoa-800">Yearly</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-gray-100">
					{#each tiers as tier}
						<tr class="{tier.popular ? 'bg-primary-50' : ''}">
							<td class="py-4 px-6 font-semibold text-cocoa-800">{$t(tier.nameKey)}</td>
							<td class="py-4 px-6 text-center text-cocoa-600">
								{tier.monthlyPrice === 0 ? '$0' : `$${tier.monthlyPrice.toFixed(2)}`}
							</td>
							<td class="py-4 px-6 text-center">
								<span class="font-semibold text-cocoa-800">
									{tier.yearlyPrice === 0 ? '$0' : `$${tier.yearlyPrice}/year`}
								</span>
								{#if tier.earlyBirdYearlyPrice}
									<span class="block text-sm text-green-600 font-medium">
										Early bird: ${tier.earlyBirdYearlyPrice}
									</span>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>

	<!-- FAQ Section -->
	<div class="bg-warm-paper border-y border-primary-100">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
			<h2 class="text-2xl font-bold text-cocoa-800 mb-8 text-center">Frequently Asked Questions</h2>
			<div class="space-y-6">
				<div>
					<h3 class="font-semibold text-cocoa-800">Can I change plans later?</h3>
					<p class="text-cocoa-600 mt-1">Yes, you can upgrade or downgrade your plan at any time from your account settings.</p>
				</div>
				<div>
					<h3 class="font-semibold text-cocoa-800">What payment methods do you accept?</h3>
					<p class="text-cocoa-600 mt-1">We accept major credit cards. Payment processing is handled securely.</p>
				</div>
				<div>
					<h3 class="font-semibold text-cocoa-800">Is there a free trial for paid plans?</h3>
					<p class="text-cocoa-600 mt-1">The Free plan lets you experience the core features. You can upgrade when you're ready for more.</p>
				</div>
				<div>
					<h3 class="font-semibold text-cocoa-800">What happens if I cancel?</h3>
					<p class="text-cocoa-600 mt-1">You'll retain access until the end of your billing period. Your data stays safe, and you can always resubscribe.</p>
				</div>
				<div>
					<h3 class="font-semibold text-cocoa-800">Do you offer refunds?</h3>
					<p class="text-cocoa-600 mt-1">Yes, we offer refunds on a case-by-case basis. Contact us within 14 days of a charge to request one.</p>
				</div>
			</div>
		</div>
	</div>

	<!-- CTA Section -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="text-center">
			<h2 class="text-xl font-bold text-cocoa-800 mb-4">Still have questions?</h2>
			<p class="text-cocoa-600 mb-6">We're happy to help you find the right plan for your needs.</p>
			<a
				href="/contact"
				class="inline-flex items-center justify-center px-6 py-3 border border-primary-200 text-base font-medium rounded-2xl text-cocoa-700 bg-warm-paper hover:bg-warm-cream transition-colors"
			>
				Contact Us
			</a>
		</div>
	</div>

	<!-- Note -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
		<p class="text-xs text-cocoa-500 text-center">
			Note: Payment integration will be added in a future update. Currently, all features are available during beta.
		</p>
	</div>
</div>
