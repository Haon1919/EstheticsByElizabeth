import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppointmentService } from '../../services/appointment.service';
import { ServiceManagementService } from '../../services/service-management.service';
import { Service, CreateAppointmentRequest } from '../../models/api-models';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.css']
})
export class BookingComponent implements OnInit {
  bookingForm: FormGroup;
  selectedDate: Date = new Date();
  selectedTime: string = '';
  availableTimes: string[] = [];
  services: Service[] = [];
  isSubmitted = false;
  isPaymentStep = false;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder, 
    private route: ActivatedRoute, 
    private appointmentService: AppointmentService,
    private serviceManagementService: ServiceManagementService
  ) {
    this.bookingForm = this.fb.group({
      service: ['', Validators.required],
      date: ['', Validators.required],
      time: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      notes: [''],
      // Payment fields
      cardName: ['', Validators.required],
      cardNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{16}$/)]],
      expiryDate: ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
      cvv: ['', [Validators.required, Validators.pattern(/^[0-9]{3,4}$/)]]
    });
  }

  ngOnInit(): void {
    // Load services from API
    this.loadServices();
    
    // Check for pre-selected service from query params
    this.route.queryParams.subscribe(params => {
      if (params['service']) {
        const serviceId = parseInt(params['service']);
        this.bookingForm.get('service')?.setValue(serviceId);
      }
    });

    // Generate available time slots
    this.generateTimeSlots();
  }

  loadServices(): void {
    this.serviceManagementService.loadServices().subscribe({
      next: (services: Service[]) => {
        // Only show bookable services (services with prices)
        this.services = services.filter(service => service.price !== undefined && service.price !== null);
      },
      error: (error: any) => {
        console.error('Error loading services:', error);
        this.errorMessage = 'Failed to load services. Please try again.';
        
        // Fallback to dummy data for GitHub Pages
        this.loadDummyServices();
      }
    });
  }

  loadDummyServices(): void {
    // Dummy services data for GitHub Pages display
    this.services = [
      {
        id: 1,
        name: 'Signature Facial',
        description: 'A personalized facial treatment tailored to your specific skin needs and concerns.',
        price: 95,
        duration: 60,
        category: { id: 1, name: 'Facial Treatments' }
      },
      {
        id: 2,
        name: 'Dermaplane + Mini Facial',
        description: 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial.',
        price: 100,
        duration: 60,
        category: { id: 1, name: 'Facial Treatments' }
      },
      {
        id: 3,
        name: 'Back Facial',
        description: 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.',
        price: 115,
        duration: 60,
        category: { id: 1, name: 'Facial Treatments' }
      },
      {
        id: 4,
        name: 'Upper Lip Wax',
        description: 'Quick and precise removal of unwanted hair from the upper lip area.',
        price: 15,
        duration: 5,
        category: { id: 2, name: 'Waxing' }
      },
      {
        id: 5,
        name: 'Eyebrow Wax',
        description: 'Precise shaping and grooming of eyebrows for a clean, defined look.',
        price: 20,
        duration: 10,
        category: { id: 2, name: 'Waxing' }
      },
      {
        id: 6,
        name: 'Full Face Wax',
        description: 'Complete facial hair removal for smooth, hair-free skin.',
        price: 45,
        duration: 25,
        category: { id: 2, name: 'Waxing' }
      }
    ];
    this.errorMessage = '';
  }

  generateTimeSlots(): void {
    const times = [];
    for (let i = 9; i <= 18; i++) {
      times.push(`${i}:00`);
      if (i < 18) {
        times.push(`${i}:30`);
      }
    }
    this.availableTimes = times;
  }

  onDateChange(event: any): void {
    this.selectedDate = new Date(event.target.value);
    this.bookingForm.get('date')?.setValue(event.target.value);
    // In a real app, you would check availability for this date
  }

  onTimeSelect(time: string): void {
    this.selectedTime = time;
    this.bookingForm.get('time')?.setValue(time);
  }

  getServiceName(serviceId: number): string {
    const service = this.services.find(s => s.id === serviceId);
    return service ? service.name : '';
  }

  getServicePrice(serviceId: number): number {
    const service = this.services.find(s => s.id === serviceId);
    return service?.price || 0;
  }

  proceedToPayment(): void {
    if (this.bookingForm.get('service')?.valid && 
        this.bookingForm.get('date')?.valid && 
        this.bookingForm.get('time')?.valid && 
        this.bookingForm.get('firstName')?.valid && 
        this.bookingForm.get('lastName')?.valid && 
        this.bookingForm.get('email')?.valid && 
        this.bookingForm.get('phone')?.valid) {
      this.isPaymentStep = true;
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.bookingForm.controls).forEach(key => {
        const control = this.bookingForm.get(key);
        control?.markAsTouched();
      });
    }
  }

  submitBooking(): void {
    if (this.bookingForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';

      // Get first and last name directly from form fields
      const firstName = this.bookingForm.get('firstName')?.value?.trim() || '';
      const lastName = this.bookingForm.get('lastName')?.value?.trim() || '';

      // Combine date and time into a proper DateTime format
      const dateValue = this.bookingForm.get('date')?.value;
      const timeValue = this.bookingForm.get('time')?.value;
      const appointmentDateTime = new Date(`${dateValue}T${timeValue}:00`);

      // Prepare the payload to match the CreateAppointmentRequest interface
      const appointmentData: CreateAppointmentRequest = {
        client: {
          firstName: firstName,
          lastName: lastName,
          email: this.bookingForm.get('email')?.value,
          phoneNumber: this.bookingForm.get('phone')?.value
        },
        serviceId: parseInt(this.bookingForm.get('service')?.value),
        time: appointmentDateTime.toISOString()
      };

      console.log('Sending appointment data:', appointmentData);

      // Use the AppointmentService to create the appointment
      this.appointmentService.scheduleAppointment(appointmentData).subscribe({
        next: (response: any) => {
          console.log('Booking successful:', response);
          this.isSubmitted = true;
          this.isLoading = false;
        },
        error: (error: any) => {
          console.error('Booking failed:', error);
          this.isLoading = false;
          
          if (error.status === 400) {
            this.errorMessage = 'Please check your booking details and try again.';
          } else if (error.status === 409) {
            this.errorMessage = error.error?.message || 'The selected time slot is no longer available.';
          } else if (error.status === 404) {
            this.errorMessage = 'The selected service is not available.';
          } else {
            this.errorMessage = 'An error occurred while booking your appointment. Please try again.';
          }
        }
      });
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.bookingForm.controls).forEach(key => {
        const control = this.bookingForm.get(key);
        control?.markAsTouched();
      });
    }
  }

  goBack(): void {
    this.isPaymentStep = false;
  }

  bookAnother(): void {
    this.isSubmitted = false;
    this.isPaymentStep = false;
    this.isLoading = false;
    this.errorMessage = '';
    this.bookingForm.reset();
    this.selectedTime = '';
  }
}