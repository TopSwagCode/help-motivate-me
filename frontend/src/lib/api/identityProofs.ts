import { apiGet, apiPost, apiDelete } from './client';
import type { IdentityProof, CreateIdentityProofRequest } from '$lib/types';

export async function getIdentityProofs(
	startDate?: string,
	endDate?: string
): Promise<IdentityProof[]> {
	const params = new URLSearchParams();
	if (startDate) params.append('startDate', startDate);
	if (endDate) params.append('endDate', endDate);

	const queryString = params.toString();
	return apiGet<IdentityProof[]>(`/identity-proofs${queryString ? `?${queryString}` : ''}`);
}

export async function createIdentityProof(
	request: CreateIdentityProofRequest
): Promise<IdentityProof> {
	return apiPost<IdentityProof>('/identity-proofs', request);
}

export async function deleteIdentityProof(id: string): Promise<void> {
	return apiDelete<void>(`/identity-proofs/${id}`);
}
