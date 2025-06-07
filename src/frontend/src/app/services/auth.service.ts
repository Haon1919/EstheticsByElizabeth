import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface AdminSession {
  isAuthenticated: boolean;
  adminName?: string;
  loginTime?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly ADMIN_PASSWORD = 'admin123'; // Hardcoded for development
  private readonly SESSION_KEY = 'adminSession';
  
  private sessionSubject = new BehaviorSubject<AdminSession>({ isAuthenticated: false });
  public session$ = this.sessionSubject.asObservable();

  constructor() {
    // Check if there's an existing session
    this.loadSession();
  }

  login(password: string): boolean {
    if (password === this.ADMIN_PASSWORD) {
      const session: AdminSession = {
        isAuthenticated: true,
        adminName: 'Administrator',
        loginTime: new Date()
      };
      
      this.sessionSubject.next(session);
      this.saveSession(session);
      return true;
    }
    return false;
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
  }

  private loadSession(): void {
    try {
      const sessionData = localStorage.getItem(this.SESSION_KEY);
      if (sessionData) {
        const session: AdminSession = JSON.parse(sessionData);
        // Check if session is less than 24 hours old
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
  }
}
