<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { commandBar } from '$lib/stores/commandBar';
	import { t } from 'svelte-i18n';
	import TopNav from '$lib/components/layout/TopNav.svelte';
	import BetaBanner from '$lib/components/layout/BetaBanner.svelte';
	import CommandBar from '$lib/components/ai/CommandBar.svelte';
	import PWAReloadPrompt from '$lib/components/PWAReloadPrompt.svelte';
	import PushPermissionPrompt from '$lib/components/PushPermissionPrompt.svelte';
	import OfflineBanner from '$lib/components/OfflineBanner.svelte';
	import ConnectionErrorOverlay from '$lib/components/ConnectionErrorOverlay.svelte';
	import GuidedTour from '$lib/components/tour/GuidedTour.svelte';
	import IdentityProofModal from '$lib/components/today/IdentityProofModal.svelte';
	import { initI18n, setLocale, getLocaleFromLanguage } from '$lib/i18n';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { browser } from '$app/environment';
	import { createGoal, getGoals } from '$lib/api/goals';
	import { createTask } from '$lib/api/tasks';
	import { createHabitStack } from '$lib/api/habitStacks';
	import { createIdentityFromAi } from '$lib/api/aiGeneral';
	import { createIdentityProof } from '$lib/api/identityProofs';
	import type {
		TaskPreviewData,
		GoalPreviewData,
		HabitStackPreviewData,
		IdentityPreviewData,
		IdentityProofPreviewData
	} from '$lib/api/aiGeneral';

	let i18nReady = $state(false);
	let authChecked = $state(false);
	let showProofModal = $state(false);

	// Initialize i18n and auth on mount
	onMount(async () => {
		// Initialize i18n first
		await initI18n();
		i18nReady = true;
		
		// Check if user is logged in (validates cookie with backend)
		if (browser) {
			await auth.init();
			authChecked = true;
		}
	});

	let { children } = $props();

	// Sync locale with user's preferred language when auth state changes
	$effect(() => {
		if ($auth.user?.preferredLanguage && i18nReady) {
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

	// Global keyboard shortcut for command bar
	function handleGlobalKeydown(e: KeyboardEvent) {
		if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
			e.preventDefault();
			if (shouldShowNav()) {
				commandBar.toggle();
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
			targetDate: data.targetDate ?? undefined,
			identityId: data.identityId ?? undefined
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

	// Handle identity creation from AI
	async function handleCreateIdentity(data: IdentityPreviewData) {
		await createIdentityFromAi(data);
	}

	// Handle identity proof creation from AI
	async function handleCreateIdentityProof(data: IdentityProofPreviewData) {
		await createIdentityProof({
			identityId: data.identityId,
			description: data.description ?? undefined,
			intensity: data.intensity
		});
	}
</script>

<svelte:window onkeydown={handleGlobalKeydown} />

<svelte:head>
	<title>Help Motivate Me</title>
	<meta name="description" content="A modern task and goal management app to help you stay motivated" />
</svelte:head>

{#if i18nReady}
	<!-- Connection Error Overlay (shows when API is unreachable) -->
	<ConnectionErrorOverlay />

	<!-- PWA Update Prompt -->
	<PWAReloadPrompt />

	<!-- Push Notification Permission Prompt -->
	<PushPermissionPrompt />

	<!-- Offline Banner (for minor offline state, not full overlay) -->
	<OfflineBanner />

	<!-- Beta Banner (shown on all pages) -->
	<BetaBanner />

	{#if shouldShowNav()}
		<TopNav />
	{/if}

	{@render children()}

	<!-- Guided Tour (renders when tour is active) -->
	{#if shouldShowNav()}
		<GuidedTour />
	{/if}

	<!-- Command Bar (Cmd+K / Ctrl+K) -->
	{#if shouldShowNav()}
		<CommandBar
			isOpen={$commandBar}
			onClose={() => commandBar.close()}
			onCreateTask={handleCreateTask}
			onCreateGoal={handleCreateGoal}
			onCreateHabitStack={handleCreateHabitStack}
			onCreateIdentity={handleCreateIdentity}
			onCreateIdentityProof={handleCreateIdentityProof}
		/>

		<!-- Identity Proof Modal -->
		<IdentityProofModal
			isOpen={showProofModal}
			onClose={() => showProofModal = false}
			onProofCreated={() => showProofModal = false}
		/>

		<!-- Floating AI Assistant Button -->
		<button
			type="button"
			onclick={() => commandBar.open()}
			data-tour="ai-assistant"
			class="fixed bottom-4 right-4 sm:bottom-6 sm:right-6 w-12 h-12 sm:w-14 sm:h-14 
			       bg-gradient-to-r from-primary-600 to-primary-700 
			       text-white rounded-full shadow-lg hover:shadow-xl hover:scale-110 
			       transition-all duration-200 flex items-center justify-center z-40
			       group touch-manipulation"
			title="AI Assistant (⌘K / Ctrl+K)"
			aria-label="Open AI Assistant"
		>
			<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M13 10V3L4 14h7v7l9-11h-7z"
				/>
			</svg>
			<!-- Keyboard shortcut hint on hover - positioned to the left to avoid overlap -->
			<span class="hidden sm:block absolute top-1/2 -translate-y-1/2 right-full mr-3 bg-gray-900 text-white text-xs px-2 py-1 
			             rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
				Press ⌘K
			</span>
		</button>

		<!-- Floating Identity Proof Button -->
		<button
			type="button"
			onclick={() => showProofModal = true}
			class="fixed bottom-20 right-4 sm:bottom-24 sm:right-6 w-12 h-12 sm:w-14 sm:h-14
			       bg-gradient-to-r from-amber-500 to-amber-600
			       text-white rounded-full shadow-lg hover:shadow-xl hover:scale-110
			       transition-all duration-200 flex items-center justify-center z-40
			       group touch-manipulation"
			title={$t('identityProof.subtitle')}
			aria-label={$t('identityProof.logProofButton')}
		>
			<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
				/>
			</svg>
			<!-- Label hint on hover - positioned to the left to avoid overlap -->
			<span class="hidden sm:block absolute top-1/2 -translate-y-1/2 right-full mr-3 bg-gray-900 text-white text-xs px-2 py-1
			             rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
				{$t('identityProof.logProofButton')}
			</span>
		</button>
	{/if}
{:else}
	<!-- Loading state while i18n initializes -->
	<div class="min-h-screen bg-gray-50 flex items-center justify-center">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{/if}
