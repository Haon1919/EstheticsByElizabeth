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
  startDate: string;
  endDate: string;
  isDateRange: boolean;
  appointments: Appointment[];
  totalCount: number;
}

// Contact form interface
export interface ContactRequest {
  name: string;
  email: string;
  phone?: string; // Optional phone number
  subject: string;
  message: string;
  interestedService?: string; // Optional
  preferredContactMethod?: string;
}

// Error response interface
export interface ErrorResponse {
  message: string;
  statusCode?: number;
}

// Contact Submissions interfaces for admin panel
export interface ContactSubmission {
  id: string;
  name: string;
  email: string;
  phone?: string;
  subject: string;
  message: string;
  interestedService?: string;
  preferredContactMethod?: string;
  submittedAt: string;
  status: 'unread' | 'read' | 'responded';
  readAt?: string;
  respondedAt?: string;
  adminNotes?: string;
}

export interface ContactSubmissionsResponse {
  success: boolean;
  data: ContactSubmission[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
  filters: {
    status?: string;
    search?: string;
  };
}

export interface ContactSubmissionsParams {
  page?: number;
  pageSize?: number;
  status?: 'unread' | 'read' | 'responded';
  search?: string;
}