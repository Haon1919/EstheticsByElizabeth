import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ContactSubmissionService } from '../../services/contact-submission.service';
import { ContactSubmission } from '../../models/api-models';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-contact-submissions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact-submissions.component.html',
  styleUrls: ['./contact-submissions.component.css']
})
export class ContactSubmissionsComponent implements OnInit {
  submissions: ContactSubmission[] = [];
  filteredSubmissions: ContactSubmission[] = [];
  selectedSubmissions: Set<string> = new Set();
  expandedSubmissions: Set<string> = new Set();
  
  // Search and filtering
  searchTerm: string = '';
  statusFilter: string = '';
  serviceFilter: string = '';
  
  // Loading states
  loading: boolean = false;

  constructor(
    private contactSubmissionService: ContactSubmissionService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/admin']);
      return;
    }

    this.loadSubmissions();
  }

  loadSubmissions(): void {
    this.loading = true;
    this.contactSubmissionService.getContactSubmissions().subscribe({
      next: (submissions) => {
        this.submissions = submissions.sort((a, b) => 
          new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime()
        );
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading submissions:', error);
        this.loadDummyContactSubmissions();
        this.loading = false;
      }
    });
  }

  loadDummyContactSubmissions(): void {
    // Load dummy contact submissions for GitHub Pages demo
    const now = new Date();
    const today = now.toISOString();
    const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
    const threeDaysAgo = new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000).toISOString();
    const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString();

    this.submissions = [
      {
        id: '1',
        name: 'Sarah Johnson',
        email: 'sarah.johnson@email.com',
        phone: '555-123-4567',
        subject: 'Facial Treatment Inquiry',
        message: 'Hi! I\'m interested in scheduling a facial treatment. I have sensitive skin and would like to know what options you recommend. Could you also let me know about pricing and availability?',
        interestedService: 'Facial',
        preferredContactMethod: 'Email',
        submittedAt: today,
        status: 'unread',
        readAt: undefined,
        respondedAt: undefined,
        adminNotes: undefined
      },
      {
        id: '2',
        name: 'Emily Rodriguez',
        email: 'emily.r@gmail.com',
        phone: undefined,
        subject: 'Booking for Next Week',
        message: 'Hello, I would like to book an appointment for a Brazilian wax next week. What are your available times? Thank you!',
        interestedService: 'Body Treatment',
        preferredContactMethod: 'Email',
        submittedAt: yesterday,
        status: 'read',
        readAt: yesterday,
        respondedAt: undefined,
        adminNotes: 'Check calendar for availability'
      },
      {
        id: '3',
        name: 'Jessica Chen',
        email: 'jchen.work@company.com',
        phone: '555-987-6543',
        subject: 'Skincare Consultation',
        message: 'I\'m looking for a comprehensive skincare consultation. I\'ve been dealing with acne and would like professional advice on treatments and products.',
        interestedService: 'Skincare Consultation',
        preferredContactMethod: 'Phone',
        submittedAt: threeDaysAgo,
        status: 'responded',
        readAt: threeDaysAgo,
        respondedAt: new Date(new Date(threeDaysAgo).getTime() + 2 * 60 * 60 * 1000).toISOString(),
        adminNotes: 'Recommended HydraFacial treatment series'
      },
      {
        id: '4',
        name: 'Amanda Thompson',
        email: 'amanda.t@email.com',
        phone: '555-456-7890',
        subject: 'Gift Certificate Question',
        message: 'Do you offer gift certificates? I\'d like to purchase one for my friend\'s birthday. What services would you recommend for someone new to your spa?',
        interestedService: 'Other',
        preferredContactMethod: 'Email',
        submittedAt: oneWeekAgo,
        status: 'responded',
        readAt: oneWeekAgo,
        respondedAt: new Date(new Date(oneWeekAgo).getTime() + 4 * 60 * 60 * 1000).toISOString(),
        adminNotes: 'Sent gift certificate information and first-time client package details'
      },
      {
        id: '5',
        name: 'Rachel Green',
        email: 'rachel.green@email.com',
        phone: undefined,
        subject: 'Wedding Prep Package',
        message: 'I\'m getting married in 3 months and want to start a skincare routine. Do you have any bridal packages or recommendations for pre-wedding treatments?',
        interestedService: 'Facial',
        preferredContactMethod: 'Email',
        submittedAt: new Date(now.getTime() - 5 * 24 * 60 * 60 * 1000).toISOString(),
        status: 'read',
        readAt: new Date(now.getTime() - 4 * 24 * 60 * 60 * 1000).toISOString(),
        respondedAt: undefined,
        adminNotes: 'Follow up with bridal package information'
      }
    ];

    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.submissions];

    // Apply search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(s => 
        s.name.toLowerCase().includes(term) ||
        s.email.toLowerCase().includes(term) ||
        s.message.toLowerCase().includes(term) ||
        (s.phone && s.phone.includes(term))
      );
    }

    // Apply status filter
    if (this.statusFilter) {
      filtered = filtered.filter(s => s.status === this.statusFilter);
    }

    // Apply service filter
    if (this.serviceFilter) {
      filtered = filtered.filter(s => s.interestedService === this.serviceFilter);
    }

    this.filteredSubmissions = filtered;
  }

  // Selection methods
  toggleSubmissionSelection(id: string, event: any): void {
    if (event.target.checked) {
      this.selectedSubmissions.add(id);
    } else {
      this.selectedSubmissions.delete(id);
    }
  }

  toggleAllFiltered(event: any): void {
    if (event.target.checked) {
      this.filteredSubmissions.forEach(s => this.selectedSubmissions.add(s.id));
    } else {
      this.filteredSubmissions.forEach(s => this.selectedSubmissions.delete(s.id));
    }
  }

  allFilteredSelected(): boolean {
    return this.filteredSubmissions.length > 0 && 
           this.filteredSubmissions.every(s => this.selectedSubmissions.has(s.id));
  }

  someFilteredSelected(): boolean {
    return this.filteredSubmissions.some(s => this.selectedSubmissions.has(s.id));
  }

  // Bulk actions
  markSelectedAsRead(): void {
    if (this.selectedSubmissions.size === 0) return;

    this.loading = true;
    const promises = Array.from(this.selectedSubmissions).map(id => 
      this.contactSubmissionService.markAsRead(id).toPromise()
    );

    Promise.all(promises).then(() => {
      this.selectedSubmissions.forEach(id => {
        const submission = this.submissions.find(s => s.id === id);
        if (submission) {
          submission.status = 'read';
          submission.readAt = new Date().toISOString();
        }
      });
      this.selectedSubmissions.clear();
      this.applyFilters();
      this.loading = false;
    }).catch(error => {
      console.error('Error marking submissions as read:', error);
      this.loading = false;
    });
  }

  deleteSelected(): void {
    if (this.selectedSubmissions.size === 0) return;

    const count = this.selectedSubmissions.size;
    if (!confirm(`Are you sure you want to delete ${count} submission(s)?`)) {
      return;
    }

    this.loading = true;
    const promises = Array.from(this.selectedSubmissions).map(id => 
      this.contactSubmissionService.deleteSubmission(id).toPromise()
    );

    Promise.all(promises).then(() => {
      this.submissions = this.submissions.filter(s => !this.selectedSubmissions.has(s.id));
      this.selectedSubmissions.clear();
      this.applyFilters();
      this.loading = false;
    }).catch(error => {
      console.error('Error deleting submissions:', error);
      this.loading = false;
    });
  }

  // Individual actions
  toggleSubmissionDetails(id: string): void {
    if (this.expandedSubmissions.has(id)) {
      this.expandedSubmissions.delete(id);
    } else {
      this.expandedSubmissions.add(id);
    }
  }

  markAsRead(id: string): void {
    this.loading = true;
    this.contactSubmissionService.markAsRead(id).subscribe({
      next: () => {
        const submission = this.submissions.find(s => s.id === id);
        if (submission) {
          submission.status = 'read';
          submission.readAt = new Date().toISOString();
        }
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error marking as read:', error);
        this.loading = false;
      }
    });
  }

  updateStatus(id: string, status: 'unread' | 'read' | 'responded'): void {
    this.loading = true;
    
    // Find the submission
    const submission = this.submissions.find(s => s.id === id);
    if (!submission) {
      this.loading = false;
      return;
    }

    // Use the new service method
    this.contactSubmissionService.updateSubmissionStatus(id, status).subscribe({
      next: () => {
        // Update the status
        submission.status = status;
        
        // Set appropriate timestamps
        const now = new Date().toISOString();
        if (status === 'read' && !submission.readAt) {
          submission.readAt = now;
        } else if (status === 'responded') {
          submission.respondedAt = now;
          if (!submission.readAt) {
            submission.readAt = now;
          }
        }

        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error updating status:', error);
        this.loading = false;
      }
    });
  }

  updateAdminNotes(id: string, notes: string): void {
    this.contactSubmissionService.updateAdminNotes(id, notes).subscribe({
      next: () => {
        const submission = this.submissions.find(s => s.id === id);
        if (submission) {
          submission.adminNotes = notes;
        }
      },
      error: (error) => {
        console.error('Error updating notes:', error);
      }
    });
  }

  deleteSubmission(id: string): void {
    const submission = this.submissions.find(s => s.id === id);
    if (!submission) return;

    if (!confirm(`Are you sure you want to delete the submission from ${submission.name}?`)) {
      return;
    }

    this.loading = true;
    this.contactSubmissionService.deleteSubmission(id).subscribe({
      next: () => {
        this.submissions = this.submissions.filter(s => s.id !== id);
        this.selectedSubmissions.delete(id);
        this.expandedSubmissions.delete(id);
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error deleting submission:', error);
        this.loading = false;
      }
    });
  }

  // Utility methods
  getUnreadCount(): number {
    return this.filteredSubmissions.filter(s => s.status === 'unread').length;
  }

  getNoSubmissionsMessage(): string {
    if (this.searchTerm || this.statusFilter || this.serviceFilter) {
      return 'No submissions match your current filters.';
    }
    return 'No contact form submissions have been received yet.';
  }

  trackBySubmission(index: number, submission: ContactSubmission): string {
    return submission.id;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatFullDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
