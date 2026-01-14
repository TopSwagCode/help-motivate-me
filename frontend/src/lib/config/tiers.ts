import type { MembershipTier } from '$lib/types/auth';

export interface TierConfig {
	id: MembershipTier;
	name: string;
	bestFor: string;
	description: string;
	monthlyPrice: number;
	yearlyPrice: number;
	earlyBirdYearlyPrice?: number;
	popular?: boolean;
}

export interface FeatureComparison {
	name: string;
	free: string | boolean;
	plus: string | boolean;
	pro: string | boolean;
}

export const tiers: TierConfig[] = [
	{
		id: 'Free',
		name: 'Free',
		bestFor: 'Getting started',
		description: 'Start becoming someone.',
		monthlyPrice: 0,
		yearlyPrice: 0
	},
	{
		id: 'Plus',
		name: 'Plus',
		bestFor: 'Building strong identities',
		description: 'Turn consistency into identity.',
		monthlyPrice: 5.99,
		yearlyPrice: 49,
		earlyBirdYearlyPrice: 39,
		popular: true
	},
	{
		id: 'Pro',
		name: 'Pro',
		bestFor: 'Full transformation',
		description: 'Accelerate change with AI support.',
		monthlyPrice: 9.99,
		yearlyPrice: 89
	}
];

export const featureComparison: FeatureComparison[] = [
	{ name: 'Identities', free: '1', plus: 'Unlimited', pro: 'Unlimited' },
	{ name: 'Habit stacks', free: '1–2', plus: 'Unlimited', pro: 'Unlimited' },
	{ name: 'Goals & tasks', free: 'Limited', plus: 'Unlimited', pro: 'Unlimited' },
	{ name: 'Daily Identity Commitment', free: true, plus: true, pro: true },
	{ name: 'Identity Score & history', free: false, plus: true, pro: true },
	{ name: 'Analytics & insights', free: false, plus: true, pro: true },
	{ name: 'Accountability buddies', free: '1', plus: 'Unlimited', pro: 'Unlimited' },
	{ name: 'AI assistant', free: false, plus: false, pro: 'Fair use' },
	{ name: 'Smart identity nudges', free: false, plus: true, pro: true },
	{ name: 'Advanced journaling', free: false, plus: true, pro: true },
	{ name: 'Priority access & features', free: false, plus: false, pro: true }
];
