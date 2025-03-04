import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, AuthRequest, AuthResponse, User } from '../models/api-models';

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

  // Add more API methods as needed
}