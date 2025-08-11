export interface Category {
  id: number;
  name: string;
}

export interface CategoryServiceCount {
  categoryId: number;
  categoryName: string;
  serviceCount: number;
}

export interface Service {
  id: number;
  name: string;
  description: string;
  price?: number;
  duration?: number;
  appointmentBufferTime?: number;
  category: Category;
  website?: string;
  afterCareInstructions?: string;
}

export interface CreateServiceRequest {
  name: string;
  description?: string;
  afterCareInstructions?: string;
  price?: number;
  duration?: number;
  appointmentBufferTime?: number;
  categoryId: number;
  website?: string;
}

export interface UpdateServiceRequest {
  name?: string;
  description?: string;
  afterCareInstructions?: string | null;
  price?: number;
  duration?: number;
  appointmentBufferTime?: number;
  categoryId?: number;
  website?: string;
}

export interface CreateCategoryRequest {
  name: string;
}

export interface UpdateCategoryRequest {
  name: string;
}

export interface CategoryResponse {
  success: boolean;
  data: Category;
  message?: string;
}

export interface DeleteCategoryResponse {
  success: boolean;
  message: string;
}
