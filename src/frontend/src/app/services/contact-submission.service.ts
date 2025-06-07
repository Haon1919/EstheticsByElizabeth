import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ApiService } from './api.service';

export interface ContactSubmission {
  id: string;
  name: string;
  email: string;
  phone?: string;
  subject: string;
  message: string;
  interestedService?: string; // Optional
  preferredContactMethod?: string;
  submittedAt: string;
  status: 'unread' | 'read' | 'responded';
  readAt?: string;
  respondedAt?: string;
  adminNotes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ContactSubmissionService {

  constructor(private apiService: ApiService) { }

  /**
   * Get all contact submissions for admin review
   */
  getContactSubmissions(): Observable<ContactSubmission[]> {
    // TODO: Replace with actual API call when backend is ready
    // return this.apiService.getContactSubmissions();
    
    // For now, return mock data since we don't have the backend endpoint yet
    return of([
      {
        id: '1',
        name: 'Sarah Johnson',
        email: 'sarah.johnson@email.com',
        phone: '(555) 123-4567',
        subject: 'Facial Treatment Inquiry',
        interestedService: 'Facial',
        preferredContactMethod: 'Email',
        message: 'Hi! I\'m interested in booking a signature facial treatment. I have sensitive skin and would like to know what products you use and if they would be suitable for my skin type. Also, what is the duration of the treatment?',
        submittedAt: '2024-01-15T10:30:00Z',
        status: 'unread'
      },
      {
        id: '2',
        name: 'Michael Chen',
        email: 'michael.chen@email.com',
        phone: '(555) 987-6543',
        subject: 'Group Booking Question',
        interestedService: 'Massage',
        preferredContactMethod: 'Phone',
        message: 'Hello, I\'m planning a bachelor party and we\'re interested in booking multiple services for the group. Can you accommodate 6 people on the same day? We\'re looking at facial treatments and maybe some waxing services.',
        submittedAt: '2024-01-14T16:45:00Z',
        status: 'unread'
      },
      {
        id: '3',
        name: 'Emily Rodriguez',
        email: 'emily.rodriguez@email.com',
        subject: 'General pricing inquiry',
        preferredContactMethod: 'Email',
        message: 'Could you please send me a complete price list for all your services? I\'m particularly interested in dermaplaning and back facial treatments.',
        submittedAt: '2024-01-14T09:15:00Z',
        status: 'responded',
        readAt: '2024-01-14T10:00:00Z',
        respondedAt: '2024-01-14T10:30:00Z',
        adminNotes: 'Sent pricing list via email'
      },
      {
        id: '4',
        name: 'David Thompson',
        email: 'david.thompson@email.com',
        phone: '(555) 456-7890',
        subject: 'Question about cancellation policy',
        preferredContactMethod: 'Email',
        message: 'What is your cancellation policy? I may need to reschedule my appointment depending on my work schedule.',
        submittedAt: '2024-01-13T14:20:00Z',
        status: 'read',
        readAt: '2024-01-13T15:00:00Z'
      },
      {
        id: '5',
        name: 'Jessica Martinez',
        email: 'jessica.martinez@email.com',
        phone: '(555) 321-0987',
        subject: 'First Time Client Questions',
        interestedService: 'Body Treatment',
        preferredContactMethod: 'Phone',
        message: 'I\'ve never had a professional facial before and I\'m a bit nervous. What should I expect during my first visit? Do I need to prepare my skin in any special way beforehand?',
        submittedAt: '2024-01-12T11:00:00Z',
        status: 'unread'
      }
    ]);
  }

  /**
   * Mark a contact submission as read
   */
  markAsRead(submissionId: string): Observable<boolean> {
    return this.updateSubmissionStatus(submissionId, 'read');
  }

  /**
   * Mark a contact submission as responded
   */
  markAsResponded(submissionId: string): Observable<boolean> {
    return this.updateSubmissionStatus(submissionId, 'responded');
  }

  /**
   * Update submission status
   */
  updateSubmissionStatus(submissionId: string, status: 'unread' | 'read' | 'responded', adminNotes?: string): Observable<boolean> {
    // TODO: Implement actual API call
    console.log(`Updating submission ${submissionId} status to ${status}`, adminNotes ? ` with notes: ${adminNotes}` : '');
    return of(true);
  }

  /**
   * Add admin notes to a contact submission
   */
  updateAdminNotes(submissionId: string, notes: string): Observable<boolean> {
    // TODO: Implement actual API call
    console.log(`Updating notes for submission ${submissionId}: ${notes}`);
    return of(true);
  }

  /**
   * Delete a contact submission
   */
  deleteSubmission(submissionId: string): Observable<boolean> {
    // TODO: Implement actual API call
    console.log(`Deleting submission ${submissionId}`);
    return of(true);
  }
}
