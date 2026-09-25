import { apiGet, apiPost, apiPatch } from './client';
import type { User, LoginRequest, Language } from '$lib/types';

export async function getCurrentUser(): Promise<User> {
	return apiGet<User>('/auth/me');
}

export async function updateLanguage(language: Language): Promise<User> {
	return apiPatch<User>('/auth/language', { language });
}

export async function login(data: LoginRequest): Promise<User> {
	return apiPost<User>('/auth/login', data);
}

export async function logout(): Promise<void> {
	return apiPost<void>('/auth/logout');
}
