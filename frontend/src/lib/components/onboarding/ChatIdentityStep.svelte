<script lang="ts">
	import ChatOnboarding from './ChatOnboarding.svelte';
	import { createIdentity } from '$lib/api/identities';
	import type { ExtractedData } from '$lib/api/ai';

	interface Props {
		onnext: () => void;
		onskip: () => void;
	}

	let { onnext, onskip }: Props = $props();

	let createdCount = $state(0);

	const initialMessage = `Hi! I'm here to help you define your identity - who you want to become.

Identity-based habits are powerful because instead of focusing on what you want to achieve, you focus on who you want to be. For example:
• "I am a healthy person" instead of "I want to lose weight"
• "I am a writer" instead of "I want to write a book"
• "I am an early riser" instead of "I want to wake up early"

Every action you take is a vote for the type of person you want to become.

So tell me, who do you want to become? What kind of person do you aspire to be?`;

	async function handleExtractedData(data: ExtractedData) {
		if (data.action === 'create' && data.type === 'identity') {
			const identityData = data.data as Record<string, unknown>;
			await createIdentity({
				name: String(identityData.name || ''),
				description: String(identityData.description || ''),
				icon: String(identityData.icon || ''),
				color: String(identityData.color || '#6366f1')
			});
			createdCount++;
		}
	}
</script>

<div class="h-full flex flex-col">
	<div class="p-4 border-b bg-white">
		<div class="flex items-center gap-3">
			<div class="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
				<span class="text-xl">🎯</span>
			</div>
			<div>
				<h2 class="font-semibold text-gray-900">Define Your Identity</h2>
				<p class="text-sm text-gray-500">
					{#if createdCount > 0}
						{createdCount} {createdCount === 1 ? 'identity' : 'identities'} created
					{:else}
						Who do you want to become?
					{/if}
				</p>
			</div>
		</div>
	</div>

	<div class="flex-1 overflow-hidden">
		<ChatOnboarding
			step="identity"
			{initialMessage}
			onExtractedData={handleExtractedData}
			onSkip={onskip}
			onNext={onnext}
		/>
	</div>
</div>
