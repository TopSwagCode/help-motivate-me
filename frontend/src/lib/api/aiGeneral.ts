const API_BASE = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';

// Types for AI Intent Response
export interface AiIntentResponse {
	intent: string; // "create_task", "create_goal", "create_habit_stack", "create_identity", "clarify", "confirmed"
	confidence: number; // 0.0 - 1.0
	preview: AiPreview | null;
	clarifyingQuestion: string | null;
	actions: string[]; // ["confirm", "edit", "cancel"]
	createNow?: boolean;
}

export interface AiPreview {
	type: 'task' | 'goal' | 'habitStack' | 'identity';
	data: TaskPreviewData | GoalPreviewData | HabitStackPreviewData | IdentityPreviewData;
}

export interface TaskPreviewData {
	title: string;
	description?: string | null;
	dueDate?: string | null;
	identityId?: string | null;
	identityName?: string | null;
	reasoning?: string | null;
}

export interface GoalPreviewData {
	title: string;
	description?: string | null;
	targetDate?: string | null;
	identityId?: string | null;
	identityName?: string | null;
	reasoning?: string | null;
}

export interface HabitStackPreviewData {
	name: string;
	description?: string | null;
	triggerCue: string;
	identityId?: string | null;
	identityName?: string | null;
	reasoning?: string | null;
	habits: Array<{ cueDescription: string; habitDescription: string }>;
}

export interface IdentityPreviewData {
	name: string;
	description?: string | null;
	icon?: string | null;
	color?: string | null;
	reasoning?: string | null;
}

export interface GeneralChatChunk {
	content: string;
	isComplete: boolean;
	intentData?: AiIntentResponse | null;
}

export interface ChatMessage {
	role: 'user' | 'assistant';
	content: string;
}

export interface AiContext {
	identities: Array<{ id: string; name: string; icon?: string | null; color?: string | null }>;
	goals: Array<{ id: string; title: string }>;
}

/**
 * Get contextual information to send with AI requests.
 * This helps the AI understand temporal references like "next week", "tomorrow", etc.
 */
export function getAiContextInfo(): Record<string, unknown> {
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
	const daysUntilWeekend = dayOfWeek === 0 ? 6 : 6 - dayOfWeek;
	const daysUntilMonday = dayOfWeek === 0 ? 1 : ((8 - dayOfWeek) % 7) || 7;

	// Calculate some useful dates
	const tomorrow = new Date(now);
	tomorrow.setDate(now.getDate() + 1);

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
		tomorrowDate: tomorrow.toISOString().split('T')[0],
		daysUntilWeekend,
		daysUntilNextMonday: daysUntilMonday,
		nextWeekDate: nextWeek.toISOString().split('T')[0],
		nextMonthDate: nextMonth.toISOString().split('T')[0],
		endOfYearDate: endOfYear.toISOString().split('T')[0]
	};
}

/**
 * Get user's AI context (identities and goals) for enriching AI interactions.
 */
export async function getAiContext(): Promise<AiContext> {
	const response = await fetch(`${API_BASE}/api/ai/context`, {
		method: 'GET',
		credentials: 'include',
		headers: { 'X-CSRF': '1' }
	});

	if (!response.ok) {
		throw new Error(`Failed to get AI context: ${response.status}`);
	}

	return response.json();
}

/**
 * Parse intent data from raw API response, handling both PascalCase and camelCase.
 */
function parseIntentData(raw: Record<string, unknown> | null | undefined): AiIntentResponse | null {
	if (!raw) return null;

	const intent = (raw.Intent ?? raw.intent) as string;
	const confidence = (raw.Confidence ?? raw.confidence) as number;
	const clarifyingQuestion = (raw.ClarifyingQuestion ?? raw.clarifyingQuestion) as string | null;
	const actions = (raw.Actions ?? raw.actions ?? []) as string[];
	const createNow = (raw.CreateNow ?? raw.createNow) as boolean | undefined;

	const rawPreview = (raw.Preview ?? raw.preview) as Record<string, unknown> | null;
	let preview: AiPreview | null = null;

	if (rawPreview) {
		const previewType = (rawPreview.Type ?? rawPreview.type) as 'task' | 'goal' | 'habitStack' | 'identity';
		const previewData = (rawPreview.Data ?? rawPreview.data) as Record<string, unknown>;

		preview = {
			type: previewType,
			data: previewData as unknown as TaskPreviewData | GoalPreviewData | HabitStackPreviewData | IdentityPreviewData
		};
	}

	return {
		intent,
		confidence,
		preview,
		clarifyingQuestion,
		actions,
		createNow
	};
}

/**
 * Extract intent data from full AI response content by finding JSON blocks.
 */
function extractIntentFromContent(content: string): AiIntentResponse | null {
	// Look for JSON block in markdown code fence
	const jsonMatch = content.match(/```json\s*([\s\S]*?)\s*```/);
	if (!jsonMatch) return null;

	try {
		const parsed = JSON.parse(jsonMatch[1]);
		return parseIntentData(parsed);
	} catch {
		return null;
	}
}

/**
 * Stream AI chat for general task/goal/habit creation.
 * Returns streaming chunks with intent classification and confidence scores.
 */
export async function* streamGeneralChat(
	messages: ChatMessage[],
	additionalContext?: Record<string, unknown>
): AsyncGenerator<GeneralChatChunk> {
	const context = {
		...getAiContextInfo(),
		...additionalContext
	};

	const response = await fetch(`${API_BASE}/api/ai/general/chat`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: JSON.stringify({ messages, context })
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
	let fullContent = '';

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
			if (data === '[DONE]') {
				// Final extraction attempt from full content
				const intentData = extractIntentFromContent(fullContent);
				if (intentData) {
					yield {
						content: '',
						isComplete: true,
						intentData
					};
				}
				return;
			}

			try {
				const raw = JSON.parse(data);
				const content = (raw.Content ?? raw.content ?? '') as string;
				const isComplete = (raw.IsComplete ?? raw.isComplete ?? false) as boolean;

				fullContent += content;

				// Try to parse extracted data from the chunk
				const rawExtracted = raw.ExtractedData ?? raw.extractedData;
				let intentData: AiIntentResponse | null = null;

				if (rawExtracted) {
					intentData = parseIntentData(rawExtracted);
				}

				// If no extracted data in chunk but content contains JSON, try to extract
				if (!intentData && fullContent.includes('```json')) {
					intentData = extractIntentFromContent(fullContent);
				}

				const chunk: GeneralChatChunk = {
					content,
					isComplete,
					intentData
				};
				yield chunk;
			} catch {
				// Skip invalid JSON
			}
		}
	}
}

/**
 * Strip JSON code blocks from content for display purposes.
 */
export function stripJsonBlocks(content: string): string {
	return content.replace(/```json[\s\S]*?```/g, '').trim();
}

/**
 * Create identity from AI recommendation.
 */
export async function createIdentityFromAi(data: IdentityPreviewData): Promise<Identity> {
	const response = await fetch(`${API_BASE}/api/ai/general/create-identity`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: JSON.stringify({
			name: data.name,
			description: data.description,
			icon: data.icon,
			color: data.color
		})
	});

	if (!response.ok) {
		throw new Error(`Failed to create identity: ${response.status}`);
	}

	return response.json();
}

// Import Identity type (this should already be imported elsewhere in your app)
export interface Identity {
	id: string;
	name: string;
	description: string | null;
	color: string | null;
	icon: string | null;
	totalTasks?: number;
	completedTasks?: number;
	tasksCompletedLast7Days?: number;
	completionRate?: number;
	createdAt: string;
}
