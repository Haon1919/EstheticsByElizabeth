import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.css']
})
export class AdminSettingsComponent implements OnInit {
  settings: any = {
    LlmProvider: 'Gemini',
    LlmApiKey: '',
    LlmEndpoint: '',
    LlmModel: 'gemini-2.5-flash'
  };
  
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.loadSettings();
  }

  getHeaders() {
    return new HttpHeaders().set('Authorization', `Bearer ${this.authService.getToken()}`);
  }

  loadSettings() {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/manage/settings`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          if (data) {
            this.settings.LlmProvider = data.LlmProvider || 'Gemini';
            this.settings.LlmApiKey = data.LlmApiKey || '';
            this.settings.LlmEndpoint = data.LlmEndpoint || '';
            this.settings.LlmModel = data.LlmModel || '';
          }
          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = 'Failed to load settings.';
          this.isLoading = false;
        }
      });
  }

  saveSettings() {
    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.http.put(`${environment.apiUrl}/manage/settings`, this.settings, { headers: this.getHeaders() })
      .subscribe({
        next: () => {
          this.successMessage = 'Settings saved successfully!';
          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = 'Failed to save settings.';
          this.isLoading = false;
        }
      });
  }
}
