<script lang="ts">
	import { transcribeAudio } from '$lib/api/ai';

	interface Props {
		onTranscription: (text: string) => void;
		disabled?: boolean;
	}

	let { onTranscription, disabled = false }: Props = $props();

	let isRecording = $state(false);
	let isTranscribing = $state(false);
	let error = $state('');
	let audioLevel = $state(0);

	let mediaRecorder: MediaRecorder | null = null;
	let audioChunks: Blob[] = [];
	let audioContext: AudioContext | null = null;
	let analyser: AnalyserNode | null = null;
	let silenceTimer: ReturnType<typeof setTimeout> | null = null;
	let animationFrame: number | null = null;

	const SILENCE_THRESHOLD = 0.01;
	const SILENCE_DURATION = 1500; // 1.5 seconds of silence to stop

	async function startRecording() {
		error = '';
		audioChunks = [];

		try {
			const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

			// Set up audio analysis for silence detection
			audioContext = new AudioContext();
			analyser = audioContext.createAnalyser();
			const source = audioContext.createMediaStreamSource(stream);
			source.connect(analyser);
			analyser.fftSize = 256;

			// Start media recorder
			mediaRecorder = new MediaRecorder(stream, {
				mimeType: 'audio/webm;codecs=opus'
			});

			mediaRecorder.ondataavailable = (event) => {
				if (event.data.size > 0) {
					audioChunks.push(event.data);
				}
			};

			mediaRecorder.onstop = async () => {
				// Clean up
				stream.getTracks().forEach((track) => track.stop());
				if (animationFrame) cancelAnimationFrame(animationFrame);
				if (silenceTimer) clearTimeout(silenceTimer);

				if (audioChunks.length > 0) {
					await processRecording();
				}
			};

			mediaRecorder.start(100); // Collect data every 100ms
			isRecording = true;

			// Start monitoring audio levels
			monitorAudioLevel();
		} catch (err) {
			console.error('Failed to start recording:', err);
			error = 'Could not access microphone. Please check permissions.';
		}
	}

	function monitorAudioLevel() {
		if (!analyser) return;

		const dataArray = new Uint8Array(analyser.frequencyBinCount);

		function checkLevel() {
			if (!analyser || !isRecording) return;

			analyser.getByteFrequencyData(dataArray);

			// Calculate average volume level
			const average = dataArray.reduce((a, b) => a + b, 0) / dataArray.length;
			const normalizedLevel = average / 255;
			audioLevel = normalizedLevel;

			// Silence detection
			if (normalizedLevel < SILENCE_THRESHOLD) {
				if (!silenceTimer) {
					silenceTimer = setTimeout(() => {
						if (isRecording && audioChunks.length > 0) {
							stopRecording();
						}
					}, SILENCE_DURATION);
				}
			} else {
				if (silenceTimer) {
					clearTimeout(silenceTimer);
					silenceTimer = null;
				}
			}

			animationFrame = requestAnimationFrame(checkLevel);
		}

		checkLevel();
	}

	function stopRecording() {
		if (mediaRecorder && mediaRecorder.state !== 'inactive') {
			mediaRecorder.stop();
		}
		isRecording = false;
		audioLevel = 0;

		if (audioContext) {
			audioContext.close();
			audioContext = null;
		}
	}

	async function processRecording() {
		isTranscribing = true;
		error = '';

		try {
			const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
			const result = await transcribeAudio(audioBlob);
			if (result.text.trim()) {
				onTranscription(result.text.trim());
			}
		} catch (err) {
			console.error('Transcription failed:', err);
			error = 'Failed to transcribe audio. Please try again.';
		} finally {
			isTranscribing = false;
		}
	}

	function toggleRecording() {
		if (isRecording) {
			stopRecording();
		} else {
			startRecording();
		}
	}
</script>

<div class="relative">
	<button
		type="button"
		onclick={toggleRecording}
		disabled={disabled || isTranscribing}
		class="relative p-3 rounded-full transition-all duration-200 {isRecording
			? 'bg-red-500 text-white hover:bg-red-600'
			: 'bg-gray-100 text-gray-600 hover:bg-gray-200'} disabled:opacity-50 disabled:cursor-not-allowed"
		title={isRecording ? 'Stop recording' : 'Start voice input'}
	>
		{#if isTranscribing}
			<svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
				<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
				></circle>
				<path
					class="opacity-75"
					fill="currentColor"
					d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
				></path>
			</svg>
		{:else}
			<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z"
				/>
			</svg>
		{/if}

		<!-- Audio level indicator -->
		{#if isRecording}
			<span
				class="absolute inset-0 rounded-full bg-red-400 opacity-30 animate-ping"
				style="transform: scale({1 + audioLevel * 0.5})"
			></span>
		{/if}
	</button>

	{#if error}
		<div class="absolute top-full left-1/2 -translate-x-1/2 mt-2 whitespace-nowrap">
			<span class="text-xs text-red-600 bg-red-50 px-2 py-1 rounded">{error}</span>
		</div>
	{/if}
</div>
