<script lang="ts">
	let name = $state('');
	let email = $state('');
	let subject = $state('general');
	let message = $state('');
	let loading = $state(false);
	let success = $state(false);
	let error = $state('');

	const subjects = [
		{ value: 'general', label: 'General Inquiry' },
		{ value: 'support', label: 'Support Request' },
		{ value: 'feedback', label: 'Feedback' },
		{ value: 'bug', label: 'Bug Report' },
		{ value: 'partnership', label: 'Partnership' }
	];

	async function handleSubmit(e: Event) {
		e.preventDefault();

		if (!name.trim() || !email.trim() || !message.trim()) {
			error = 'Please fill in all required fields';
			return;
		}

		loading = true;
		error = '';

		// Simulate form submission - in production this would call an API
		try {
			await new Promise((resolve) => setTimeout(resolve, 1000));
			success = true;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to send message. Please try again.';
		} finally {
			loading = false;
		}
	}

	function resetForm() {
		name = '';
		email = '';
		subject = 'general';
		message = '';
		success = false;
		error = '';
	}
</script>

<svelte:head>
	<title>Contact - Help Motivate Me</title>
	<meta
		name="description"
		content="Get in touch with Help Motivate Me - we're here to help with questions, feedback, or support"
	/>
</svelte:head>

<div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
	<!-- Header -->
	<div class="bg-warm-paper shadow-sm border-b">
		<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
			<a href="/" class="text-primary-600 hover:text-primary-700 text-sm font-medium mb-2 inline-block">
				&larr; Back to Home
			</a>
			<h1 class="text-3xl font-bold text-cocoa-800">Contact Us</h1>
			<p class="mt-2 text-cocoa-600">
				We'd love to hear from you
			</p>
		</div>
	</div>

	<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
		<div class="grid grid-cols-1 md:grid-cols-3 gap-8">
			<!-- Contact Info -->
			<div class="md:col-span-1">
				<div class="bg-warm-paper rounded-xl shadow-sm border border-primary-100 p-6">
					<h2 class="text-lg font-semibold text-cocoa-800 mb-4">Get in Touch</h2>

					<div class="space-y-4">
						<div class="flex items-start gap-3">
							<div class="w-10 h-10 bg-primary-100 rounded-2xl flex items-center justify-center flex-shrink-0">
								<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
								</svg>
							</div>
							<div>
								<h3 class="font-medium text-cocoa-800">Email</h3>
								<p class="text-cocoa-600 text-sm">Fill out the form and we'll respond within 24-48 hours</p>
							</div>
						</div>

						<div class="flex items-start gap-3">
							<div class="w-10 h-10 bg-primary-100 rounded-2xl flex items-center justify-center flex-shrink-0">
								<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
								</svg>
							</div>
							<div>
								<h3 class="font-medium text-cocoa-800">FAQ</h3>
								<p class="text-cocoa-600 text-sm">
									Check our <a href="/faq" class="text-primary-600 hover:text-primary-700">FAQ page</a> for common questions
								</p>
							</div>
						</div>

						<div class="flex items-start gap-3">
							<div class="w-10 h-10 bg-primary-100 rounded-2xl flex items-center justify-center flex-shrink-0">
								<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
								</svg>
							</div>
							<div>
								<h3 class="font-medium text-cocoa-800">Response Time</h3>
								<p class="text-cocoa-600 text-sm">We typically respond within 24-48 hours</p>
							</div>
						</div>
					</div>
				</div>
			</div>

			<!-- Contact Form -->
			<div class="md:col-span-2">
				<div class="bg-warm-paper rounded-xl shadow-sm border border-primary-100 p-6">
					{#if success}
						<!-- Success State -->
						<div class="text-center py-8">
							<div class="w-16 h-16 mx-auto bg-green-100 rounded-full flex items-center justify-center mb-4">
								<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
								</svg>
							</div>
							<h2 class="text-2xl font-bold text-cocoa-800 mb-2">Message Sent!</h2>
							<p class="text-cocoa-600 mb-6">
								Thank you for reaching out. We'll get back to you within 24-48 hours.
							</p>
							<button
								onclick={resetForm}
								class="btn-secondary"
							>
								Send Another Message
							</button>
						</div>
					{:else}
						<!-- Form -->
						<h2 class="text-lg font-semibold text-cocoa-800 mb-6">Send us a message</h2>

						{#if error}
							<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-2xl text-sm mb-6">
								{error}
							</div>
						{/if}

						<form onsubmit={handleSubmit} class="space-y-6">
							<div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
								<div>
									<label for="name" class="label">Name <span class="text-red-500">*</span></label>
									<input
										type="text"
										id="name"
										bind:value={name}
										required
										class="input"
										placeholder="Your name"
									/>
								</div>

								<div>
									<label for="email" class="label">Email <span class="text-red-500">*</span></label>
									<input
										type="email"
										id="email"
										bind:value={email}
										required
										class="input"
										placeholder="your@email.com"
									/>
								</div>
							</div>

							<div>
								<label for="subject" class="label">Subject</label>
								<select
									id="subject"
									bind:value={subject}
									class="input"
								>
									{#each subjects as option}
										<option value={option.value}>{option.label}</option>
									{/each}
								</select>
							</div>

							<div>
								<label for="message" class="label">Message <span class="text-red-500">*</span></label>
								<textarea
									id="message"
									bind:value={message}
									required
									rows="5"
									class="input resize-none"
									placeholder="How can we help you?"
								></textarea>
							</div>

							<div class="flex justify-end">
								<button
									type="submit"
									disabled={loading}
									class="btn-primary px-8"
								>
									{loading ? 'Sending...' : 'Send Message'}
								</button>
							</div>
						</form>
					{/if}
				</div>
			</div>
		</div>

		<!-- Privacy Note -->
		<div class="mt-8 text-center">
			<p class="text-sm text-cocoa-500">
				We respect your privacy. Your information will only be used to respond to your inquiry.
				See our <a href="/privacy" class="text-primary-600 hover:text-primary-700">Privacy Policy</a>.
			</p>
		</div>
	</div>
</div>
