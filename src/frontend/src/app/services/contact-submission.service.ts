import { Injectable } from '@angular/core';
import { Observable, of, map, catchError } from 'rxjs';
import { ApiService } from './api.service';
import { ContactSubmission, ContactSubmissionsParams } from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class ContactSubmissionService {

  constructor(private apiService: ApiService) { }

  /**
   * Get all contact submissions for admin review with optional filtering and pagination
   */
  getContactSubmissions(params?: ContactSubmissionsParams): Observable<ContactSubmission[]> {
    return this.apiService.getContactSubmissions(params).pipe(
      map(response => response.data),
      catchError(error => {
        console.error('Error fetching contact submissions:', error);
        // Return empty array on error to prevent breaking the UI
        return of([]);
      })
    );
  }

  /**
   * Get contact submissions with full response including pagination info
   */
  getContactSubmissionsWithPagination(params?: ContactSubmissionsParams): Observable<any> {
    return this.apiService.getContactSubmissions(params).pipe(
      catchError(error => {
        console.error('Error fetching contact submissions:', error);
        // Return empty response structure on error
        return of({
          success: false,
          data: [],
          pagination: { page: 1, pageSize: 50, totalCount: 0, totalPages: 0 },
          filters: {}
        });
      })
    );
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
    return this.apiService.updateContactSubmissionStatus(submissionId, status, adminNotes).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error updating submission ${submissionId} status to ${status}:`, error);
        return of(false);
      })
    );
  }

  /**
   * Add admin notes to a contact submission
   */
  updateAdminNotes(submissionId: string, notes: string): Observable<boolean> {
    return this.apiService.updateContactSubmissionNotes(submissionId, notes).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error updating notes for submission ${submissionId}:`, error);
        return of(false);
      })
    );
  }

  /**
   * Delete a contact submission
   */
  deleteSubmission(submissionId: string): Observable<boolean> {
    return this.apiService.deleteContactSubmission(submissionId).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error deleting submission ${submissionId}:`, error);
        return of(false);
      })
    );
  }
}
