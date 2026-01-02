<script lang="ts">
	let openSection: string | null = null;

	function toggleSection(section: string) {
		openSection = openSection === section ? null : section;
	}

	const faqSections = [
		{
			id: 'what-is',
			title: 'What is Help Motivate Me?',
			content: `Help Motivate Me is a comprehensive productivity and motivation platform designed to help you build better habits, achieve your goals, and become the person you want to be. The app combines proven behavioral science principles with practical tools to make lasting change easier.`
		},
		{
			id: 'identities',
			title: 'What are Identities and how do they work?',
			content: `Identities are the core of lasting behavior change. Instead of just setting goals, you define who you want to become - "I am a writer," "I am a healthy person," or "I am an organized professional."

Every time you complete a task or habit aligned with an identity, you cast a "vote" for that identity. These votes accumulate as evidence of your new identity. You'll see reinforcement messages like "That's what a writer does!" or "You're becoming a healthy person!" 

This approach works because behavior change is easier when it aligns with how you see yourself. Rather than forcing yourself to write because you "should," you write because that's what writers do - and you're a writer.`
		},
		{
			id: 'identity-votes',
			title: 'What are Identity Votes?',
			content: `Identity votes are a tracking system that shows you're becoming the person you want to be. Each completed task or habit linked to an identity counts as one vote for that identity.

For example, if you've defined "I am a healthy person" as an identity, then:
- Going for a morning run = 1 vote for "healthy person"
- Preparing a nutritious meal = 1 vote for "healthy person"
- Getting 8 hours of sleep = 1 vote for "healthy person"

The app tracks your total votes, recent activity, and completion rates. These visible metrics serve as proof of your identity shift, making it psychologically easier to continue the behavior. You're not just someone trying to be healthy - you're someone who has proven it 50 times this month.`
		},
		{
			id: 'habit-stacks',
			title: 'What is Habit Stacking?',
			content: `Habit Stacking is a powerful technique for building new habits by linking them to existing routines. The formula is simple: "After [CURRENT HABIT], I will [NEW HABIT]."

For example:
- After I pour my morning coffee, I will meditate for 2 minutes
- After I close my laptop for lunch, I will do 10 push-ups
- After I brush my teeth at night, I will lay out tomorrow's workout clothes

The app helps you create and track these stacks. You can define a series of habits that flow naturally from one to another, leveraging the momentum of existing routines to make new habits stick.

You can also link habit stacks to identities. A "Writer" identity might have a morning stack: After I make coffee → I will write 250 words → Then I will save my work. Each completed stack reinforces your identity.`
		},
		{
			id: 'goals',
			title: 'How do Goals work?',
			content: `Goals help you define what you want to achieve and track your progress. Unlike traditional goal-setting apps that focus solely on outcomes, this app emphasizes the process and identity behind your goals.

You can create goals with:
- Clear descriptions of what you want to achieve
- Target dates and milestones
- Links to identities (e.g., "Complete a marathon" linked to "I am an athlete")
- Associated tasks that break the goal into actionable steps

The app tracks your progress and helps you see how daily actions (tasks) connect to larger objectives (goals) and ultimately to who you're becoming (identities). This three-layer approach - identity, goals, tasks - ensures your daily efforts align with your long-term vision.`
		},
		{
			id: 'tasks',
			title: 'What are Tasks and how do they differ from habits?',
			content: `Tasks are specific, actionable items you need to complete. They can be one-time actions or recurring activities that contribute to your goals and identities.

Key features:
- Set due dates and priorities
- Link tasks to goals and identities
- Mark as complete to earn identity votes
- Postpone tasks when needed
- Track completion history

While habit stacks are routines you repeat regularly in sequence, tasks are individual items that need your attention. A task might be "Review quarterly budget" (one-time) or "Call Mom" (recurring weekly). Both types of activities can contribute to your identities and goals.`
		},
		{
			id: 'today',
			title: 'What is the Today view?',
			content: `The Today view is your daily dashboard - your mission control for the day. It shows everything that needs your attention right now:

- Tasks due today or overdue
- Habit stacks scheduled for today
- Progress on active goals
- Quick access to log journal entries
- Identity vote progress

This focused view helps you start each day with clarity, knowing exactly what actions will move you forward. Instead of being overwhelmed by everything on your plate, you see only what matters today.`
		},
		{
			id: 'journal',
			title: 'How does the Journal work?',
			content: `The Journal feature provides a space for reflection, tracking your thoughts, wins, challenges, and insights about your progress.

You can:
- Write daily entries about your experiences
- Attach images or files to capture moments
- Reflect on what's working and what isn't
- Track patterns in your motivation and energy
- Document your journey of identity change

Regular journaling helps you maintain awareness of your progress, process challenges, and celebrate wins. It's a powerful tool for self-reflection and course correction. Looking back at journal entries from weeks or months ago can show you just how far you've come.`
		},
		{
			id: 'analytics',
			title: 'What Analytics are available?',
			content: `The Analytics section gives you data-driven insights into your progress:

- Task completion rates over time
- Identity vote trends and streaks
- Goal progress visualization
- Habit stack adherence rates
- Best performing times and patterns

These metrics help you understand what's working, identify obstacles, and make informed adjustments to your approach. Data removes guesswork - you can see exactly which habits are sticking and which identities are gaining momentum.`
		},
		{
			id: 'login-social',
			title: 'How do I log in with Google or GitHub?',
			content: `We support social login to make access quick and secure:

**Google Login:**
Click "Continue with Google" on the login page. You'll be redirected to Google to authorize access. Once approved, you'll be automatically logged in. No password needed!

**GitHub Login:**
Click "Continue with GitHub" on the login page. Authorize the app through GitHub, and you're in.

**Benefits:**
- No password to remember
- Secure authentication through trusted providers
- Quick login process
- You can link multiple providers to the same account

If you sign up with one provider and later want to add another, you can link additional login methods in your account settings.`
		},
		{
			id: 'login-email',
			title: 'How does Email/Token login work?',
			content: `We offer passwordless email authentication for a secure, hassle-free experience:

**How it works:**
1. Enter your email address on the login page
2. Click "Send Login Link"
3. Check your email for a secure login link
4. Click the link to instantly log in

**Benefits:**
- No password to create, remember, or reset
- More secure than traditional passwords
- Works from any device with email access
- Automatic account creation for new users

The login link is single-use and expires after a short time for security. If you don't receive the email, check your spam folder or request a new link.

**Note:** You can also set a traditional password in your account settings if you prefer that login method.`
		},
		{
			id: 'multiple-logins',
			title: 'Can I use multiple login methods?',
			content: `Yes! You can link multiple authentication methods to your account for flexibility:

- Start with Google, add GitHub later
- Use email login from your phone, Google from your laptop
- Link a traditional password as a backup

**To add a login method:**
1. Log in with your primary method
2. Go to Settings
3. Look for "Connected Accounts" or "Login Methods"
4. Click "Link" next to the provider you want to add

**To remove a method:**
You can unlink login methods in Settings, but the app will prevent you from removing your last login method to avoid getting locked out.

This flexibility ensures you can always access your account, even if one provider has issues or you switch devices.`
		},
		{
			id: 'getting-started',
			title: 'How do I get started?',
			content: `Here's the recommended path to get the most out of the app:

**Step 1: Define Your Identities (5 minutes)**
Think about who you want to become. Pick 2-3 identities to start, like "I am a healthy person" or "I am a focused professional."

**Step 2: Create a Simple Habit Stack (5 minutes)**
Link a new habit to something you already do. Start small - "After I wake up, I will drink a glass of water."

**Step 3: Add a Few Tasks (5 minutes)**
What needs to get done today? Add 3-5 tasks and link them to your identities when relevant.

**Step 4: Take Action**
Complete tasks, work through your habit stack, and watch your identity votes accumulate.

**Step 5: Reflect in the Journal (2 minutes)**
End your day with a quick journal entry about what went well.

The key is to start simple. You don't need everything perfect on day one. Add goals and more complex habit stacks as you get comfortable with the basics.`
		},
		{
			id: 'privacy',
			title: 'Is my data private and secure?',
			content: `Yes. Your data privacy and security are top priorities:

- All data is encrypted in transit (HTTPS)
- Passwords are hashed and never stored in plain text
- Social logins use OAuth 2.0 industry standards
- Your data is never sold or shared with third parties
- Regular automated backups protect against data loss

You own your data. You can export it or delete your account at any time from the settings page.`
		},
		{
			id: 'support',
			title: 'How do I get help or report issues?',
			content: `We're here to help:

- Check this FAQ for common questions
- Review the in-app help tooltips and guides
- Contact support through the Settings page
- Report bugs or request features via GitHub (if available)

We continuously improve the app based on user feedback, so don't hesitate to share your thoughts or suggestions!`
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
	<div class="bg-white shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-3xl font-bold text-gray-900">Frequently Asked Questions</h1>
					<p class="mt-2 text-gray-600">
						Everything you need to know about Help Motivate Me
					</p>
				</div>
				<a
					href="/auth/login"
					class="hidden sm:inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
				>
					Get Started
				</a>
			</div>
		</div>
	</div>

	<!-- FAQ Content -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="space-y-4">
			{#each faqSections as section}
				<div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
					<button
						on:click={() => toggleSection(section.id)}
						class="w-full px-6 py-4 text-left flex items-center justify-between hover:bg-gray-50 transition-colors"
					>
						<h2 class="text-lg font-semibold text-gray-900">
							{section.title}
						</h2>
						<svg
							class="w-5 h-5 text-gray-500 transform transition-transform {openSection ===
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
						<div class="px-6 py-4 border-t border-gray-100 bg-gray-50">
							<div class="prose prose-sm max-w-none text-gray-700">
								{#each section.content.split('\n\n') as paragraph}
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
			<h2 class="text-2xl font-bold text-white mb-4">Ready to Start Your Journey?</h2>
			<p class="text-indigo-100 mb-6 max-w-2xl mx-auto">
				Join thousands of users building better habits and becoming the person they want to be.
				Sign up free and start making progress today.
			</p>
			<div class="flex flex-col sm:flex-row gap-4 justify-center">
				<a
					href="/auth/register"
					class="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-lg text-indigo-600 bg-white hover:bg-gray-50 transition-colors"
				>
					Create Free Account
				</a>
				<a
					href="/auth/login"
					class="inline-flex items-center justify-center px-6 py-3 border-2 border-white text-base font-medium rounded-lg text-white hover:bg-white hover:text-indigo-600 transition-colors"
				>
					Sign In
				</a>
			</div>
		</div>

		<!-- Footer Note -->
		<div class="mt-8 text-center text-sm text-gray-500">
			<p>
				Still have questions? <a href="/settings" class="text-indigo-600 hover:text-indigo-700"
					>Contact Support</a
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
