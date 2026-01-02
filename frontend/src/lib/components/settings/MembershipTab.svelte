<script lang="ts">
	import { auth } from '$lib/stores/auth';
	import { updateMembership } from '$lib/api/settings';
	import type { MembershipTier } from '$lib/types/auth';

	let loading = $state<MembershipTier | null>(null);
	let error = $state('');

	const tiers = [
		{
			id: 'Free' as const,
			name: 'Free',
			price: '$0',
			description: 'Get started with the basics',
			features: [
				'Up to 5 goals',
				'Basic habit tracking',
				'Daily journal entries',
				'Community support'
			]
		},
		{
			id: 'Plus' as const,
			name: 'Plus',
			price: '$9/mo',
			description: 'For serious habit builders',
			features: [
				'Unlimited goals',
				'Advanced analytics',
				'Priority support',
				'Custom themes',
				'Export data'
			],
			popular: true
		},
		{
			id: 'Pro' as const,
			name: 'Pro',
			price: '$19/mo',
			description: 'For power users and teams',
			features: [
				'Everything in Plus',
				'API access',
				'Team features',
				'White-label options',
				'Dedicated support'
			]
		}
	];

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
</script>

<div>
	<h2 class="text-lg font-semibold text-gray-900 mb-2">Membership</h2>
	<p class="text-sm text-gray-600 mb-6">
		Current plan: <span class="font-medium text-primary-600">{$auth.user?.membershipTier}</span>
	</p>

	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-6">
			{error}
		</div>
	{/if}

	<div class="grid gap-4 md:grid-cols-3">
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
						? 'border-primary-200'
						: 'border-gray-200'}"
			>
				{#if tier.popular && !isCurrentTier}
					<span
						class="absolute -top-3 left-1/2 -translate-x-1/2 bg-primary-600 text-white text-xs font-medium px-3 py-1 rounded-full"
					>
						Popular
					</span>
				{/if}

				<h3 class="text-lg font-semibold text-gray-900">{tier.name}</h3>
				<p class="text-2xl font-bold text-gray-900 mt-1">{tier.price}</p>
				<p class="text-sm text-gray-500 mt-1">{tier.description}</p>

				<ul class="mt-4 space-y-2">
					{#each tier.features as feature}
						<li class="flex items-start gap-2 text-sm text-gray-600">
							<svg
								class="w-5 h-5 text-green-500 flex-shrink-0"
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
							{feature}
						</li>
					{/each}
				</ul>

				<div class="mt-5">
					{#if isCurrentTier}
						<button disabled class="btn-secondary w-full cursor-default"> Current Plan </button>
					{:else if isUpgrade}
						<button
							onclick={() => handleUpgrade(tier.id)}
							disabled={loading !== null}
							class="btn-primary w-full"
						>
							{loading === tier.id ? 'Upgrading...' : `Upgrade to ${tier.name}`}
						</button>
					{:else if isDowngrade}
						<button
							onclick={() => handleUpgrade(tier.id)}
							disabled={loading !== null}
							class="btn-secondary w-full"
						>
							{loading === tier.id ? 'Changing...' : `Switch to ${tier.name}`}
						</button>
					{/if}
				</div>
			</div>
		{/each}
	</div>

	<p class="text-xs text-gray-500 mt-6 text-center">
		Note: This is a placeholder. Payment integration will be added in a future update.
	</p>
</div>
