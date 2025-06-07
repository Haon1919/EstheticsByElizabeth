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

// Appointment related interfaces
export interface Client {
  id?: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

export interface Category {
  id: number;
  name: string;
}

export interface Service {
  id: number;
  name: string;
  description: string;
  price?: number;
  duration?: number;
  category: Category;
}

export interface CreateAppointmentRequest {
  serviceId: number;
  time: string; // ISO string format
  client: Client;
}

export interface Appointment {
  id: number;
  time: string;
  service: Service;
  client: Client;
}

export interface AppointmentHistoryResponse {
  client: Client;
  appointments: Appointment[];
  totalCount: number;
}

export interface AppointmentsByDateResponse {
  date: string;
  appointments: Appointment[];
  totalCount: number;
}

// Contact form interface
export interface ContactRequest {
  name: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
}

// Error response interface
export interface ErrorResponse {
  message: string;
  statusCode?: number;
}