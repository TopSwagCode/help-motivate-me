<script lang="ts">
	import { t } from 'svelte-i18n';

	let openSection: string | null = null;

	function toggleSection(section: string) {
		openSection = openSection === section ? null : section;
	}

	const faqSections = [
		{
			id: 'what-is',
			titleKey: 'faq.questions.whatIs.title',
			contentKey: 'faq.questions.whatIs.content'
		},
		{
			id: 'identities',
			titleKey: 'faq.questions.identities.title',
			contentKey: 'faq.questions.identities.content'
		},
		{
			id: 'identity-votes',
			titleKey: 'faq.questions.identityVotes.title',
			contentKey: 'faq.questions.identityVotes.content'
		},
		{
			id: 'habit-stacks',
			titleKey: 'faq.questions.habitStacks.title',
			contentKey: 'faq.questions.habitStacks.content'
		},
		{
			id: 'goals',
			titleKey: 'faq.questions.goals.title',
			contentKey: 'faq.questions.goals.content'
		},
		{
			id: 'tasks',
			titleKey: 'faq.questions.tasks.title',
			contentKey: 'faq.questions.tasks.content'
		},
		{
			id: 'today',
			titleKey: 'faq.questions.today.title',
			contentKey: 'faq.questions.today.content'
		},
		{
			id: 'journal',
			titleKey: 'faq.questions.journal.title',
			contentKey: 'faq.questions.journal.content'
		},
		{
			id: 'analytics',
			titleKey: 'faq.questions.analytics.title',
			contentKey: 'faq.questions.analytics.content'
		},
		{
			id: 'login-social',
			titleKey: 'faq.questions.loginSocial.title',
			contentKey: 'faq.questions.loginSocial.content'
		},
		{
			id: 'login-email',
			titleKey: 'faq.questions.loginEmail.title',
			contentKey: 'faq.questions.loginEmail.content'
		},
		{
			id: 'multiple-logins',
			titleKey: 'faq.questions.multipleLogins.title',
			contentKey: 'faq.questions.multipleLogins.content'
		},
		{
			id: 'getting-started',
			titleKey: 'faq.questions.gettingStarted.title',
			contentKey: 'faq.questions.gettingStarted.content'
		},
		{
			id: 'privacy',
			titleKey: 'faq.questions.privacy.title',
			contentKey: 'faq.questions.privacy.content'
		},
		{
			id: 'support',
			titleKey: 'faq.questions.support.title',
			contentKey: 'faq.questions.support.content'
		}
	];
</script>

<svelte:head>
	<title>FAQ - Help Motivate Me</title>
	<meta
		name="description"
		content="Frequently asked questions about Help Motivate Me - your productivity and motivation platform"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-warm-paper shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-3xl font-bold text-cocoa-800">{$t('faq.title')}</h1>
					<p class="mt-2 text-cocoa-600">
						{$t('faq.subtitle')}
					</p>
				</div>
				<a
					href="/auth/login"
					class="hidden sm:inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-2xl text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
				>
					{$t('faq.getStarted')}
				</a>
			</div>
		</div>
	</div>

	<!-- FAQ Content -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="space-y-4">
			{#each faqSections as section}
				<div class="bg-warm-paper rounded-2xl shadow-sm border border-primary-100 overflow-hidden">
					<button
						on:click={() => toggleSection(section.id)}
						class="w-full px-6 py-4 text-left flex items-center justify-between hover:bg-warm-cream transition-colors"
					>
						<h2 class="text-lg font-semibold text-cocoa-800">
							{$t(section.titleKey)}
						</h2>
						<svg
							class="w-5 h-5 text-cocoa-500 transform transition-transform {openSection ===
							section.id
								? 'rotate-180'
								: ''}"
							fill="none"
							stroke="currentColor"
							viewBox="0 0 24 24"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M19 9l-7 7-7-7"
							/>
						</svg>
					</button>

					{#if openSection === section.id}
						<div class="px-6 py-4 border-t border-gray-100 bg-warm-cream">
							<div class="prose prose-sm max-w-none text-cocoa-700">
								{#each $t(section.contentKey).split('\n\n') as paragraph}
									<p class="mb-4 last:mb-0 whitespace-pre-line">{paragraph}</p>
								{/each}
							</div>
						</div>
					{/if}
				</div>
			{/each}
		</div>

		<!-- Call to Action -->
		<div class="mt-12 bg-gradient-to-r from-indigo-600 to-purple-600 rounded-xl p-8 text-center">
			<h2 class="text-2xl font-bold text-white mb-4">{$t('faq.cta.title')}</h2>
			<p class="text-indigo-100 mb-6 max-w-2xl mx-auto">
				{$t('faq.cta.description')}
			</p>
			<div class="flex flex-col sm:flex-row gap-4 justify-center">
				<a
					href="/auth/register"
					class="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-2xl text-indigo-600 bg-warm-paper hover:bg-warm-cream transition-colors"
				>
					{$t('faq.cta.createAccount')}
				</a>
				<a
					href="/auth/login"
					class="inline-flex items-center justify-center px-6 py-3 border-2 border-white text-base font-medium rounded-2xl text-white hover:bg-warm-paper hover:text-indigo-600 transition-colors"
				>
					{$t('faq.cta.signIn')}
				</a>
			</div>
		</div>

		<!-- Footer Note -->
		<div class="mt-8 text-center text-sm text-cocoa-500">
			<p>
				{$t('faq.footer.stillHaveQuestions')} <a href="/settings" class="text-indigo-600 hover:text-indigo-700"
					>{$t('faq.footer.contactSupport')}</a
				>
			</p>
		</div>
	</div>
</div>

<style>
	.prose p {
		line-height: 1.7;
	}
</style>
