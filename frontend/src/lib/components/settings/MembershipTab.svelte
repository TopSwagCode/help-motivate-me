<script lang="ts">
	import { auth } from '$lib/stores/auth';
	import { updateMembership } from '$lib/api/settings';
	import { tiers, featureComparison } from '$lib/config/tiers';
	import type { MembershipTier } from '$lib/types/auth';

	let loading = $state<MembershipTier | null>(null);
	let error = $state('');
	let billingPeriod = $state<'monthly' | 'yearly'>('yearly');

	async function handleUpgrade(tier: MembershipTier) {
		if (tier === $auth.user?.membershipTier) return;

		loading = tier;
		error = '';

		try {
			const updatedUser = await updateMembership({ tier });
			auth.updateUser(updatedUser);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update membership';
		} finally {
			loading = null;
		}
	}

	function getTierIndex(tier: MembershipTier): number {
		return tiers.findIndex((t) => t.id === tier);
	}

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

	function getFeatureValue(featureName: string, tierId: MembershipTier): string | boolean {
		const feature = featureComparison.find(f => f.name === featureName);
		if (!feature) return false;
		
		switch (tierId) {
			case 'Free': return feature.free;
			case 'Plus': return feature.plus;
			case 'Pro': return feature.pro;
			default: return false;
		}
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-cocoa-800 mb-2">Membership</h2>
	<p class="text-sm text-cocoa-600 mb-6">
		Current plan: <span class="font-medium text-primary-600">{$auth.user?.membershipTier}</span>
	</p>

	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-6">
			{error}
		</div>
	{/if}

	<!-- Billing Toggle -->
	<div class="flex justify-center items-center gap-4 mb-6">
		<span class="text-sm font-medium {billingPeriod === 'monthly' ? 'text-cocoa-800' : 'text-cocoa-500'}">
			Monthly
		</span>
		<button
			onclick={() => billingPeriod = billingPeriod === 'monthly' ? 'yearly' : 'monthly'}
			aria-label="Toggle between monthly and yearly billing"
			class="relative w-12 h-6 bg-gray-200 rounded-full transition-colors {billingPeriod === 'yearly' ? 'bg-primary-600' : ''}"
		>
			<span
				class="absolute top-0.5 left-0.5 w-5 h-5 bg-warm-paper rounded-full shadow transition-transform {billingPeriod === 'yearly' ? 'translate-x-6' : ''}"
			></span>
		</button>
		<span class="text-sm font-medium {billingPeriod === 'yearly' ? 'text-cocoa-800' : 'text-cocoa-500'}">
			Yearly <span class="text-green-600 text-xs">Save more</span>
		</span>
	</div>

	<!-- Pricing Cards -->
	<div class="grid gap-4 md:grid-cols-3 mb-8">
		{#each tiers as tier}
			{@const isCurrentTier = tier.id === $auth.user?.membershipTier}
			{@const currentIndex = getTierIndex($auth.user?.membershipTier ?? 'Free')}
			{@const tierIndex = getTierIndex(tier.id)}
			{@const isUpgrade = tierIndex > currentIndex}
			{@const isDowngrade = tierIndex < currentIndex}

			<div
				class="relative rounded-xl border-2 p-5 {isCurrentTier
					? 'border-primary-500 bg-primary-50'
					: tier.popular
						? 'border-primary-200 shadow-md'
						: 'border-primary-100'}"
			>
				{#if tier.popular && !isCurrentTier}
					<span
						class="absolute -top-3 left-1/2 -translate-x-1/2 bg-primary-600 text-white text-xs font-medium px-3 py-1 rounded-full"
					>
						Popular
					</span>
				{/if}

				<div class="text-center">
					<h3 class="text-lg font-semibold text-cocoa-800">{tier.name}</h3>
					<p class="text-xs text-cocoa-500 mt-1">{tier.bestFor}</p>
					
					<div class="mt-3">
						<span class="text-2xl font-bold text-cocoa-800">{formatPrice(tier)}</span>
						{#if billingPeriod === 'yearly' && tier.monthlyPrice > 0}
							<span class="text-cocoa-500 text-sm">/year</span>
						{/if}
					</div>
					<p class="text-xs text-cocoa-500">{getPriceSubtext(tier)}</p>
					
					{#if billingPeriod === 'yearly' && tier.earlyBirdYearlyPrice}
						<p class="text-xs text-green-600 font-medium mt-1">
							✨ Early bird: ${tier.earlyBirdYearlyPrice}
						</p>
					{/if}
					
					<p class="text-sm text-cocoa-600 mt-2 italic">"{tier.description}"</p>
				</div>

				<div class="mt-4">
					{#if isCurrentTier}
						<button disabled class="btn-secondary w-full cursor-default text-sm py-2"> 
							Current Plan 
						</button>
					{:else if isUpgrade}
						<button
							onclick={() => handleUpgrade(tier.id)}
							disabled={loading !== null}
							class="btn-primary w-full text-sm py-2"
						>
							{loading === tier.id ? 'Upgrading...' : `Upgrade to ${tier.name}`}
						</button>
					{:else if isDowngrade}
						<button
							onclick={() => handleUpgrade(tier.id)}
							disabled={loading !== null}
							class="btn-secondary w-full text-sm py-2"
						>
							{loading === tier.id ? 'Changing...' : `Switch to ${tier.name}`}
						</button>
					{/if}
				</div>
			</div>
		{/each}
	</div>

	<!-- Feature Comparison -->
	<div class="bg-warm-cream rounded-xl p-4">
		<h3 class="font-semibold text-cocoa-800 mb-4 text-center">Compare Plans</h3>
		
		<!-- Desktop Table -->
		<div class="hidden md:block overflow-x-auto">
			<table class="w-full text-sm">
				<thead>
					<tr class="border-b border-primary-100">
						<th class="py-2 px-3 text-left text-cocoa-700 font-medium">Feature</th>
						{#each tiers as tier}
							<th class="py-2 px-3 text-center font-medium {tier.id === $auth.user?.membershipTier ? 'text-primary-600 bg-primary-100 rounded-t' : 'text-cocoa-700'}">
								{tier.name}
							</th>
						{/each}
					</tr>
				</thead>
				<tbody class="divide-y divide-gray-100">
					{#each featureComparison as feature}
						<tr>
							<td class="py-2 px-3 text-cocoa-600">{feature.name}</td>
							{#each tiers as tier}
								{@const value = getFeatureValue(feature.name, tier.id)}
								<td class="py-2 px-3 text-center {tier.id === $auth.user?.membershipTier ? 'bg-primary-50' : ''}">
									{#if typeof value === 'boolean'}
										{#if value}
											<svg class="w-4 h-4 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
											</svg>
										{:else}
											<span class="text-gray-300">—</span>
										{/if}
									{:else}
										<span class="text-cocoa-600">{value}</span>
									{/if}
								</td>
							{/each}
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<!-- Mobile List -->
		<div class="md:hidden space-y-3">
			{#each featureComparison as feature}
				<div class="bg-warm-paper rounded-2xl p-3">
					<p class="font-medium text-gray-800 text-sm mb-2">{feature.name}</p>
					<div class="grid grid-cols-3 gap-2 text-xs text-center">
						{#each tiers as tier}
							{@const value = getFeatureValue(feature.name, tier.id)}
							<div class="{tier.id === $auth.user?.membershipTier ? 'bg-primary-100 rounded py-1' : 'py-1'}">
								<span class="text-cocoa-500 block">{tier.name}</span>
								{#if typeof value === 'boolean'}
									{#if value}
										<svg class="w-4 h-4 text-green-500 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
										</svg>
									{:else}
										<span class="text-gray-300">—</span>
									{/if}
								{:else}
									<span class="text-cocoa-700 font-medium">{value}</span>
								{/if}
							</div>
						{/each}
					</div>
				</div>
			{/each}
		</div>
	</div>

	<p class="text-xs text-cocoa-500 mt-6 text-center">
		Note: This is a placeholder. Payment integration will be added in a future update.
	</p>
</div>
