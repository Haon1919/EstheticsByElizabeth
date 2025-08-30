import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AppointmentService } from '../../services/appointment.service';
import { AuthService } from '../../services/auth.service';
import { ServiceManagementService } from '../../services/service-management.service';
import {
  Appointment,
  AppointmentsByDateResponse,
  AppointmentHistoryResponse,
  CreateAppointmentRequest,
  Client,
  EarliestAppointmentDateResponse
} from '../../models/api-models';
import { Service } from '../../models/services.models';

@Component({
  selector: 'app-admin-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-appointments.component.html',
  styleUrls: ['./admin-appointments.component.css']
})
export class AdminAppointmentsComponent implements OnInit {
  // Active tab
  activeTab: 'calendar' | 'search' | 'schedule' = 'calendar';
  
  // Loading states
  loading = false;
  searchLoading = false;
  scheduleLoading = false;
  
  // Calendar view
  selectedDate: string = '';
  selectedEndDate: string = '';
  isDateRangeMode: boolean = false;
  appointments: Appointment[] = [];
  appointmentResponse: AppointmentsByDateResponse | null = null;
  
  // Client search
  searchEmail: string = '';
  searchResults: AppointmentHistoryResponse | null = null;
  selectedServiceFilter: number = 0; // 0 means all services
  filteredAppointments: Appointment[] = [];
  uniqueServicesInHistory: Service[] = []; // Services that appear in the client's history
  
  // Schedule new appointment
  newAppointment = {
    client: {
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: ''
    },
    serviceId: 0,
    date: '',
    time: ''
  };
  
  // Available services and time slots
  services: Service[] = [];
  availableTimes: string[] = [];
  
  // Error handling
  errorMessage = '';
  successMessage = '';

  constructor(
    private appointmentService: AppointmentService,
    private authService: AuthService,
    private serviceManagementService: ServiceManagementService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/admin']);
      return;
    }

    // Initialize date with earliest appointment date or today's date as fallback
    this.initializeDates();
    this.loadServices();
    this.generateTimeSlots();
    
