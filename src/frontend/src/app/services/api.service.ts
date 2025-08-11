import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ApiResponse,
  AuthRequest,
  AuthResponse,
  User,
  CreateAppointmentRequest,
  Appointment,
  AppointmentHistoryResponse,
  AppointmentsByDateResponse,
  EarliestAppointmentDateResponse,
  ContactRequest,
  ContactSubmissionsResponse,
  ContactSubmissionsParams,
  ErrorResponse,
  ClientReviewFlag,
  UpdateReviewFlagRequest,
  BanClientRequest,
  ClientBanResponse
} from '../models/api-models';

import {
  Service,
  Category,
  CreateServiceRequest,
  UpdateServiceRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CategoryServiceCount
} from '../models/services.models';

import {
  GalleryImageResponse,
  GalleryImage,
  CreateGalleryImageRequest,
  UpdateGalleryImageRequest,
  UploadImageResponse,
  GalleryCategory
} from '../models/gallery.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Authentication methods
  login(credentials: AuthRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/auth/login`, credentials);
  }

  // User methods
  getCurrentUser(): Observable<ApiResponse<User>> {
    return this.http.get<ApiResponse<User>>(`${this.baseUrl}/users/me`);
  }

  // Services methods
  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(`${this.baseUrl}/services`)
      .pipe(catchError(this.handleError));
  }

  // Categories methods
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`)
      .pipe(catchError(this.handleError));
  }

  // Category Management methods for admin panel
  createCategory(categoryData: CreateCategoryRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/manage/categories`, categoryData)
      .pipe(catchError(this.handleError));
  }

  updateCategory(categoryId: number, categoryData: UpdateCategoryRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/manage/categories/${categoryId}`, categoryData)
      .pipe(catchError(this.handleError));
  }

  deleteCategory(categoryId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/manage/categories/${categoryId}`)
      .pipe(catchError(this.handleError));
  }

  getServiceCountByCategory(): Observable<CategoryServiceCount[]> {
    return this.http.get<CategoryServiceCount[]>(`${this.baseUrl}/categories/service-counts`)
      .pipe(catchError(this.handleError));
  }

  // Appointment methods
  createAppointment(appointmentData: CreateAppointmentRequest): Observable<Appointment> {
    return this.http.post<Appointment>(`${this.baseUrl}/appointments`, appointmentData)
      .pipe(catchError(this.handleError));
  }

  getAppointmentsByDate(date: string): Observable<AppointmentsByDateResponse> {
    return this.http.get<AppointmentsByDateResponse>(`${this.baseUrl}/appointments/date/${date}`)
      .pipe(catchError(this.handleError));
  }

  getAppointmentsByDateRange(startDate: string, endDate: string): Observable<AppointmentsByDateResponse> {
    const params = new HttpParams().set('endDate', endDate);
    return this.http.get<AppointmentsByDateResponse>(`${this.baseUrl}/appointments/date/${startDate}`, { params })
      .pipe(catchError(this.handleError));
  }

  getAppointmentHistory(email: string): Observable<AppointmentHistoryResponse> {
    const params = new HttpParams().set('email', email);
    return this.http.get<AppointmentHistoryResponse>(`${this.baseUrl}/appointments/history`, { params })
      .pipe(catchError(this.handleError));
  }

  cancelAppointment(appointmentId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/appointments/${appointmentId}`)
      .pipe(catchError(this.handleError));
  }

  getEarliestAppointmentDate(): Observable<EarliestAppointmentDateResponse> {
    return this.http.get<EarliestAppointmentDateResponse>(`${this.baseUrl}/appointments/earliest-date`)
      .pipe(catchError(this.handleError));
  }

  // Contact form method (if contact API exists)
  submitContactForm(contactData: ContactRequest): Observable<any> {
    // Note: This assumes a contact endpoint exists or will be created
    return this.http.post(`${this.baseUrl}/contact`, contactData)
      .pipe(catchError(this.handleError));
  }

  // Contact Submissions methods for admin panel
  getContactSubmissions(params?: ContactSubmissionsParams): Observable<ContactSubmissionsResponse> {
    let httpParams = new HttpParams();
    
    if (params?.page) {
      httpParams = httpParams.set('page', params.page.toString());
    }
    if (params?.pageSize) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }
    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params?.search) {
      httpParams = httpParams.set('search', params.search);
    }

    return this.http.get<ContactSubmissionsResponse>(`${this.baseUrl}/manage/contacts`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  updateContactSubmissionStatus(submissionId: string, status: 'unread' | 'read' | 'responded', adminNotes?: string): Observable<any> {
    const payload: any = { status };
    if (adminNotes) {
      payload.adminNotes = adminNotes;
    }
    
    return this.http.put(`${this.baseUrl}/manage/contacts/${submissionId}/status`, payload)
      .pipe(catchError(this.handleError));
  }

  updateContactSubmissionNotes(submissionId: string, notes: string): Observable<any> {
    return this.http.put(`${this.baseUrl}/manage/contacts/${submissionId}/notes`, { adminNotes: notes })
      .pipe(catchError(this.handleError));
  }

  deleteContactSubmission(submissionId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/manage/contacts/${submissionId}`)
      .pipe(catchError(this.handleError));
  }

  // Client Management methods for admin panel
  getClientReviewFlags(status?: string): Observable<ClientReviewFlag[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    
    return this.http.get<ClientReviewFlag[]>(`${this.baseUrl}/client-reviews`, { params })
      .pipe(catchError(this.handleError));
  }

  getClientReviewFlag(flagId: number): Observable<ClientReviewFlag> {
    return this.http.get<ClientReviewFlag>(`${this.baseUrl}/client-reviews/${flagId}`)
      .pipe(catchError(this.handleError));
  }

  updateClientReviewFlag(flagId: number, updateData: UpdateReviewFlagRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/client-reviews/${flagId}`, updateData)
      .pipe(catchError(this.handleError));
  }

  banClient(clientId: number, banData: BanClientRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/clients/${clientId}/ban`, banData)
      .pipe(catchError(this.handleError));
  }

  unbanClient(clientId: number): Observable<ClientBanResponse> {
    return this.http.delete<ClientBanResponse>(`${this.baseUrl}/clients/${clientId}/ban`)
      .pipe(catchError(this.handleError));
  }

  getClientPendingReviews(clientId: number): Observable<ClientReviewFlag[]> {
    return this.http.get<ClientReviewFlag[]>(`${this.baseUrl}/clients/${clientId}/reviews`)
      .pipe(catchError(this.handleError));
  }

  // Service Management methods for admin panel
  createService(serviceData: CreateServiceRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/manage/services`, serviceData)
      .pipe(catchError(this.handleError));
  }

  updateService(serviceId: number, serviceData: UpdateServiceRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/manage/services/${serviceId}`, serviceData)
      .pipe(catchError(this.handleError));
  }

  deleteService(serviceId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/manage/services/${serviceId}`)
      .pipe(catchError(this.handleError));
  }

  // Gallery Management methods for admin panel
  getGalleryImages(category?: string): Observable<GalleryImageResponse> {
    let params = new HttpParams();
    if (category) {
      params = params.set('category', category);
    }
    
    return this.http.get<GalleryImageResponse>(`${this.baseUrl}/manage/gallery`, { params })
      .pipe(catchError(this.handleError));
  }

  getGalleryImage(imageId: number): Observable<GalleryImage> {
    return this.http.get<GalleryImage>(`${this.baseUrl}/manage/gallery/${imageId}`)
      .pipe(catchError(this.handleError));
  }

  createGalleryImage(imageData: CreateGalleryImageRequest): Observable<GalleryImage> {
    return this.http.post<GalleryImage>(`${this.baseUrl}/manage/gallery`, imageData)
      .pipe(catchError(this.handleError));
  }

  createGalleryImageWithFile(formData: FormData): Observable<GalleryImage> {
    return this.http.post<GalleryImage>(`${this.baseUrl}/manage/gallery`, formData)
      .pipe(catchError(this.handleError));
  }

  updateGalleryImage(imageId: number, imageData: UpdateGalleryImageRequest): Observable<GalleryImage> {
    return this.http.put<GalleryImage>(`${this.baseUrl}/manage/gallery/${imageId}`, imageData)
      .pipe(catchError(this.handleError));
  }

  deleteGalleryImage(imageId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/manage/gallery/${imageId}`)
      .pipe(catchError(this.handleError));
  }

  uploadGalleryImage(file: File): Observable<UploadImageResponse> {
    const formData = new FormData();
    formData.append('image', file);
    
    return this.http.post<UploadImageResponse>(`${this.baseUrl}/upload-image`, formData)
      .pipe(catchError(this.handleError));
  }

  // Public Gallery methods (for non-admin users)
  getPublicGalleryImages(category?: string): Observable<any> {
    // For now, use the same endpoint as admin (could be different in the future)
    return this.getGalleryImages(category);
  }

  updateGalleryImageOrder(imageIds: number[]): Observable<any> {
    return this.http.put(`${this.baseUrl}/manage/gallery/reorder`, { imageIds })
      .pipe(catchError(this.handleError));
  }

  // Error handling
  private handleError(error: any): Observable<never> {
    let errorMessage = 'An unknown error occurred!';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = error.error?.message || `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    
    console.error('API Error:', error);
    return throwError(() => new Error(errorMessage));
  }
}