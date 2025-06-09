import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { 
  CreateAppointmentRequest,
  Appointment,
  AppointmentHistoryResponse,
  AppointmentsByDateResponse 
} from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {

  constructor(private apiService: ApiService) { }

  /**
   * Schedule a new appointment
   */
  scheduleAppointment(appointmentData: CreateAppointmentRequest): Observable<Appointment> {
    return this.apiService.createAppointment(appointmentData);
  }

  /**
   * Get appointments for a specific date
   */
  getAppointmentsByDate(date: string): Observable<AppointmentsByDateResponse> {
    return this.apiService.getAppointmentsByDate(date);
  }

  /**
   * Get appointments for a date range
   */
  getAppointmentsByDateRange(startDate: string, endDate: string): Observable<AppointmentsByDateResponse> {
    return this.apiService.getAppointmentsByDateRange(startDate, endDate);
  }

  /**
   * Get appointment history for a client by email
   */
  getClientAppointmentHistory(email: string): Observable<AppointmentHistoryResponse> {
    return this.apiService.getAppointmentHistory(email);
  }

  /**
   * Cancel an appointment
   */
  cancelAppointment(appointmentId: number): Observable<any> {
    return this.apiService.cancelAppointment(appointmentId);
  }

  /**
   * Check if a specific time slot is available
   * This can be implemented by checking existing appointments for that date
   */
  checkTimeAvailability(date: string, time: string): Observable<boolean> {
    return new Observable(observer => {
      this.getAppointmentsByDate(date).subscribe({
        next: (response) => {
          const appointmentDateTime = new Date(`${date}T${time}`);
          const isAvailable = !response.appointments.some(appointment => {
            const existingDateTime = new Date(appointment.time);
            return existingDateTime.getTime() === appointmentDateTime.getTime();
          });
          observer.next(isAvailable);
          observer.complete();
        },
        error: (error) => {
          console.error('Error checking time availability:', error);
          observer.next(true); // Assume available if can't check
          observer.complete();
        }
      });
    });
  }

  /**
   * Format appointment time for display
   */
  formatAppointmentTime(time: string): string {
    const date = new Date(time);
    return date.toLocaleString();
  }

  /**
   * Format appointment date for API requests (YYYY-MM-DD)
   */
  formatDateForApi(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  /**
   * Create appointment request object from form data
   */
  createAppointmentRequest(
    serviceId: number,
    date: string,
    time: string,
    clientData: {
      firstName: string;
      lastName: string;
      email: string;
      phoneNumber: string;
    }
  ): CreateAppointmentRequest {
    // Combine date and time into ISO string format
    const appointmentDateTime = new Date(`${date}T${time}:00`);
    
    return {
      serviceId: serviceId,
      time: appointmentDateTime.toISOString(),
      client: {
        firstName: clientData.firstName,
        lastName: clientData.lastName,
        email: clientData.email,
        phoneNumber: clientData.phoneNumber
      }
    };
  }
}
