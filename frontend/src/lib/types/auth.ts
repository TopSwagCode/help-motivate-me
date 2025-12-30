export interface User {
	id: string;
	username: string;
	email: string;
	displayName: string | null;
	createdAt: string;
	linkedProviders: string[];
}

export interface LoginRequest {
	username: string;
	password: string;
}

export interface RegisterRequest {
	username: string;
	email: string;
	password: string;
	displayName?: string;
}
