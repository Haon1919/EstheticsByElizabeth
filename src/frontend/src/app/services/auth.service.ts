import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface AdminSession {
  isAuthenticated: boolean;
  adminName?: string;
  loginTime?: Date;
  token?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly SESSION_KEY = 'adminSession';
  private readonly TOKEN_KEY = 'adminToken';
  
  private sessionSubject = new BehaviorSubject<AdminSession>({ isAuthenticated: false });
  public session$ = this.sessionSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.loadSession();
  }

  login(password: string): Observable<boolean> {
    return this.apiService.login({ password }).pipe(
      map(response => {
        const token = response.data?.token;
        if (response.success && token) {
          const session: AdminSession = {
            isAuthenticated: true,
            adminName: 'Administrator',
            loginTime: new Date(),
            token
          };
          this.sessionSubject.next(session);
          this.saveSession(session);
          return true;
        }
        return false;
      }),
      catchError(error => {
        console.error('Admin login failed:', error);
        return of(false);
      })
    );
  }

  logout(): void {
    const session: AdminSession = { isAuthenticated: false };
    this.sessionSubject.next(session);
    this.clearSession();
  }

  isAuthenticated(): boolean {
    return this.sessionSubject.value.isAuthenticated;
  }

  getSession(): AdminSession {
    return this.sessionSubject.value;
  }

  private saveSession(session: AdminSession): void {
    localStorage.setItem(this.SESSION_KEY, JSON.stringify(session));
    if (session.token) {
      localStorage.setItem(this.TOKEN_KEY, session.token);
    }
  }

  private loadSession(): void {
    try {
      const sessionData = localStorage.getItem(this.SESSION_KEY);
      const token = localStorage.getItem(this.TOKEN_KEY);
      if (sessionData && token) {
        const session: AdminSession = JSON.parse(sessionData);
        session.token = token;
        if (session.loginTime && new Date().getTime() - new Date(session.loginTime).getTime() < 24 * 60 * 60 * 1000) {
          this.sessionSubject.next(session);
        } else {
          this.clearSession();
        }
      }
    } catch (error) {
      console.error('Failed to load admin session:', error);
      this.clearSession();
    }
  }

  private clearSession(): void {
    localStorage.removeItem(this.SESSION_KEY);
    localStorage.removeItem(this.TOKEN_KEY);
  }
}
