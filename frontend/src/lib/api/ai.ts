const API_BASE = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';

export interface ChatMessage {
	role: 'user' | 'assistant';
	content: string;
}

export interface ExtractedData {
	action: string;
	type?: string; // The entity type: identity, habitStack, goal
	data: Record<string, unknown>;
	suggestedActions: string[];
}

export interface ChatStreamChunk {
	content: string;
	isComplete: boolean;
	extractedData?: ExtractedData | null;
}

export interface TranscriptionResponse {
	text: string;
	durationSeconds: number;
}

export type OnboardingStep = 'identity' | 'habitStack' | 'goal';

export async function* streamOnboardingChat(
	messages: ChatMessage[],
	step: OnboardingStep,
	context?: Record<string, unknown>
): AsyncGenerator<ChatStreamChunk> {
	const response = await fetch(`${API_BASE}/api/ai/onboarding/chat`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: JSON.stringify({
			messages,
			step,
			context
		})
	});

	if (!response.ok) {
		throw new Error(`Chat request failed: ${response.status}`);
	}

	if (!response.body) {
		throw new Error('No response body');
	}

	const reader = response.body.getReader();
	const decoder = new TextDecoder();
	let buffer = '';

	while (true) {
		const { done, value } = await reader.read();
		if (done) break;

		buffer += decoder.decode(value, { stream: true });

		// Process complete lines from the buffer
		const lines = buffer.split('\n');
		buffer = lines.pop() || ''; // Keep incomplete line in buffer

		for (const line of lines) {
			if (!line.startsWith('data: ')) continue;

			const data = line.slice(6); // Remove 'data: ' prefix
			if (data === '[DONE]') return;

			try {
				const raw = JSON.parse(data);
				// Handle PascalCase from C# API
				const rawExtracted = raw.ExtractedData ?? raw.extractedData;
				let extractedData: ExtractedData | null = null;

				if (rawExtracted) {
					extractedData = {
						action: rawExtracted.Action ?? rawExtracted.action ?? '',
						type: rawExtracted.Type ?? rawExtracted.type,
						data: rawExtracted.Data ?? rawExtracted.data ?? {},
						suggestedActions: rawExtracted.SuggestedActions ?? rawExtracted.suggestedActions ?? []
					};
				}

				const chunk: ChatStreamChunk = {
					content: raw.Content ?? raw.content ?? '',
					isComplete: raw.IsComplete ?? raw.isComplete ?? false,
					extractedData
				};
				yield chunk;
			} catch {
				// Skip invalid JSON
			}
		}
	}
}

export async function transcribeAudio(audioBlob: Blob): Promise<TranscriptionResponse> {
	const formData = new FormData();
	formData.append('file', audioBlob, 'recording.webm');

	const response = await fetch(`${API_BASE}/api/ai/onboarding/transcribe`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'X-CSRF': '1'
		},
		body: formData
	});

	if (!response.ok) {
		throw new Error(`Transcription failed: ${response.status}`);
	}

	return response.json();
}
