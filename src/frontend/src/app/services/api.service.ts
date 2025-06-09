import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  ApiResponse, 
  AuthRequest, 
  AuthResponse, 
  User,
  Service,
  Category,
  CreateAppointmentRequest,
  Appointment,
  AppointmentHistoryResponse,
  AppointmentsByDateResponse,
  ContactRequest,
  ContactSubmissionsResponse,
  ContactSubmissionsParams,
  ErrorResponse
} from '../models/api-models';

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
    return this.http.delete(`${this.baseUrl}/debug/contacts/${submissionId}`)
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