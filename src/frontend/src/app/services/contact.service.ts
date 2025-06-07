import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ApiService } from './api.service';
import { ContactRequest } from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class ContactService {

  constructor(private apiService: ApiService) { }

  /**
   * Submit contact form
   * Note: This will try to use the API service, but if no contact endpoint exists,
   * it will fall back to a mock submission for now
   */
  submitContactForm(contactData: ContactRequest): Observable<any> {
    // Try to submit via API first
    try {
      return this.apiService.submitContactForm(contactData);
    } catch (error) {
      // If API submission fails, fall back to mock success
      console.log('Contact form submitted (mock):', contactData);
      return of({ 
        success: true, 
        message: 'Contact form submitted successfully',
        timestamp: new Date().toISOString()
      });
    }
  }

  /**
   * Validate contact form data before submission
   */
  validateContactData(contactData: ContactRequest): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!contactData.name || contactData.name.trim().length === 0) {
      errors.push('Name is required');
    }

    if (!contactData.email || contactData.email.trim().length === 0) {
      errors.push('Email is required');
    } else if (!this.isValidEmail(contactData.email)) {
      errors.push('Invalid email format');
    }

    if (!contactData.phone || contactData.phone.trim().length === 0) {
      errors.push('Phone number is required');
    }

    if (!contactData.subject || contactData.subject.trim().length === 0) {
      errors.push('Subject is required');
    }

    if (!contactData.message || contactData.message.trim().length === 0) {
      errors.push('Message is required');
    } else if (contactData.message.trim().length < 10) {
      errors.push('Message must be at least 10 characters long');
    }

    return {
      isValid: errors.length === 0,
      errors
    };
  }

  /**
   * Simple email validation
   */
  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  /**
   * Format phone number for display
   */
  formatPhoneNumber(phone: string): string {
    // Remove all non-digit characters
    const cleaned = phone.replace(/\D/g, '');
    
    // Format as (XXX) XXX-XXXX if US number
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    
    // Return original if not a standard US number
    return phone;
  }

  /**
   * Get common contact subjects for dropdown
   */
  getContactSubjects(): string[] {
    return [
      'booking',
      'services', 
      'pricing',
      'feedback',
      'other'
    ];
  }

  /**
   * Get contact subject display text
   */
  getSubjectDisplayText(subject: string): string {
    const subjectMap: { [key: string]: string } = {
      'booking': 'Booking Information',
      'services': 'Service Inquiry',
      'pricing': 'Pricing Information', 
      'feedback': 'Feedback',
      'other': 'Other'
    };
    
    return subjectMap[subject] || subject;
  }
}
