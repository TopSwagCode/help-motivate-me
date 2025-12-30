export interface Category {
	id: string;
	name: string;
	color: string | null;
	icon: string | null;
}

export interface CreateCategoryRequest {
	name: string;
	color?: string;
	icon?: string;
}
