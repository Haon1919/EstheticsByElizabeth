import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  todayAppointments: any[] = [];
  unreadMessagesCount = 0;
  pendingFlagsCount = 0;
  isLoading = true;
  sendingReminders = false;
  remindersMessage = '';

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  getHeaders() {
    return new HttpHeaders().set('Authorization', `Bearer ${this.authService.getToken()}`);
  }

  loadDashboardData() {
    this.isLoading = true;
    const today = new Date().toISOString().split('T')[0];

    // Fetch today's appointments
    this.http.get<any>(`${environment.apiUrl}/appointments/date/${today}`, { headers: this.getHeaders() })
      .subscribe(res => {
        this.todayAppointments = res.appointments || [];
      });

    // Fetch unread messages
    this.http.get<any>(`${environment.apiUrl}/manage/contacts?status=unread`, { headers: this.getHeaders() })
      .subscribe(res => {
        this.unreadMessagesCount = res.pagination?.totalCount || 0;
      });

    // Fetch pending flags
    this.http.get<any[]>(`${environment.apiUrl}/client-reviews?status=Pending`, { headers: this.getHeaders() })
      .subscribe(res => {
        this.pendingFlagsCount = res.length || 0;
        this.isLoading = false;
      });
  }

  sendReminders() {
    this.sendingReminders = true;
    this.remindersMessage = '';
    this.http.post<any>(`${environment.apiUrl}/send-appointment-reminders`, {}, { headers: this.getHeaders() })
      .subscribe({
        next: (res) => {
          this.remindersMessage = `Success: ${res.message}`;
          this.sendingReminders = false;
        },
        error: (err) => {
          this.remindersMessage = 'Failed to send reminders.';
          this.sendingReminders = false;
        }
      });
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
