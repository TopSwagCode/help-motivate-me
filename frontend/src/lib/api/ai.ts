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

/**
 * Get contextual information to send with AI requests.
 * This helps the AI understand temporal references like "next week", "tomorrow", etc.
 */
export function getAiContext(): Record<string, unknown> {
	const now = new Date();
	const locale = navigator.language || 'en-US';
	
	// Get timezone info
	const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
	
	// Format dates for clarity
	const dateFormatter = new Intl.DateTimeFormat(locale, {
		weekday: 'long',
		year: 'numeric',
		month: 'long',
		day: 'numeric'
	});
	
	// Get day of week (0 = Sunday, 6 = Saturday)
	const dayOfWeek = now.getDay();
	const daysUntilWeekend = dayOfWeek === 0 ? 6 : (6 - dayOfWeek);
	const daysUntilMonday = dayOfWeek === 0 ? 1 : (8 - dayOfWeek) % 7 || 7;
	
	// Calculate some useful dates
	const nextWeek = new Date(now);
	nextWeek.setDate(now.getDate() + 7);
	
	const nextMonth = new Date(now);
	nextMonth.setMonth(now.getMonth() + 1);
	
	const endOfYear = new Date(now.getFullYear(), 11, 31);
	
	return {
		currentDate: now.toISOString().split('T')[0], // YYYY-MM-DD
		currentDateFormatted: dateFormatter.format(now),
		currentYear: now.getFullYear(),
		currentMonth: now.getMonth() + 1, // 1-12
		currentDayOfMonth: now.getDate(),
		dayOfWeek: now.toLocaleDateString(locale, { weekday: 'long' }),
		timeZone,
		locale,
		// Helpful for relative date references
		daysUntilWeekend,
		daysUntilNextMonday: daysUntilMonday,
		nextWeekDate: nextWeek.toISOString().split('T')[0],
		nextMonthDate: nextMonth.toISOString().split('T')[0],
		endOfYearDate: endOfYear.toISOString().split('T')[0]
	};
}

export async function* streamOnboardingChat(
	messages: ChatMessage[],
	step: OnboardingStep,
	additionalContext?: Record<string, unknown>
): AsyncGenerator<ChatStreamChunk> {
	// Always include base AI context (current date, etc.) merged with any additional context
	const context = {
		...getAiContext(),
		...additionalContext
	};
	
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
					const rawData = rawExtracted.Data ?? rawExtracted.data ?? {};
					// Type can be at root level OR inside the data dictionary (backend puts it in data)
					const extractedType = rawExtracted.Type ?? rawExtracted.type ?? rawData.type ?? rawData.Type;
					
					extractedData = {
						action: rawExtracted.Action ?? rawExtracted.action ?? '',
						type: extractedType,
						data: rawData,
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
