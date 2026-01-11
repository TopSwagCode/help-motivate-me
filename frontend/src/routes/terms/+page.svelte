<script lang="ts">
	let openSection: string | null = $state('acceptance');

	function toggleSection(section: string) {
		openSection = openSection === section ? null : section;
	}

	const sections = [
		{
			id: 'acceptance',
			title: '1. Acceptance of Terms',
			content: `By accessing or using Help Motivate Me ("the Service"), you agree to be bound by these Terms of Service ("Terms"). If you do not agree to these Terms, please do not use the Service.

These Terms apply to all users, whether you have a free account or a paid subscription. We may update these Terms from time to time, and your continued use of the Service after changes constitutes acceptance of the updated Terms.`
		},
		{
			id: 'service-description',
			title: '2. Service Description',
			content: `Help Motivate Me is a productivity and personal development platform that helps you:

- Set and track goals
- Build and maintain habits through habit stacking
- Define and reinforce personal identities
- Journal your progress and reflections
- Analyze your productivity patterns

**AI-Assisted Features**
The Service includes AI-powered features to help with suggestions, onboarding, and productivity insights. AI assistance uses only the text content you explicitly provide and does not access your personal information like email, age, or address.`
		},
		{
			id: 'user-accounts',
			title: '3. User Accounts',
			content: `**Registration**
To use the Service, you must create an account using a valid email address or through a supported authentication provider (Google, GitHub).

**Account Security**
You are responsible for:
- Maintaining the confidentiality of your account credentials
- All activities that occur under your account
- Notifying us immediately of any unauthorized access

**One Account Per Person**
Each individual should maintain only one account. Creating multiple accounts to circumvent restrictions or abuse the Service is prohibited.

**Accurate Information**
You agree to provide accurate, current, and complete information during registration and to update this information as needed.`
		},
		{
			id: 'user-responsibilities',
			title: '4. User Responsibilities',
			content: `You agree to use the Service only for lawful purposes and in accordance with these Terms. You will not:

- Use the Service for any illegal or unauthorized purpose
- Attempt to gain unauthorized access to the Service or its systems
- Interfere with or disrupt the Service or servers
- Upload malicious code, viruses, or harmful content
- Impersonate any person or entity
- Harvest or collect user data without authorization
- Use the Service to send spam or unsolicited messages
- Violate any applicable laws or regulations

We reserve the right to terminate accounts that violate these responsibilities.`
		},
		{
			id: 'content',
			title: '5. Your Content',
			content: `**Ownership**
You retain ownership of all content you create within the Service, including goals, tasks, journal entries, and other personal data.

**License to Us**
By using the Service, you grant us a limited license to store, process, and display your content solely for the purpose of providing the Service to you.

**Content Responsibility**
You are solely responsible for the content you enter into the Service. This includes:
- Text in goals, tasks, and journal entries
- Any information shared with AI-assisted features
- Content shared through accountability buddy features

**Sensitive Information**
We recommend not entering highly sensitive personal information (financial data, health records, passwords, etc.) into text fields. While we secure your data, text input fields are under your control.`
		},
		{
			id: 'intellectual-property',
			title: '6. Intellectual Property',
			content: `**Our Property**
The Service, including its design, features, code, and branding, is owned by Help Motivate Me and protected by intellectual property laws. You may not copy, modify, distribute, or reverse engineer any part of the Service.

**Your Content**
You retain all intellectual property rights to the content you create. We claim no ownership over your goals, journal entries, or other personal content.

**Feedback**
If you provide feedback or suggestions about the Service, you grant us the right to use that feedback without obligation to you.`
		},
		{
			id: 'payment',
			title: '7. Payment Terms',
			content: `**Subscriptions**
The Service offers both free and paid subscription tiers. Paid subscriptions are billed on a recurring basis (monthly or annually) as specified at the time of purchase.

**Billing**
- Subscriptions automatically renew unless cancelled
- You will be charged at the beginning of each billing cycle
- Prices may change with notice to subscribers

**Refunds**
- We offer refunds on a case-by-case basis
- Contact us within 14 days of a charge to request a refund
- Refunds are not guaranteed for partial billing periods

**Cancellation**
You may cancel your subscription at any time through your account settings. Cancellation takes effect at the end of the current billing period.`
		},
		{
			id: 'service-availability',
			title: '8. Service Availability',
			content: `**Availability**
We strive to maintain high availability but do not guarantee uninterrupted access to the Service. The Service may be temporarily unavailable due to:
- Scheduled maintenance
- Emergency maintenance
- Technical issues beyond our control
- Force majeure events

**Modifications**
We may modify, suspend, or discontinue any part of the Service at any time. We will provide reasonable notice for significant changes that affect your use of the Service.

**No Guarantee**
The Service is provided on an "as is" and "as available" basis. We do not guarantee that the Service will meet your specific requirements or expectations.`
		},
		{
			id: 'liability',
			title: '9. Limitation of Liability',
			content: `**Disclaimer**
TO THE MAXIMUM EXTENT PERMITTED BY LAW, THE SERVICE IS PROVIDED "AS IS" WITHOUT WARRANTIES OF ANY KIND, WHETHER EXPRESS OR IMPLIED.

**Limitation**
We are not liable for:
- Indirect, incidental, special, or consequential damages
- Loss of profits, data, or business opportunities
- Damages resulting from your use or inability to use the Service
- Actions of third parties or other users

**Maximum Liability**
Our total liability for any claims related to the Service is limited to the amount you paid us in the 12 months preceding the claim, or $100, whichever is greater.

**Exceptions**
Some jurisdictions do not allow limitation of certain warranties or liabilities. In such cases, our liability is limited to the maximum extent permitted by law.`
		},
		{
			id: 'termination',
			title: '10. Account Termination',
			content: `**By You**
You may delete your account at any time through your account settings. Upon deletion:
- Your account and all associated data will be permanently removed
- This action is irreversible
- You will lose access to any paid features immediately

**By Us**
We may suspend or terminate your account if you:
- Violate these Terms
- Engage in fraudulent or harmful activities
- Fail to pay for subscription services
- Create risk or legal exposure for us

**Effect of Termination**
Upon termination, your right to use the Service ends immediately. We may retain certain data as required by law or for legitimate business purposes.`
		},
		{
			id: 'changes',
			title: '11. Changes to Terms',
			content: `We may modify these Terms at any time. When we make changes:

**Notification**
- Material changes will be communicated via email or prominent notice
- Minor clarifications may be made without notice

**Acceptance**
- Continued use of the Service after changes constitutes acceptance
- If you disagree with changes, you should stop using the Service

**Review**
We encourage you to periodically review these Terms to stay informed of any updates.`
		},
		{
			id: 'governing-law',
			title: '12. Governing Law',
			content: `These Terms are governed by and construed in accordance with applicable laws. Any disputes arising from these Terms or your use of the Service will be resolved through:

1. Good faith negotiation between the parties
2. Mediation if negotiation is unsuccessful
3. Binding arbitration or court proceedings as a last resort

By using the Service, you agree to resolve disputes through these methods before pursuing litigation.`
		},
		{
			id: 'contact',
			title: '13. Contact Information',
			content: `If you have questions about these Terms of Service, please contact us:

- Through our Contact page
- We typically respond within 24-48 hours

**Effective Date**
These Terms are effective as of ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}.

Thank you for using Help Motivate Me. We're committed to helping you achieve your goals while respecting your rights and privacy.`
		}
	];
