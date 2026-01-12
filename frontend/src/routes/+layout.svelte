<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import TopNav from '$lib/components/layout/TopNav.svelte';
	import CommandBar from '$lib/components/ai/CommandBar.svelte';
	import { initI18n, setLocale, getLocaleFromLanguage } from '$lib/i18n';
	import { isLoading as i18nLoading } from 'svelte-i18n';
	import { createGoal, getGoals } from '$lib/api/goals';
	import { createTask } from '$lib/api/tasks';
	import { createHabitStack } from '$lib/api/habitStacks';
	import type {
		TaskPreviewData,
		GoalPreviewData,
		HabitStackPreviewData
	} from '$lib/api/aiGeneral';

	// Initialize i18n
	initI18n();

	let { children } = $props();

	// Command bar state
	let commandBarOpen = $state(false);

	// Sync locale with user's preferred language when auth state changes
	$effect(() => {
		if ($auth.user?.preferredLanguage) {
			const userLocale = getLocaleFromLanguage($auth.user.preferredLanguage);
			setLocale(userLocale);
		}
	});

	// Routes that should NOT show the nav
	const publicRoutes = ['/', '/auth/login', '/auth/register', '/auth/callback', '/faq', '/privacy', '/terms', '/pricing', '/about', '/contact'];

	function shouldShowNav(): boolean {
		const path = $page.url.pathname;
		return $auth.user !== null && !publicRoutes.includes(path);
	}

	function shouldShowCommandBar(): boolean {
		const path = $page.url.pathname;
		return $auth.user !== null && !publicRoutes.includes(path);
	}

	// Global keyboard shortcut for command bar
	function handleGlobalKeydown(e: KeyboardEvent) {
		if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
			e.preventDefault();
			if (shouldShowCommandBar()) {
				commandBarOpen = !commandBarOpen;
			}
		}
	}

	// Get or create an "AI Tasks" goal for task creation
	async function getOrCreateAiTasksGoal(): Promise<string> {
		const goals = await getGoals();

		// Look for existing "AI Tasks" or "Quick Tasks" goal
		const aiGoal = goals.find(
			(g) => g.title === 'AI Tasks' || g.title === 'Quick Tasks' || g.title === 'Inbox'
		);

		if (aiGoal) {
			return aiGoal.id;
		}

		// Create a new "AI Tasks" goal
		const newGoal = await createGoal({
			title: 'AI Tasks',
			description: 'Tasks created via AI assistant'
		});

		return newGoal.id;
	}

	// Handle task creation from AI
	async function handleCreateTask(data: TaskPreviewData) {
		const goalId = await getOrCreateAiTasksGoal();
		await createTask(goalId, {
			title: data.title,
			description: data.description ?? undefined,
			dueDate: data.dueDate ?? undefined,
			identityId: data.identityId ?? undefined
		});
	}

	// Handle goal creation from AI
	async function handleCreateGoal(data: GoalPreviewData) {
		await createGoal({
			title: data.title,
			description: data.description ?? undefined,
			targetDate: data.targetDate ?? undefined
		});
	}

	// Handle habit stack creation from AI
	async function handleCreateHabitStack(data: HabitStackPreviewData) {
		await createHabitStack({
			name: data.name,
			description: data.description ?? undefined,
			triggerCue: data.triggerCue,
			identityId: data.identityId ?? undefined,
			items: data.habits.map((h) => ({
				cueDescription: h.cueDescription,
				habitDescription: h.habitDescription
			}))
		});
	}
</script>

<svelte:window onkeydown={handleGlobalKeydown} />

<svelte:head>
	<title>Help Motivate Me</title>
	<meta name="description" content="A modern task and goal management app to help you stay motivated" />
</svelte:head>

{#if shouldShowNav()}
	<TopNav />
{/if}

{@render children()}

<!-- Command Bar (Cmd+K / Ctrl+K) -->
{#if shouldShowCommandBar()}
	<CommandBar
		isOpen={commandBarOpen}
		onClose={() => (commandBarOpen = false)}
		onCreateTask={handleCreateTask}
		onCreateGoal={handleCreateGoal}
		onCreateHabitStack={handleCreateHabitStack}
	/>
{/if}
