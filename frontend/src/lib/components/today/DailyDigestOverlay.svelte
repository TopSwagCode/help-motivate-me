<script lang="ts">
	import { onDestroy } from 'svelte';
	import { t } from 'svelte-i18n';
	import { browser } from '$app/environment';
	import { dailyDigestStore } from '$lib/stores/dailyDigest';
	import type { DigestIdentity } from '$lib/types';

	let canvas: HTMLCanvasElement;
	let ctx: CanvasRenderingContext2D | null = null;
	let animationId: number;
	let confettiPieces: ConfettiPiece[] = [];
	let animationStarted = $state(false);

	interface ConfettiPiece {
		x: number;
		y: number;
		vx: number;
		vy: number;
		color: string;
		rotation: number;
		rotationSpeed: number;
		size: number;
	}

	const COLORS = [
		'#f97316', '#eab308', '#22c55e', '#3b82f6',
		'#a855f7', '#ec4899', '#ef4444'
	];

	function initConfetti() {
		if (!browser || !canvas) return;
		ctx = canvas.getContext('2d');
		if (!ctx) return;

		canvas.width = window.innerWidth;
		canvas.height = window.innerHeight;

		for (let i = 0; i < 120; i++) {
			confettiPieces.push({
				x: canvas.width / 2,
				y: canvas.height / 2,
				vx: (Math.random() - 0.5) * 18,
				vy: Math.random() * -14 - 4,
				color: COLORS[Math.floor(Math.random() * COLORS.length)],
				rotation: Math.random() * 360,
				rotationSpeed: (Math.random() - 0.5) * 10,
				size: Math.random() * 8 + 4
			});
		}

		animateConfetti();
	}

	function animateConfetti() {
		if (!ctx || !canvas) return;

		ctx.clearRect(0, 0, canvas.width, canvas.height);

		confettiPieces = confettiPieces.filter((piece) => {
			piece.x += piece.vx;
			piece.y += piece.vy;
			piece.vy += 0.3;
			piece.vx *= 0.99;
			piece.rotation += piece.rotationSpeed;

			ctx!.save();
			ctx!.translate(piece.x, piece.y);
			ctx!.rotate((piece.rotation * Math.PI) / 180);
			ctx!.fillStyle = piece.color;
			ctx!.fillRect(-piece.size / 2, -piece.size / 2, piece.size, piece.size * 0.6);
			ctx!.restore();

			return piece.y <= canvas.height + 50;
		});

		if (confettiPieces.length > 0) {
			animationId = requestAnimationFrame(animateConfetti);
		}
	}

	function hasPositiveActivity(identities: DigestIdentity[]): boolean {
		return identities.some((i) => i.yesterdayVotes > 0);
	}

	function getScoreDelta(identity: DigestIdentity): number {
		return identity.todayScore - identity.yesterdayScore;
	}

	function getBarWidth(score: number): string {
		// Clamp score between 0 and 100 for display
		return `${Math.max(0, Math.min(100, score))}%`;
	}

	let digestState = $derived($dailyDigestStore);

	// Trigger animation when overlay becomes visible
	$effect(() => {
		if (digestState.visible && digestState.data && browser) {
			animationStarted = false;
			confettiPieces = [];
			if (animationId) cancelAnimationFrame(animationId);

			setTimeout(() => {
				animationStarted = true;
				if (digestState.data && hasPositiveActivity(digestState.data.identities)) {
					setTimeout(() => initConfetti(), 100);
				}
			}, 300);
		}
	});

	onDestroy(() => {
		if (browser && animationId) {
			cancelAnimationFrame(animationId);
		}
	});

	function handleDismiss() {
		dailyDigestStore.dismiss();
	}
</script>

