export interface GalleryImage {
  id: number;
  src: string;
  alt: string;
  category: string;
  title?: string;
  description?: string;
  isActive: boolean;
  sortOrder: number;
  uploadedAt: string;
  updatedAt?: string;
}

export interface CreateGalleryImageRequest {
  src: string;
  alt: string;
  category: string;
  title?: string;
  description?: string;
  isActive?: boolean;
  sortOrder?: number;
}

export interface UpdateGalleryImageRequest {
  src?: string;
  alt?: string;
  category?: string;
  title?: string;
  description?: string;
  isActive?: boolean;
  sortOrder?: number;
}

export interface GalleryImageResponse {
  success: boolean;
  data: GalleryImage[];
  totalCount: number;
  categories: string[];
}

export interface GalleryCategory {
  id: string;
  name: string;
  count: number;
}

export interface UploadImageResponse {
  success: boolean;
  url: string;
  filename: string;
  message?: string;
}
