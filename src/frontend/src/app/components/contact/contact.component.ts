import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ContactService } from '../../services/contact.service';
import { ContactRequest } from '../../models/api-models';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.css']
})
export class ContactComponent {
  contactForm: FormGroup;
  isSubmitted = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private contactService: ContactService
  ) {
    this.contactForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: [''], // Phone is optional
      subject: ['', [Validators.required, Validators.maxLength(100)]],
      interestedService: [''], // Optional
      preferredContactMethod: ['Email'],
      message: ['', Validators.required]
    });
  }

  submitForm(): void {
    if (this.contactForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      const contactData: ContactRequest = {
        name: this.contactForm.get('name')?.value,
        email: this.contactForm.get('email')?.value,
        phone: this.contactForm.get('phone')?.value,
        subject: this.contactForm.get('subject')?.value,
        interestedService: this.contactForm.get('interestedService')?.value,
        preferredContactMethod: this.contactForm.get('preferredContactMethod')?.value,
        message: this.contactForm.get('message')?.value
      };

      this.contactService.submitContactForm(contactData).subscribe({
        next: (response: any) => {
          console.log('Contact form submitted successfully:', response);
          // Use the message from the API response, or fall back to default
          this.successMessage = response?.message || 'Thank you for your message! We will get back to you soon.';
          this.isSubmitted = true;
          this.isLoading = false;
          this.contactForm.reset();
        },
        error: (error: any) => {
          console.error('Error submitting contact form:', error);
          // Try to get error message from API response
          const errorMsg = error?.error?.message || error?.message || 'There was an error sending your message. Please try again or contact us directly.';
          this.errorMessage = errorMsg;
          this.isLoading = false;
        }
      });
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.contactForm.controls).forEach(key => {
        const control = this.contactForm.get(key);
        control?.markAsTouched();
      });
    }
  }

  resetForm(): void {
    this.isSubmitted = false;
    this.successMessage = '';
    this.errorMessage = '';
    this.contactForm.reset();
  }
}