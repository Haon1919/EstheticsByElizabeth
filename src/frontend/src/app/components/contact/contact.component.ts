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
      phone: ['', Validators.required],
      subject: ['', Validators.required],
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
        message: this.contactForm.get('message')?.value
      };

      this.contactService.submitContactForm(contactData).subscribe({
        next: (response: any) => {
          console.log('Contact form submitted successfully:', response);
          this.successMessage = 'Thank you for your message! We will get back to you soon.';
          this.isSubmitted = true;
          this.isLoading = false;
          this.contactForm.reset();
        },
        error: (error: any) => {
          console.error('Error submitting contact form:', error);
          this.errorMessage = 'There was an error sending your message. Please try again or contact us directly.';
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