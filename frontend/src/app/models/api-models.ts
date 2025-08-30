import { Service } from './services.models';

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

export interface AdminLoginRequest {
  password: string;
}

export interface AdminLoginResponse {
  token: string;
}

// Appointment related interfaces
export interface Client {
  id?: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
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
  client: Client; // Both APIs now return client data with each appointment
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

export interface EarliestAppointmentDateResponse {
  earliestDate: string | null;
  hasAppointments: boolean;
  message: string;
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
  status: 'unread' | 'read' | 'responded' | 'banned';
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

// Client Review and Ban Management interfaces
export interface ClientReviewFlag {
  id: number;
  clientId: number;
  appointmentId: number;
  flagReason: string;
  flagDate: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Banned';
  reviewDate?: string;
  reviewedBy?: string;
  adminComments?: string;
  autoFlags: number;
  client: Client;
  appointment: {
    id: number;
    time: string;
    serviceId: number;
  };
}

export interface UpdateReviewFlagRequest {
  status: 'Pending' | 'Approved' | 'Rejected' | 'Banned';
  reviewedBy?: string;
  adminComments?: string;
}

export interface BanClientRequest {
  isBanned: boolean;
  reason?: string;
  adminName?: string;
  comments?: string;
}

export interface ClientManagementResponse {
  success: boolean;
  data: ClientReviewFlag[];
  totalCount: number;
  filters: {
    status?: string;
    clientId?: string;
  };
}

export interface ClientBanResponse {
  success: boolean;
  message: string;
  unbannedFlags?: number;
}
