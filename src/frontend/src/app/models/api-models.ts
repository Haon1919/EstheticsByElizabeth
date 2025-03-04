// Common models shared between frontend and backend

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface User {
  id: number;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
}

export interface AuthRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiresAt: string;
}