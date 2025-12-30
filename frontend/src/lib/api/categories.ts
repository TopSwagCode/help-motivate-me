import { apiGet, apiPost, apiPut, apiDelete } from './client';
import type { Category, CreateCategoryRequest } from '$lib/types';

export async function getCategories(): Promise<Category[]> {
	return apiGet<Category[]>('/categories');
}

export async function getCategory(id: string): Promise<Category> {
	return apiGet<Category>(`/categories/${id}`);
}

export async function createCategory(data: CreateCategoryRequest): Promise<Category> {
	return apiPost<Category>('/categories', data);
}

export async function updateCategory(id: string, data: CreateCategoryRequest): Promise<Category> {
	return apiPut<Category>(`/categories/${id}`, data);
}

export async function deleteCategory(id: string): Promise<void> {
	return apiDelete<void>(`/categories/${id}`);
}
