<script lang="ts">
	import { onDestroy } from 'svelte';
	import { t } from 'svelte-i18n';
	import { browser } from '$app/environment';
	import { dailyDigestStore } from '$lib/stores/dailyDigest';
	import type { DigestIdentity } from '$lib/types';

	let canvas: HTMLCanvasElement;
	let ctx: CanvasRenderingContext2D | null = null;
	let animationId: number = 0;
	let scoreParticles: ScoreParticle[] = [];
	let animationStarted = $state(false);
	let barDisplayScore: number[] = $state([]);
	let barRefs: HTMLDivElement[] = $state([]);
	let barAbsorbing: boolean[] = $state([]);
	let sequenceResolve: (() => void) | null = null;

	interface ScoreParticle {
		x: number;
		y: number;
		startX: number;
		startY: number;
		targetX: number;
		targetY: number;
		arcHeight: number;
		progress: number;
		speed: number;
		label: string;
		color: string;
		baseFontSize: number;
		fontSize: number;
		opacity: number;
		identityIndex: number;
		arrived: boolean;
		type: 'gain' | 'loss';
	}

	function spawnSingleGainParticle(identity: DigestIdentity, index: number, particleIndex: number): Promise<void> {
		return new Promise((resolve) => {
			if (!browser || !canvas || !ctx) { resolve(); return; }

			const barEl = barRefs[index];
			if (!barEl) { resolve(); return; }

			const barRect = barEl.getBoundingClientRect();
			const canvasRect = canvas.getBoundingClientRect();

			// Target: current leading edge of the bar (where it will be after this +1)
			const nextScore = barDisplayScore[index] + 1;
			const targetX = barRect.left - canvasRect.left + barRect.width * (Math.min(100, nextScore) / 100);
			const targetY = barRect.top - canvasRect.top + barRect.height / 2;

			// Start: below the identity card
			const startX = barRect.left - canvasRect.left + barRect.width * 0.3 + Math.random() * barRect.width * 0.4;
			const startY = barRect.bottom - canvasRect.top + 35;

			const particle: ScoreParticle = {
				x: startX,
				y: startY,
				startX,
				startY,
				targetX,
				targetY,
				arcHeight: 30 + Math.random() * 50,
				progress: 0,
				speed: 0.018 + Math.random() * 0.004,
				label: '+1',
				color: '#22c55e',
				baseFontSize: 13 + Math.random() * 3,
				fontSize: 13 + Math.random() * 3,
				opacity: 1,
				identityIndex: index,
				arrived: false,
				type: 'gain'
			};

			// When this particle arrives, bump the bar and resolve
			const originalResolve = resolve;
			sequenceResolve = () => {
				barDisplayScore[index] = nextScore;
				barAbsorbing[index] = true;
				setTimeout(() => { barAbsorbing[index] = false; }, 200);
				sequenceResolve = null;
				originalResolve();
			};

			scoreParticles.push(particle);
			startAnimationLoop();
		});
	}

	function spawnSingleLossParticle(identity: DigestIdentity, index: number, particleIndex: number): Promise<void> {
		return new Promise((resolve) => {
			if (!browser || !canvas || !ctx) { resolve(); return; }

			const barEl = barRefs[index];
			if (!barEl) { resolve(); return; }

			const barRect = barEl.getBoundingClientRect();
			const canvasRect = canvas.getBoundingClientRect();

			const currentScore = barDisplayScore[index];
			// Start: current right edge of the bar
			const startX = barRect.left - canvasRect.left + barRect.width * (Math.min(100, currentScore) / 100);
			const startY = barRect.top - canvasRect.top + barRect.height / 2;

			const targetX = startX + 25 + Math.random() * 35;
			const targetY = startY - 35 - Math.random() * 25;

			// Shrink bar immediately when loss particle spawns
			barDisplayScore[index] = currentScore - 1;

			const particle: ScoreParticle = {
				x: startX,
				y: startY,
				startX,
				startY,
				targetX,
				targetY,
				arcHeight: 8 + Math.random() * 12,
				progress: 0,
				speed: 0.025 + Math.random() * 0.008, // faster — short distance from bar
				label: '-1',
				color: '#ef4444',
				baseFontSize: 12 + Math.random() * 2,
				fontSize: 12 + Math.random() * 2,
				opacity: 1,
				identityIndex: index,
				arrived: false,
				type: 'loss'
			};

			// Resolve after a short delay so the next -1 doesn't overlap too much
			// Loss is faster since it starts at the bar, so shorter gap
			const originalResolve = resolve;
			sequenceResolve = () => {
				sequenceResolve = null;
				originalResolve();
			};

			scoreParticles.push(particle);
			startAnimationLoop();
		});
	}

	async function animateIdentity(identity: DigestIdentity, index: number) {
		const delta = identity.todayScore - identity.yesterdayScore;

		if (delta > 0) {
			const count = Math.min(delta, 8);
			for (let i = 0; i < count; i++) {
				await spawnSingleGainParticle(identity, index, i);
				// Small pause between particles so they don't stack
				await sleep(120);
			}
		} else if (delta < 0) {
			const count = Math.min(Math.abs(delta), 6);
			for (let i = 0; i < count; i++) {
				await spawnSingleLossParticle(identity, index, i);
				// Shorter pause for losses — they're quicker
				await sleep(80);
			}
		}
	}

	function sleep(ms: number): Promise<void> {
		return new Promise((resolve) => setTimeout(resolve, ms));
	}

	function quadraticBezier(start: number, control: number, end: number, t: number): number {
		const inv = 1 - t;
		return inv * inv * start + 2 * inv * t * control + t * t * end;
	}

	function startAnimationLoop() {
		if (animationId) return;
		animateLoop();
	}

	function animateLoop() {
		if (!ctx || !canvas) return;

		ctx.clearRect(0, 0, canvas.width, canvas.height);

		scoreParticles = scoreParticles.filter((p) => {
			p.progress += p.speed;

			const t = Math.min(p.progress, 1);

			if (p.type === 'gain') {
				const controlX = (p.startX + p.targetX) / 2;
				const controlY = p.startY - p.arcHeight;
				p.x = quadraticBezier(p.startX, controlX, p.targetX, t);
				p.y = quadraticBezier(p.startY, controlY, p.targetY, t);
			} else {
				const controlX = p.startX + (p.targetX - p.startX) * 0.3;
				const controlY = p.startY - p.arcHeight;
				p.x = quadraticBezier(p.startX, controlX, p.targetX, t);
				p.y = quadraticBezier(p.startY, controlY, p.targetY, t);
			}

			// Fade and shrink
			const fadeStart = p.type === 'gain' ? 0.65 : 0.4;
			if (t > fadeStart) {
				const fadeProg = (t - fadeStart) / (1 - fadeStart);
				p.opacity = 1 - fadeProg;
				p.fontSize = p.baseFontSize * (1 - fadeProg * 0.3);
			}

			if (t >= 1) {
				if (!p.arrived) {
					p.arrived = true;
					// Notify the sequence that this particle landed
					if (sequenceResolve) sequenceResolve();
				}
				return false;
			}

			// Draw text particle
			ctx!.save();
			ctx!.globalAlpha = p.opacity;
			ctx!.font = `bold ${Math.max(8, p.fontSize)}px system-ui, -apple-system, sans-serif`;
			ctx!.fillStyle = p.color;
			ctx!.strokeStyle = p.type === 'gain' ? 'rgba(0,0,0,0.3)' : 'rgba(0,0,0,0.25)';
			ctx!.lineWidth = 2.5;
			ctx!.textAlign = 'center';
			ctx!.textBaseline = 'middle';
			ctx!.strokeText(p.label, p.x, p.y);
			ctx!.fillText(p.label, p.x, p.y);
			ctx!.restore();

			return true;
		});

		if (scoreParticles.length > 0) {
			animationId = requestAnimationFrame(animateLoop);
		} else {
			animationId = 0;
		}
	}

	function getScoreDelta(identity: DigestIdentity): number {
		return identity.todayScore - identity.yesterdayScore;
	}

	function getBarWidth(score: number): string {
		return `${Math.max(0, Math.min(100, score))}%`;
	}

	function getBarBackground(identity: DigestIdentity, currentScore: number): string {
		const yesterday = Math.max(0, Math.min(100, identity.yesterdayScore));
		const color = identity.color || '#8b7355';

		if (currentScore <= yesterday || currentScore === 0) {
			return color;
		}

		const keptRatio = (yesterday / currentScore) * 100;
		return `linear-gradient(90deg, ${color} ${keptRatio}%, color-mix(in srgb, ${color}, #4ade80 40%) ${keptRatio}%, color-mix(in srgb, ${color}, #4ade80 40%) 100%)`;
	}

	let digestState = $derived($dailyDigestStore);
	let animationAborted = false;

	async function startSequentialAnimation(identities: DigestIdentity[]) {
		animationAborted = false;
		barDisplayScore = identities.map((i) => i.yesterdayScore);
		barAbsorbing = identities.map(() => false);

		if (!canvas) return;
		ctx = canvas.getContext('2d');
		if (!ctx) return;
		canvas.width = window.innerWidth;
		canvas.height = window.innerHeight;

		// Wait for cards to fade in
		await sleep(400);

		for (let i = 0; i < identities.length; i++) {
			if (animationAborted) return;
			// Wait for the card's fade-slide-in to finish (200ms per card stagger + 400ms animation)
			const cardDelay = Math.max(0, (i * 200 + 400) - 400);
			if (cardDelay > 0) await sleep(cardDelay);

			await animateIdentity(identities[i], i);

			// Pause between identities so each one is distinct
			if (i < identities.length - 1) {
				await sleep(300);
			}
		}
	}

	$effect(() => {
		if (digestState.visible && digestState.data && browser) {
			animationStarted = false;
			animationAborted = true;
			scoreParticles = [];
			sequenceResolve = null;
			if (animationId) {
				cancelAnimationFrame(animationId);
				animationId = 0;
			}

			setTimeout(() => {
				animationStarted = true;
				if (digestState.data) {
					startSequentialAnimation(digestState.data.identities);
				}
			}, 300);
		}
	});

	onDestroy(() => {
		animationAborted = true;
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
		<!-- Canvas for score particles (z-10 to render above modal, pointer-events-none for click-through) -->
		<canvas bind:this={canvas} class="absolute inset-0 z-10 pointer-events-none"></canvas>

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
							{@const displayScore = barDisplayScore[index] ?? identity.yesterdayScore}
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
								<div
									class="relative h-3 bg-gray-100 rounded-full overflow-hidden"
									bind:this={barRefs[index]}
								>
									<!-- Ghost bar (yesterday) -->
									<div
										class="absolute inset-y-0 left-0 rounded-full transition-none"
										style="width: {getBarWidth(identity.yesterdayScore)}; background-color: {identity.color || '#8b7355'}; opacity: 0.25;"
									></div>
									<!-- Animated bar (today) — driven by barDisplayScore -->
									<div
										class="absolute inset-y-0 left-0 rounded-full digest-bar {barAbsorbing[index] ? 'bar-absorbing' : ''}"
										style="
											width: {getBarWidth(displayScore)};
											background: {getBarBackground(identity, displayScore)};
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

	@keyframes bar-pulse {
		0% {
			transform: scaleY(1);
		}
		50% {
			transform: scaleY(1.4);
		}
		100% {
			transform: scaleY(1);
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
		transition: width 0.25s ease-out;
	}

	.bar-absorbing {
		animation: bar-pulse 0.2s ease-out;
		transform-origin: center;
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
