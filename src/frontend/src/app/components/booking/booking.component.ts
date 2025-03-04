import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

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
  services = [
    { id: 101, name: 'Custom Facial', price: 85 },
    { id: 102, name: 'Deep Cleansing Facial', price: 95 },
    { id: 103, name: 'Anti-Aging Facial', price: 120 },
    { id: 104, name: 'Hydrating Facial', price: 90 },
    { id: 201, name: 'Body Scrub', price: 70 },
    { id: 202, name: 'Body Wrap', price: 95 },
    { id: 203, name: 'Massage Therapy', price: 85 },
    { id: 204, name: 'Back Facial', price: 75 },
    { id: 301, name: 'Eyebrow Waxing', price: 20 },
    { id: 302, name: 'Lip & Chin Waxing', price: 18 },
    { id: 303, name: 'Full Leg Waxing', price: 65 },
    { id: 304, name: 'Brazilian Waxing', price: 70 },
    { id: 401, name: 'Special Event Makeup', price: 75 },
    { id: 402, name: 'Bridal Makeup', price: 150 },
    { id: 403, name: 'Makeup Lesson', price: 100 }
  ];
  isSubmitted = false;
  isPaymentStep = false;

  constructor(private fb: FormBuilder, private route: ActivatedRoute) {
    this.bookingForm = this.fb.group({
      service: ['', Validators.required],
      date: ['', Validators.required],
      time: ['', Validators.required],
      name: ['', Validators.required],
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
    this.route.queryParams.subscribe(params => {
      if (params['service']) {
        const serviceId = parseInt(params['service']);
        this.bookingForm.get('service')?.setValue(serviceId);
      }
    });

    // Generate available time slots
    this.generateTimeSlots();
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
    return service ? service.price : 0;
  }

  proceedToPayment(): void {
    if (this.bookingForm.get('service')?.valid && 
        this.bookingForm.get('date')?.valid && 
        this.bookingForm.get('time')?.valid && 
        this.bookingForm.get('name')?.valid && 
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
      // In a real app, you would send the form data to your backend
      console.log('Booking submitted:', this.bookingForm.value);
      this.isSubmitted = true;
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
    this.bookingForm.reset();
    this.selectedTime = '';
  }
}