    // Initialize filtered appointments
    this.filteredAppointments = [];
  }

  private initializeDates(): void {
    // Get earliest appointment date from backend
    this.appointmentService.getEarliestAppointmentDate().subscribe({
      next: (response: EarliestAppointmentDateResponse) => {
        let dateToUse: string;
        if (response.hasAppointments && response.earliestDate) {
          // Use earliest appointment date
          dateToUse = response.earliestDate;
        } else {
          // Fallback to today's date if no appointments exist
          dateToUse = this.formatDateForInput(new Date());
        }
        
        // Set both selectedDate and selectedEndDate
        this.selectedDate = dateToUse;
        this.selectedEndDate = dateToUse;
        this.newAppointment.date = dateToUse;
        
        // Load appointments for the initialized date
        this.loadAppointmentsByDate();
      },
      error: (error) => {
        console.error('Error getting earliest appointment date:', error);
        // Fallback to today's date on error
        const todayDate = this.formatDateForInput(new Date());
        this.selectedDate = todayDate;
        this.selectedEndDate = todayDate;
        this.newAppointment.date = todayDate;
        
        // Load appointments for today's date
        this.loadAppointmentsByDate();
      }
    });
  }

  // Tab management
  switchTab(tab: 'calendar' | 'search' | 'schedule'): void {
    this.activeTab = tab;
    this.clearMessages();
    
    // Reset search filters when switching to search tab
    if (tab === 'search') {
      this.selectedServiceFilter = 0;
      this.filteredAppointments = [];
      this.uniqueServicesInHistory = [];
    }
  }

  // Calendar view methods
  setDateMode(isRange: boolean): void {
    if (this.isDateRangeMode !== isRange) {
      this.isDateRangeMode = isRange;
      if (!this.isDateRangeMode) {
        // Reset end date to start date when switching back to single date mode
        this.selectedEndDate = this.selectedDate;
      }
      this.loadAppointmentsByDate();
    }
  }

  toggleDateRangeMode(): void {
    this.isDateRangeMode = !this.isDateRangeMode;
    if (!this.isDateRangeMode) {
      // Reset end date to start date when switching back to single date mode
      this.selectedEndDate = this.selectedDate;
    }
    this.loadAppointmentsByDate();
  }

  loadAppointmentsByDate(): void {
    console.log('yeet')
    if (!this.selectedDate) return;
    console.log('yoot')
    console.log(this.isDateRangeMode, this.selectedEndDate, this.selectedDate);

    this.loading = true;
    this.clearMessages();

    if (this.isDateRangeMode && this.selectedEndDate && this.selectedEndDate !== this.selectedDate) {
      // Load date range
      this.appointmentService.getAppointmentsByDateRange(this.selectedDate, this.selectedEndDate).subscribe({
        next: (response: AppointmentsByDateResponse) => {
          this.appointmentResponse = response;
          this.appointments = response.appointments;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading appointments:', error);
          this.errorMessage = 'Failed to load appointments for the selected date range.';
          this.loading = false;
        }
      });
    } else {
      // Load single date
      this.appointmentService.getAppointmentsByDate(this.selectedDate).subscribe({
        next: (response: AppointmentsByDateResponse) => {
          this.appointmentResponse = response;
          this.appointments = response.appointments;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading appointments:', error);
          this.errorMessage = 'Failed to load appointments for the selected date.';
          this.loading = false;
        }
      });
    }
  }

  onDateChange(): void {
    // If end date is before start date, adjust it
    if (this.selectedEndDate < this.selectedDate) {
      this.selectedEndDate = this.selectedDate;
    }
    this.loadAppointmentsByDate();
  }

  onEndDateChange(): void {
    // If end date is before start date, adjust start date
    if (this.selectedEndDate < this.selectedDate) {
      this.selectedDate = this.selectedEndDate;
    }
    this.loadAppointmentsByDate();
  }

  // Client search methods
  searchClientAppointments(): void {
    if (!this.searchEmail.trim()) {
      this.errorMessage = 'Please enter a client email address.';
      return;
    }

    this.searchLoading = true;
    this.clearMessages();

    this.appointmentService.getClientAppointmentHistory(this.searchEmail.trim()).subscribe({
      next: (response: AppointmentHistoryResponse) => {
        this.searchResults = response;
        this.extractUniqueServices(); // Extract unique services from history
        this.applyServiceFilter(); // Apply filter after loading data
        if (response.appointments.length === 0) {
          this.errorMessage = 'No appointments found for this client.';
        }
        this.searchLoading = false;
      },
      error: (error) => {
        console.error('Error searching client appointments:', error);
        this.errorMessage = 'Failed to load client appointment history.';
        this.searchLoading = false;
      }
    });
  }

  extractUniqueServices(): void {
    if (!this.searchResults) {
      this.uniqueServicesInHistory = [];
      return;
    }

    // Extract unique services from appointment history
    const serviceMap = new Map<number, Service>();
    
    this.searchResults.appointments.forEach(appointment => {
      if (!serviceMap.has(appointment.service.id)) {
        serviceMap.set(appointment.service.id, appointment.service);
      }
    });

    this.uniqueServicesInHistory = Array.from(serviceMap.values());
    console.log('Unique services in history:', this.uniqueServicesInHistory);
  }

  applyServiceFilter(): void {
    if (!this.searchResults) {
      this.filteredAppointments = [];
      return;
    }

    console.log('Applying service filter. Selected filter:', this.selectedServiceFilter);
    console.log('Available appointments:', this.searchResults.appointments.length);

    if (this.selectedServiceFilter === 0) {
      // Show all appointments
      this.filteredAppointments = [...this.searchResults.appointments];
      console.log('Showing all appointments:', this.filteredAppointments.length);
    } else {
      // Filter by selected service
      this.filteredAppointments = this.searchResults.appointments.filter(
        appointment => {
          console.log(`Comparing appointment service ID ${appointment.service.id} with filter ${this.selectedServiceFilter}`);
          return appointment.service.id === this.selectedServiceFilter;
        }
      );
      console.log('Filtered appointments:', this.filteredAppointments.length);
    }
  }

  onServiceFilterChange(): void {
    // Convert string value to number (HTML select returns string)
    this.selectedServiceFilter = Number(this.selectedServiceFilter);
    this.applyServiceFilter();
  }

  // Check if this is the most recent appointment for this service
  isMostRecentForService(appointment: Appointment): boolean {
    if (!this.searchResults) return false;
    
    // Find all appointments for the same service
    const serviceAppointments = this.searchResults.appointments.filter(
      app => app.service.id === appointment.service.id
    );
    
    // Sort by time descending and check if this is the first (most recent)
    serviceAppointments.sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime());
    
    return serviceAppointments.length > 0 && serviceAppointments[0].id === appointment.id;
  }

  // Re-book appointment with same service and client
  rebookAppointment(appointment: Appointment): void {
    // Switch to schedule tab and pre-populate form
    this.activeTab = 'schedule';
    console.log('Appointment object:', appointment);
    console.log('Client data:', appointment.client);
    
    // Calculate the rebook date based on the original appointment date + appointmentBufferTime
    let rebookDate: Date;
    if (appointment.service.appointmentBufferTime && appointment.service.appointmentBufferTime > 0) {
      // Parse the original appointment date
      const originalDate = new Date(appointment.time);
      // Add the buffer time (in weeks) to the original date
      rebookDate = new Date(originalDate);
      rebookDate.setDate(originalDate.getDate() + (appointment.service.appointmentBufferTime * 7));
      console.log(`Calculated rebook date: ${rebookDate.toISOString()} (original: ${originalDate.toISOString()}, buffer: ${appointment.service.appointmentBufferTime} weeks)`);
    } else {
      // Fallback to today's date if no buffer time is set
      rebookDate = new Date();
      console.log('No appointment buffer time set, using today\'s date as fallback');
    }
    
    // Pre-populate the form with client and service data
    this.newAppointment = {
      client: {
        firstName: appointment.client.firstName,
        lastName: appointment.client.lastName,
        email: appointment.client.email,
        phoneNumber: appointment.client.phoneNumber || ''
      },
      serviceId: appointment.service.id,
      date: this.formatDateForInput(rebookDate),
      time: ''
    };
    
    const clientName = `${appointment.client.firstName} ${appointment.client.lastName}`.trim();
    const bufferWeeks = appointment.service.appointmentBufferTime || 0;
    this.successMessage = `Pre-filled form for ${clientName} - ${appointment.service.name}. Recommended date: ${rebookDate.toLocaleDateString()} (${bufferWeeks} weeks from original appointment)`;
  }

  // Schedule new appointment methods
  loadServices(): void {
    this.serviceManagementService.loadServices().subscribe({
      next: (services: Service[]) => {
        this.services = services.filter(service => 
          service.price !== undefined && service.price !== null && service.price > 0
        );
      },
      error: (error) => {
        console.error('Error loading services:', error);
        this.errorMessage = 'Failed to load services.';
      }
    });
  }

  generateTimeSlots(): void {
    const times = [];
    for (let i = 9; i <= 18; i++) {
      times.push(`${i.toString().padStart(2, '0')}:00`);
      if (i < 18) {
        times.push(`${i.toString().padStart(2, '0')}:30`);
      }
    }
    this.availableTimes = times;
  }

  scheduleNewAppointment(): void {
    if (!this.validateNewAppointment()) {
      return;
    }

    this.scheduleLoading = true;
    this.clearMessages();

    // Combine date and time
    const appointmentDateTime = new Date(`${this.newAppointment.date}T${this.newAppointment.time}:00`);

    const appointmentData: CreateAppointmentRequest = {
      client: {
        firstName: this.newAppointment.client.firstName,
        lastName: this.newAppointment.client.lastName,
        email: this.newAppointment.client.email,
        phoneNumber: this.newAppointment.client.phoneNumber
      },
      serviceId: this.newAppointment.serviceId,
      time: appointmentDateTime.toISOString()
    };

    this.appointmentService.scheduleAppointment(appointmentData).subscribe({
      next: (response) => {
        this.successMessage = 'Appointment scheduled successfully!';
        this.resetNewAppointmentForm();
        this.scheduleLoading = false;
        
        // Refresh calendar if on the same date
        if (this.selectedDate === this.newAppointment.date) {
          this.loadAppointmentsByDate();
        }
      },
      error: (error) => {
        console.error('Error scheduling appointment:', error);
        this.errorMessage = error.error?.message || 'Failed to schedule appointment.';
        this.scheduleLoading = false;
      }
    });
  }

  validateNewAppointment(): boolean {
    if (!this.newAppointment.client.firstName.trim()) {
      this.errorMessage = 'First name is required.';
      return false;
    }
    if (!this.newAppointment.client.lastName.trim()) {
      this.errorMessage = 'Last name is required.';
      return false;
    }
    if (!this.newAppointment.client.email.trim()) {
      this.errorMessage = 'Email is required.';
      return false;
    }
    if (!this.newAppointment.serviceId) {
      this.errorMessage = 'Please select a service.';
      return false;
    }
    if (!this.newAppointment.date) {
      this.errorMessage = 'Please select a date.';
      return false;
    }
    if (!this.newAppointment.time) {
      this.errorMessage = 'Please select a time.';
      return false;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.newAppointment.client.email)) {
      this.errorMessage = 'Please enter a valid email address.';
      return false;
    }

    return true;
  }

  resetNewAppointmentForm(): void {
    this.newAppointment = {
      client: {
        firstName: '',
        lastName: '',
        email: '',
        phoneNumber: ''
      },
      serviceId: 0,
      date: this.formatDateForInput(new Date()),
      time: ''
    };
  }

  // Cancel appointment
  cancelAppointment(appointmentId: number): void {
    if (!confirm('Are you sure you want to cancel this appointment?')) {
      return;
    }

    this.loading = true;
    this.clearMessages();

    this.appointmentService.cancelAppointment(appointmentId).subscribe({
      next: () => {
        this.successMessage = 'Appointment cancelled successfully.';
        this.loadAppointmentsByDate(); // Refresh the list
        this.loading = false;
      },
      error: (error) => {
        console.error('Error cancelling appointment:', error);
        this.errorMessage = 'Failed to cancel appointment.';
        this.loading = false;
      }
    });
  }

  // Utility methods
  formatDate(dateString: string): string {
    try {
      if (!dateString) return 'No date selected';
      
      const date = new Date(dateString);
      if (isNaN(date.getTime())) {
        return 'No date selected';
      }
      return date.toLocaleDateString('en-US', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      });
    } catch (error) {
      return 'No date selected';
    }
  }

  formatDateRange(): string {
    if (!this.appointmentResponse) {
      // Fallback to selectedDate if appointmentResponse is not available
      if (this.selectedDate) {
        if (this.isDateRangeMode && this.selectedEndDate && this.selectedEndDate !== this.selectedDate) {
          const startDate = this.formatDate(this.selectedDate + 'T00:00:00');
          const endDate = this.formatDate(this.selectedEndDate + 'T00:00:00');
          return `${startDate} - ${endDate}`;
        } else {
          return this.formatDate(this.selectedDate + 'T00:00:00');
        }
      }
      return 'Select a date';
    }
    
    if (this.appointmentResponse.isDateRange) {
      const startDate = this.formatDate(this.appointmentResponse.startDate + 'T00:00:00');
      const endDate = this.formatDate(this.appointmentResponse.endDate + 'T00:00:00');
      return `${startDate} - ${endDate}`;
    } else {
      return this.formatDate(this.appointmentResponse.startDate + 'T00:00:00');
    }
  }

  getDateRangeText(): string {
    if (!this.appointmentResponse) return 'Select a date';
    
    if (this.appointmentResponse.isDateRange) {
      return `${this.appointmentResponse.startDate} to ${this.appointmentResponse.endDate}`;
    } else {
      return this.appointmentResponse.startDate;
    }
  }

  getDateRangeDays(): number {
    if (!this.appointmentResponse || !this.appointmentResponse.isDateRange) return 1;
    
    const start = new Date(this.appointmentResponse.startDate);
    const end = new Date(this.appointmentResponse.endDate);
    const diffTime = Math.abs(end.getTime() - start.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDateForInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  getServiceName(serviceId: number): string {
    const service = this.services.find(s => s.id === serviceId);
    return service ? service.name : `Service ID ${serviceId}`;
  }

  getServicePrice(serviceId: number): number {
    const service = this.services.find(s => s.id === serviceId);
    return service?.price || 0;
  }

  clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/admin']);
  }

  trackByAppointment(index: number, appointment: Appointment): number {
    return appointment.id;
  }

  // Helper methods for template
  isUpcoming(appointmentTime: string): boolean {
    return new Date(appointmentTime) > new Date();
  }

  getAppointmentStatusClass(appointmentTime: string): string {
    return this.isUpcoming(appointmentTime) ? 'upcoming' : 'completed';
  }

  getAppointmentStatusText(appointmentTime: string): string {
    return this.isUpcoming(appointmentTime) ? 'Upcoming' : 'Completed';
  }

  getStatsDateDisplay(): string {
    if (!this.selectedDate) {
      return 'Select a date';
    }
    
    if (this.isDateRangeMode && this.selectedEndDate && this.selectedEndDate !== this.selectedDate) {
      const startDate = this.formatDate(this.selectedDate + 'T00:00:00');
      const endDate = this.formatDate(this.selectedEndDate + 'T00:00:00');
      return `${startDate} - ${endDate}`;
    } else {
      return this.formatDate(this.selectedDate + 'T00:00:00');
    }
  }
}