import type { TourStep } from '$lib/types/tour';

export const tourSteps: TourStep[] = [
	// TODAY PAGE - Step 1: Daily Commitment
	{
		id: 'today-commitment',
		page: '/today',
		element: '[data-tour="daily-commitment"]',
		titleKey: 'tour.today.commitment.title',
		descriptionKey: 'tour.today.commitment.description',
		position: 'bottom'
	},
	// TODAY PAGE - Step 2: Identity Progress
	{
		id: 'today-identity-progress',
		page: '/today',
		element: '[data-tour="identity-progress"]',
		titleKey: 'tour.today.identityProgress.title',
		descriptionKey: 'tour.today.identityProgress.description',
		position: 'bottom'
	},
	// TODAY PAGE - Step 3: Habit Stacks
	{
		id: 'today-habits',
		page: '/today',
		element: '[data-tour="habit-stacks"]',
		titleKey: 'tour.today.habits.title',
		descriptionKey: 'tour.today.habits.description',
		position: 'bottom'
	},
	// TODAY PAGE - Step 4: Tasks
	{
		id: 'today-tasks',
		page: '/today',
		element: '[data-tour="tasks-section"]',
		titleKey: 'tour.today.tasks.title',
		descriptionKey: 'tour.today.tasks.description',
		position: 'top'
	},
	// TODAY PAGE - Step 5: AI Assistant
	{
		id: 'today-ai-assistant',
		page: '/today',
		element: '[data-tour="ai-assistant"]',
		titleKey: 'tour.today.aiAssistant.title',
		descriptionKey: 'tour.today.aiAssistant.description',
		position: 'top'
	},
	// GOALS PAGE
	{
		id: 'goals-overview',
		page: '/goals',
		element: '[data-tour="goals-list"]',
		titleKey: 'tour.goals.overview.title',
		descriptionKey: 'tour.goals.overview.description',
		position: 'bottom'
	},
	// HABIT STACKS PAGE
	{
		id: 'stacks-overview',
		page: '/habit-stacks',
		element: '[data-tour="stacks-list"]',
		titleKey: 'tour.habitStacks.overview.title',
		descriptionKey: 'tour.habitStacks.overview.description',
		position: 'bottom'
	},
	// JOURNAL PAGE
	{
		id: 'journal-overview',
		page: '/journal',
		element: '[data-tour="journal-feed"]',
		titleKey: 'tour.journal.overview.title',
		descriptionKey: 'tour.journal.overview.description',
		position: 'top'
	},
	// IDENTITIES PAGE
	{
		id: 'identities-overview',
		page: '/identities',
		element: '[data-tour="identities-list"]',
		titleKey: 'tour.identities.overview.title',
		descriptionKey: 'tour.identities.overview.description',
		position: 'bottom'
	},
	// BUDDIES PAGE
	{
		id: 'buddies-overview',
		page: '/buddies',
		element: '[data-tour="buddies-section"]',
		titleKey: 'tour.buddies.overview.title',
		descriptionKey: 'tour.buddies.overview.description',
		position: 'bottom'
	}
];
