export type ProofIntensity = 'Easy' | 'Moderate' | 'Hard';

export interface IdentityProof {
	id: string;
	identityId: string;
	identityName: string;
	identityColor: string | null;
	identityIcon: string | null;
	proofDate: string;
	description: string | null;
	intensity: ProofIntensity;
	voteValue: number;
	createdAt: string;
}

export interface CreateIdentityProofRequest {
	identityId: string;
	description?: string | null;
	intensity?: ProofIntensity;
}
