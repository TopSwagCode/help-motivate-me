<script lang="ts">
	let openSection: string | null = $state('at-a-glance');

	function toggleSection(section: string) {
		openSection = openSection === section ? null : section;
	}

	const sections = [
		{
			id: 'at-a-glance',
			title: 'Privacy at a Glance',
			content: `We believe privacy is a fundamental right, not a feature. Here's what makes us different:

- No tracking cookies - we don't track your behavior across the web
- No data selling - your information is never sold to third parties
- No advertising partners - we don't share your data with advertisers
- AI uses only text you provide - we never send your personal information (age, address, etc.) to AI services
- You control what you enter - you decide what information to share in text fields`
		},
		{
			id: 'what-we-collect',
			title: 'What We Collect',
			content: `We collect only what's necessary to provide our service:

**Account Information**
- Email address (for authentication and account recovery)
- Username and display name (chosen by you)
- Authentication tokens (for secure login via email, Google, or GitHub)

**Content You Create**
- Goals, tasks, and habit stacks you set up
- Journal entries you write
- Identity definitions you create
- Progress and completion data

**Technical Data**
- Session information (to keep you logged in)
- Basic error logs (to fix bugs and improve the service)

We do not collect browsing history, location data, device fingerprints, or any data for behavioral profiling.`
		},
		{
			id: 'what-we-dont-collect',
			title: 'What We Do NOT Collect',
			content: `We explicitly avoid collecting:

- Tracking cookies or third-party cookies
- Behavioral analytics data
- Cross-site tracking information
- Location or GPS data
- Device fingerprints
- Social media activity
- Contact lists or address books
- Biometric data
- Data from third-party advertising networks

We don't use Google Analytics, Facebook Pixel, or any similar tracking services.`
		},
		{
			id: 'ai-usage',
			title: 'How We Use AI',
			content: `Our app uses AI to help with suggestions, onboarding, and productivity features. Here's how we handle this responsibly:

**What We Send to AI**
- Only the text content you explicitly provide in input fields
- Context about your goals or habits when you request AI assistance

**What We Never Send to AI**
- Your email address
- Your age, location, or address
- Your real name (unless you type it in a text field)
- Any personal information from your profile

**Your Responsibility**
Text input fields are under your control. If you type sensitive personal information into a text field that gets processed by AI, that's your choice. We recommend keeping sensitive details out of AI-assisted features.

**AI Provider**
We use reputable AI providers with their own privacy commitments. No AI provider stores your prompts for training purposes through our integration.`
		},
		{
			id: 'data-security',
			title: 'Data Security',
			content: `We take security seriously:

**Encryption**
- All data transmitted over HTTPS (TLS encryption)
- Passwords are hashed using industry-standard algorithms
- Database connections are encrypted

**Authentication**
- Secure OAuth 2.0 for Google and GitHub login
- Passwordless email login option with time-limited tokens
- Session tokens with automatic expiration

**Access Control**
- Role-based access to admin features
- No shared passwords among team members
- Regular security reviews`
		},
		{
			id: 'your-rights',
			title: 'Your Rights',
			content: `You have full control over your data:

**Export Your Data**
- Download all your goals, tasks, habits, and journal entries
- Available in standard formats from your settings

**Delete Your Account**
- Permanently delete your account and all associated data
- This action is irreversible and removes everything

**Access Information**
- View all data we store about you
- Request clarification about how your data is used

**Correction**
- Update or correct any information in your profile
- Change your email, username, or other details anytime

To exercise any of these rights, visit your account settings or contact us.`
		},
		{
			id: 'third-parties',
			title: 'Third Parties',
			content: `We work with a minimal set of third parties:

**Authentication Providers**
- Google OAuth (if you choose to sign in with Google)
- GitHub OAuth (if you choose to sign in with GitHub)
These providers only verify your identity - we don't share your activity data with them.

**Infrastructure**
- Cloud hosting for our servers and database
- These providers process data only as necessary to provide infrastructure

**What We Don't Have**
- No advertising partners
- No data brokers
- No analytics companies
- No social media tracking
- No affiliate programs that track users

We never sell, rent, or trade your personal information.`
		},
		{
			id: 'cookies',
			title: 'Cookies',
			content: `We use only essential cookies:

**Session Cookie**
- Purpose: Keep you logged in
- Duration: Until you log out or session expires
- Type: First-party, essential

**That's It**
We don't use:
- Tracking cookies
- Analytics cookies
- Advertising cookies
- Third-party cookies
- Persistent behavior tracking

No cookie banner is needed because we don't use cookies that require consent beyond essential functionality.`
		},
		{
			id: 'contact',
			title: 'Contact Us',
			content: `Have questions about privacy? We're here to help.

**Email**: Contact us through our contact page
**Response Time**: We typically respond within 24-48 hours

We take privacy inquiries seriously and will address any concerns promptly.

**Updates to This Policy**
If we make significant changes to this privacy policy, we'll notify users via email or a prominent notice on our website. Minor clarifications may be made without notice.

Last updated: ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}`
		}
	];
</script>

<svelte:head>
	<title>Privacy Policy - Help Motivate Me</title>
	<meta
		name="description"
		content="Our privacy policy - we respect your privacy with no tracking cookies, no data selling, and transparent AI usage"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-warm-paper shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<div class="flex items-center justify-between">
				<div>
					<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
						&larr; Back to Home
					</a>
					<h1 class="text-3xl font-bold text-cocoa-800">Privacy Policy</h1>
					<p class="mt-2 text-cocoa-600">
						How we protect and respect your privacy
					</p>
				</div>
			</div>
		</div>
	</div>

	<!-- Highlight Box -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="bg-green-50 border border-green-200 rounded-xl p-6">
			<div class="flex items-start gap-4">
				<div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
					<svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
					</svg>
				</div>
				<div>
					<h2 class="text-lg font-semibold text-green-900">Privacy-First by Design</h2>
					<p class="text-green-800 mt-1">
						No tracking cookies. No data selling. No advertising partners. We built this app to respect your privacy from day one.
					</p>
				</div>
			</div>
		</div>
	</div>

	<!-- Content -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
		<div class="space-y-4">
			{#each sections as section}
				<div class="bg-warm-paper rounded-2xl shadow-sm border border-primary-100 overflow-hidden">
					<button
						onclick={() => toggleSection(section.id)}
						class="w-full px-6 py-4 text-left flex items-center justify-between hover:bg-warm-cream transition-colors"
					>
						<h2 class="text-lg font-semibold text-cocoa-800">
							{section.title}
						</h2>
						<svg
							class="w-5 h-5 text-cocoa-500 transform transition-transform {openSection === section.id ? 'rotate-180' : ''}"
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
								{#each section.content.split('\n\n') as paragraph}
									<p class="mb-4 last:mb-0 whitespace-pre-line">{paragraph}</p>
								{/each}
							</div>
						</div>
					{/if}
				</div>
			{/each}
		</div>

		<!-- Footer -->
		<div class="mt-12 text-center">
			<p class="text-sm text-cocoa-500">
				Questions about our privacy practices? <a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium">Contact us</a>
			</p>
		</div>
	</div>
</div>

<style>
	.prose p {
		line-height: 1.7;
	}
</style>
