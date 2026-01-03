const API_BASE = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';

export class ApiError extends Error {
	constructor(
		public status: number,
		message: string
	) {
		super(message);
		this.name = 'ApiError';
	}
}

async function handleResponse<T>(response: Response): Promise<T> {
	if (!response.ok) {
		if (response.status === 401) {
			// Redirect to login on unauthorized
			window.location.href = '/auth/login';
		}

		const errorText = await response.text();
		let errorMessage = `HTTP ${response.status}`;
		try {
			const errorJson = JSON.parse(errorText);
			errorMessage = errorJson.message || errorJson.title || errorMessage;
		} catch {
			errorMessage = errorText || errorMessage;
		}
		throw new ApiError(response.status, errorMessage);
	}

	if (response.status === 204) {
		return undefined as T;
	}

	return response.json();
}

export async function apiGet<T>(endpoint: string): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'GET',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		}
	});
	return handleResponse<T>(response);
}

export async function apiPost<T>(endpoint: string, data?: unknown): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: data ? JSON.stringify(data) : undefined
	});
	return handleResponse<T>(response);
}

export async function apiPut<T>(endpoint: string, data: unknown): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'PUT',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: JSON.stringify(data)
	});
	return handleResponse<T>(response);
}

export async function apiPatch<T>(endpoint: string, data?: unknown): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'PATCH',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: data ? JSON.stringify(data) : undefined
	});
	return handleResponse<T>(response);
}

export async function apiDelete<T>(endpoint: string): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'DELETE',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		}
	});
	return handleResponse<T>(response);
}

export function getOAuthUrl(provider: string): string {
	return `${API_BASE}/api/auth/external/${provider}`;
}