</script>

<svelte:head>
	<title>Terms of Service - Help Motivate Me</title>
	<meta
		name="description"
		content="Terms of Service for Help Motivate Me - understand your rights and responsibilities when using our service"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-white shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<div class="flex items-center justify-between">
				<div>
					<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
						&larr; Back to Home
					</a>
					<h1 class="text-3xl font-bold text-gray-900">Terms of Service</h1>
					<p class="mt-2 text-gray-600">
						Your rights and responsibilities when using our service
					</p>
				</div>
			</div>
		</div>
	</div>

	<!-- Summary Box -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<div class="bg-blue-50 border border-blue-200 rounded-xl p-6">
			<div class="flex items-start gap-4">
				<div class="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
					<svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
					</svg>
				</div>
				<div>
					<h2 class="text-lg font-semibold text-blue-900">Plain Language Summary</h2>
					<p class="text-blue-800 mt-1">
						Use our service responsibly. You own your content. We protect your privacy. If something goes wrong, let's work it out fairly. Read the full terms below for details.
					</p>
				</div>
			</div>
		</div>
	</div>

	<!-- Content -->
	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
		<div class="space-y-4">
			{#each sections as section}
				<div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
					<button
						onclick={() => toggleSection(section.id)}
						class="w-full px-6 py-4 text-left flex items-center justify-between hover:bg-gray-50 transition-colors"
					>
						<h2 class="text-lg font-semibold text-gray-900">
							{section.title}
						</h2>
						<svg
							class="w-5 h-5 text-gray-500 transform transition-transform {openSection === section.id ? 'rotate-180' : ''}"
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

		<!-- Related Links -->
		<div class="mt-12 flex flex-col sm:flex-row gap-4 justify-center">
			<a href="/privacy" class="text-primary-600 hover:text-primary-700 font-medium text-center">
				View Privacy Policy &rarr;
			</a>
			<span class="hidden sm:inline text-gray-300">|</span>
			<a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium text-center">
				Contact Us &rarr;
			</a>
		</div>
	</div>
</div>

<style>
	.prose p {
		line-height: 1.7;
	}
</style>
