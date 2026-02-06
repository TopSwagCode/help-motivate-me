import { apiGet, apiPost, apiPut, apiDelete } from './client';
import type { Identity, IdentityStats, CreateIdentityRequest, UpdateIdentityRequest } from '$lib/types';

export async function getIdentities(): Promise<Identity[]> {
	return apiGet<Identity[]>('/identities');
}

export async function getIdentity(id: string): Promise<Identity> {
	return apiGet<Identity>(`/identities/${id}`);
}

export async function createIdentity(data: CreateIdentityRequest): Promise<Identity> {
	return apiPost<Identity>('/identities', data);
}

export async function updateIdentity(id: string, data: UpdateIdentityRequest): Promise<Identity> {
	return apiPut<Identity>(`/identities/${id}`, data);
}

export async function deleteIdentity(id: string): Promise<void> {
	return apiDelete<void>(`/identities/${id}`);
}

export async function getIdentityStats(id: string): Promise<IdentityStats> {
	return apiGet<IdentityStats>(`/identities/${id}/stats`);
}
