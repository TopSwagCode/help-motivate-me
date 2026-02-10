<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { commandBar } from '$lib/stores/commandBar';
	import { milestoneStore } from '$lib/stores/milestones';
	import { t } from 'svelte-i18n';
	import TopNav from '$lib/components/layout/TopNav.svelte';
	import BottomNav from '$lib/components/layout/BottomNav.svelte';
	import BetaBanner from '$lib/components/layout/BetaBanner.svelte';
	import CommandBar from '$lib/components/ai/CommandBar.svelte';
	import PWAReloadPrompt from '$lib/components/PWAReloadPrompt.svelte';
	import PushPermissionPrompt from '$lib/components/PushPermissionPrompt.svelte';
	import OfflineBanner from '$lib/components/OfflineBanner.svelte';
	import ConnectionErrorOverlay from '$lib/components/ConnectionErrorOverlay.svelte';
	import GuidedTour from '$lib/components/tour/GuidedTour.svelte';
	import IdentityProofModal from '$lib/components/today/IdentityProofModal.svelte';
	import HelpPopup from '$lib/components/help/HelpPopup.svelte';
	import MilestoneCelebration from '$lib/components/milestones/MilestoneCelebration.svelte';
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
	let showHelpPopup = $state(false);

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

	// Check for unseen milestones when user is authenticated
	$effect(() => {
		if (authChecked && $auth.user && browser) {
			// Small delay to avoid blocking initial render
			setTimeout(() => {
				milestoneStore.checkForNew();
			}, 500);
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

	// Check if user is currently in onboarding (not yet completed)
	function isInOnboarding(): boolean {
		return $auth.user !== null && !$auth.user.hasCompletedOnboarding;
	}

	function shouldShowNav(): boolean {
		const path = $page.url.pathname;
		return $auth.user !== null && !publicRoutes.includes(path);
	}

	// Should show full nav elements (bottom nav, floating buttons, etc.)
	// Hidden during onboarding to keep focus on the onboarding flow
	function shouldShowFullNav(): boolean {
		return shouldShowNav() && !isInOnboarding();
	}

	// Global keyboard shortcut for command bar
	function handleGlobalKeydown(e: KeyboardEvent) {
		if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
			e.preventDefault();
			if (shouldShowFullNav()) {
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
			description: 'Tasks created via AI assistant',
			targetDate: null
		});

		return newGoal.id;
	}

	// Handle task creation from AI
	async function handleCreateTask(data: TaskPreviewData) {
		const goalId = await getOrCreateAiTasksGoal();
		await createTask(goalId, {
			title: data.title,
			description: data.description ?? null,
			dueDate: data.dueDate ?? null,
			identityId: data.identityId ?? null
		});
	}

	// Handle goal creation from AI
	async function handleCreateGoal(data: GoalPreviewData) {
		await createGoal({
			title: data.title,
			description: data.description ?? null,
			targetDate: data.targetDate ?? null,
			identityId: data.identityId ?? null
		});
	}

	// Handle habit stack creation from AI
	async function handleCreateHabitStack(data: HabitStackPreviewData) {
		await createHabitStack({
			name: data.name,
			description: data.description ?? null,
			triggerCue: data.triggerCue,
			identityId: data.identityId ?? null,
			items: data.habits.map((h) => ({
				cueDescription: h.cueDescription,
				habitDescription: h.habitDescription
			}))
		});
	}

	// Handle identity creation from AI
	async function handleCreateIdentity(data: IdentityPreviewData) {
		await createIdentityFromAi(data);
		// Check for milestones that may have been unlocked
		milestoneStore.checkForNew();
	}

	// Handle identity proof creation from AI
	async function handleCreateIdentityProof(data: IdentityProofPreviewData) {
		await createIdentityProof({
			identityId: data.identityId,
			description: data.description ?? null,
			intensity: data.intensity
		});
		// Check for milestones that may have been unlocked
		milestoneStore.checkForNew();
	}
</script>

<svelte:window onkeydown={handleGlobalKeydown} />

<svelte:head>
	<title>Help Motivate Me</title>
	<meta name="description" content="A modern task and goal management app to help you stay motivated" />
</svelte:head>

{#if i18nReady}
	<div class="flex flex-col min-h-screen">
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
			<TopNav hideUserMenu={isInOnboarding()} onHelpClick={() => showHelpPopup = true} />
		{/if}

		<div class="flex-1 {shouldShowFullNav() ? 'pb-28' : ''}">
			{@render children()}
		</div>
	</div>

	<!-- Mobile Bottom Navigation -->
	{#if shouldShowFullNav()}
		<BottomNav />
	{/if}

	<!-- Guided Tour (renders when tour is active) -->
	{#if shouldShowFullNav()}
		<GuidedTour />
	{/if}

	<!-- Command Bar (Cmd+K / Ctrl+K) -->
	{#if shouldShowFullNav()}
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
		<div class="fixed bottom-24 right-4 sm:right-6 z-30 animate-wiggle">
			<button
				type="button"
				onclick={() => commandBar.open()}
				data-tour="ai-assistant"
				class="group relative w-12 h-12 sm:w-14 sm:h-14
				       bg-gradient-to-r from-primary-500 to-primary-600
				       text-white rounded-full shadow-lg hover:shadow-xl hover:scale-110
				       transition-all duration-300 flex items-center justify-center
				       touch-manipulation"
				title="AI Assistant (⌘K / Ctrl+K)"
				aria-label="Open AI Assistant"
			>
				<span class="absolute right-full mr-3 px-3 py-1.5 bg-cocoa-900 text-white text-sm font-medium rounded-2xl whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none">
					Commands
				</span>
				<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M13 10V3L4 14h7v7l9-11h-7z"
					/>
				</svg>
			</button>
		</div>

		<!-- Floating Identity Proof Button -->
		<div class="fixed bottom-40 right-4 sm:right-6 z-30 animate-wiggle" style="animation-delay: 2.5s;">
			<button
				type="button"
				data-tour="log-win-button"
				onclick={() => showProofModal = true}
				class="group relative w-12 h-12 sm:w-14 sm:h-14
				       bg-gradient-to-r from-primary-400 to-primary-500
				       text-white rounded-full shadow-lg hover:shadow-xl hover:scale-110
				       transition-all duration-300 flex items-center justify-center
				       touch-manipulation"
				title={$t('identityProof.subtitle')}
				aria-label={$t('identityProof.logProofButton')}
			>
				<span class="absolute right-full mr-3 px-3 py-1.5 bg-cocoa-900 text-white text-sm font-medium rounded-2xl whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none">
					Log Win
				</span>
				<!-- Checkmark icon -->
				<svg class="w-6 h-6 sm:w-7 sm:h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2.5"
						d="M5 13l4 4L19 7"
					/>
				</svg>
			</button>
		</div>

		<!-- Help Popup -->
		<HelpPopup
			isOpen={showHelpPopup}
			onClose={() => showHelpPopup = false}
		/>

		<!-- Milestone Celebration Modal -->
		<MilestoneCelebration />
	{/if}
{:else}
	<!-- Loading state while i18n initializes -->
	<div class="min-h-screen bg-warm-cream flex items-center justify-center">
		<div class="animate-spin w-8 h-8 border-4 border-primary-500 border-t-transparent rounded-full"></div>
	</div>
{/if}