{#if digestState.visible && digestState.data}
	<div class="fixed inset-0 z-[300] flex items-center justify-center bg-black/60 backdrop-blur-sm">
		<!-- Confetti canvas -->
		<canvas bind:this={canvas} class="absolute inset-0 pointer-events-none"></canvas>

		<!-- Modal -->
		<div class="relative w-full max-w-md mx-4 bg-warm-paper rounded-3xl shadow-2xl overflow-hidden animate-bounce-in">
			<!-- Close button -->
			<button
				onclick={handleDismiss}
				class="absolute top-4 right-4 p-2 text-white/70 hover:text-white transition-colors z-10"
				aria-label="Close"
			>
				<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
				</svg>
			</button>

			<!-- Header -->
			<div class="bg-gradient-to-br from-primary-400 via-primary-500 to-primary-600 pt-8 pb-6 px-6 text-center">
				<div class="w-16 h-16 mx-auto bg-warm-paper/20 rounded-full flex items-center justify-center mb-3">
					<svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
					</svg>
				</div>
				<h2 class="text-xl font-bold text-white">{$t('dailyDigest.title')}</h2>
				<p class="text-white/80 text-sm mt-1">{$t('dailyDigest.subtitle')}</p>
			</div>

			<!-- Identity list -->
			<div class="px-6 py-4 max-h-[50vh] overflow-y-auto">
				{#if digestState.data.totalYesterdayVotes === 0 && digestState.data.identities.every((i) => getScoreDelta(i) === 0)}
					<div class="text-center py-6">
						<p class="text-cocoa-600">{$t('dailyDigest.noActivity')}</p>
					</div>
				{:else}
					<div class="space-y-4">
						{#each digestState.data.identities as identity, index (identity.id)}
							{@const delta = getScoreDelta(identity)}
							<div
								class="digest-identity"
								style="animation-delay: {index * 200}ms"
							>
								<!-- Identity header -->
								<div class="flex items-center justify-between mb-1.5">
									<div class="flex items-center gap-2 min-w-0">
										{#if identity.icon}
											<span class="text-lg flex-shrink-0">{identity.icon}</span>
										{/if}
										<span class="font-medium text-cocoa-800 truncate text-sm">{identity.name}</span>
									</div>
									<span
										class="flex-shrink-0 text-xs font-semibold px-2 py-0.5 rounded-full {delta > 0
											? 'bg-green-100 text-green-700'
											: delta < 0
												? 'bg-red-100 text-red-700'
												: 'bg-gray-100 text-gray-600'}"
									>
										{delta > 0 ? `+${delta}` : delta === 0 ? '=' : delta}
									</span>
								</div>

								<!-- Progress bar -->
								<div class="relative h-3 bg-gray-100 rounded-full overflow-hidden">
									<!-- Ghost bar (yesterday) -->
									<div
										class="absolute inset-y-0 left-0 rounded-full transition-none"
										style="width: {getBarWidth(identity.yesterdayScore)}; background-color: {identity.color || '#8b7355'}; opacity: 0.25;"
									></div>
									<!-- Animated bar (today) -->
									<div
										class="absolute inset-y-0 left-0 rounded-full digest-bar"
										style="
											width: {animationStarted ? getBarWidth(identity.todayScore) : getBarWidth(identity.yesterdayScore)};
											background-color: {identity.color || '#8b7355'};
											transition-delay: {index * 200}ms;
										"
									>
										<div class="shimmer-overlay"></div>
									</div>
								</div>

								<!-- Vote breakdown -->
								{#if identity.yesterdayVotes > 0}
									<div class="flex flex-wrap gap-x-3 gap-y-0.5 mt-1 text-[11px] text-cocoa-500">
										{#if identity.habitVotes > 0}
											<span>{$t('dailyDigest.habits')}: {identity.habitVotes}</span>
										{/if}
										{#if identity.taskVotes > 0}
											<span>{$t('dailyDigest.tasks')}: {identity.taskVotes}</span>
										{/if}
										{#if identity.proofVotes > 0}
											<span>{$t('dailyDigest.proofs')}: {identity.proofVotes}</span>
										{/if}
										{#if identity.stackBonusVotes > 0}
											<span>{$t('dailyDigest.bonus')}: {identity.stackBonusVotes}</span>
										{/if}
									</div>
								{/if}
							</div>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="px-6 pb-6 pt-2">
				<button
					onclick={handleDismiss}
					class="w-full px-4 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors"
				>
					{$t('dailyDigest.letsGo')}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	@keyframes bounce-in {
		0% {
			opacity: 0;
			transform: scale(0.3);
		}
		50% {
			transform: scale(1.05);
		}
		70% {
			transform: scale(0.9);
		}
		100% {
			opacity: 1;
			transform: scale(1);
		}
	}

	@keyframes fade-slide-in {
		0% {
			opacity: 0;
			transform: translateY(12px);
		}
		100% {
			opacity: 1;
			transform: translateY(0);
		}
	}

	@keyframes shimmer {
		0% {
			transform: translateX(-100%);
		}
		100% {
			transform: translateX(200%);
		}
	}

	.animate-bounce-in {
		animation: bounce-in 0.5s ease-out;
	}

	.digest-identity {
		opacity: 0;
		animation: fade-slide-in 0.4s ease-out forwards;
	}

	.digest-bar {
		transition: width 1.2s cubic-bezier(0.34, 1.56, 0.64, 1);
	}

	.shimmer-overlay {
		position: absolute;
		inset: 0;
		background: linear-gradient(
			90deg,
			transparent 0%,
			rgba(255, 255, 255, 0.3) 50%,
			transparent 100%
		);
		animation: shimmer 2s ease-in-out infinite;
		animation-delay: 1s;
	}
</style>
