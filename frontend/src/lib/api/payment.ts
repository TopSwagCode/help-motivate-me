import { apiGet, apiPost } from './client';

export interface CreateCheckoutRequest {
    tier: 'Plus' | 'Pro';
    billingInterval: 'monthly' | 'yearly';
}

export interface CheckoutSessionResponse {
    checkoutUrl: string;
    sessionId: string;
}

export interface SubscriptionStatus {
    hasActiveSubscription: boolean;
    tier: string | null;
    billingInterval: string | null;
    currentPeriodEnd: string | null;
    cancelAtPeriodEnd: boolean | null;
}

export async function createCheckout(data: CreateCheckoutRequest): Promise<CheckoutSessionResponse> {
    return apiPost<CheckoutSessionResponse>('/payment/checkout', data);
}

export async function getSubscriptionStatus(): Promise<SubscriptionStatus> {
    return apiGet<SubscriptionStatus>('/payment/subscription');
}

export async function cancelSubscription(): Promise<void> {
    await apiPost('/payment/cancel');
}
