import type { MembershipTier, UserRole } from './auth';

export interface AdminStats {
	totalUsers: number;
	activeUsers: number;
	usersLoggedInToday: number;
	membershipStats: MembershipStats;
	taskTotals: TaskTotals;
}

export interface MembershipStats {
	freeUsers: number;
	plusUsers: number;
	proUsers: number;
}

export interface TaskTotals {
	totalTasksCreated: number;
	totalTasksCompleted: number;
}

export interface DailyStats {
	date: string;
	tasksCreated: number;
	tasksCompleted: number;
	tasksDue: number;
}

export interface AdminUser {
	id: string;
	username: string;
	email: string;
	displayName: string | null;
	isActive: boolean;
	membershipTier: MembershipTier;
	role: UserRole;
	createdAt: string;
	updatedAt: string;
	aiCallsCount: number;
	aiTotalCostUsd: number;
}

export interface UpdateRoleRequest {
	role: UserRole;
}
