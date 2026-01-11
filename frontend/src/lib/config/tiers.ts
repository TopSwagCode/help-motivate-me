import type { MembershipTier } from '$lib/types/auth';

export interface TierConfig {
	id: MembershipTier;
	name: string;
	price: string;
	description: string;
	features: string[];
	popular?: boolean;
}

export const tiers: TierConfig[] = [
	{
		id: 'Free',
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
		id: 'Plus',
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
		id: 'Pro',
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
