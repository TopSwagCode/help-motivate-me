import type { MembershipTier } from '$lib/types/auth';

export interface TierConfig {
	id: MembershipTier;
	nameKey: string;
	bestForKey: string;
	descriptionKey: string;
	monthlyPrice: number;
	yearlyPrice: number;
	earlyBirdYearlyPrice?: number;
	popular?: boolean;
}

export interface FeatureComparison {
	nameKey: string;
	free: string | boolean;
	plus: string | boolean;
	pro: string | boolean;
}

export const tiers: TierConfig[] = [
	{
		id: 'Free',
		nameKey: 'settings.membership.tiers.free.name',
		bestForKey: 'settings.membership.tiers.free.bestFor',
		descriptionKey: 'settings.membership.tiers.free.description',
		monthlyPrice: 0,
		yearlyPrice: 0
	},
	{
		id: 'Plus',
		nameKey: 'settings.membership.tiers.plus.name',
		bestForKey: 'settings.membership.tiers.plus.bestFor',
		descriptionKey: 'settings.membership.tiers.plus.description',
		monthlyPrice: 5.99,
		yearlyPrice: 49,
		earlyBirdYearlyPrice: 39,
		popular: true
	},
	{
		id: 'Pro',
		nameKey: 'settings.membership.tiers.pro.name',
		bestForKey: 'settings.membership.tiers.pro.bestFor',
		descriptionKey: 'settings.membership.tiers.pro.description',
		monthlyPrice: 9.99,
		yearlyPrice: 89
	}
];

export const featureComparison: FeatureComparison[] = [
	{ nameKey: 'settings.membership.features.identities', free: '1', plus: 'unlimited', pro: 'unlimited' },
	{ nameKey: 'settings.membership.features.habitStacks', free: '1–2', plus: 'unlimited', pro: 'unlimited' },
	{ nameKey: 'settings.membership.features.goalsAndTasks', free: 'limited', plus: 'unlimited', pro: 'unlimited' },
	{ nameKey: 'settings.membership.features.dailyIdentityCommitment', free: true, plus: true, pro: true },
	{ nameKey: 'settings.membership.features.identityScoreHistory', free: false, plus: true, pro: true },
	{ nameKey: 'settings.membership.features.analyticsInsights', free: false, plus: true, pro: true },
	{ nameKey: 'settings.membership.features.accountabilityBuddies', free: '1', plus: 'unlimited', pro: 'unlimited' },
	{ nameKey: 'settings.membership.features.aiAssistant', free: false, plus: false, pro: 'fairUse' },
	{ nameKey: 'settings.membership.features.smartIdentityNudges', free: false, plus: true, pro: true },
	{ nameKey: 'settings.membership.features.advancedJournaling', free: false, plus: true, pro: true },
	{ nameKey: 'settings.membership.features.priorityAccess', free: false, plus: false, pro: true }
];
