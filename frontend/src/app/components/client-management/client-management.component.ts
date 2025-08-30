import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClientManagementService } from '../../services/client-management.service';
import { AuthService } from '../../services/auth.service';
import { ClientReviewFlag } from '../../models/api-models';

@Component({
  selector: 'app-client-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './client-management.component.html',
  styleUrls: ['./client-management.component.css']
})
export class ClientManagementComponent implements OnInit {
  reviewFlags: ClientReviewFlag[] = [];
  filteredFlags: ClientReviewFlag[] = [];
  selectedFlags: Set<number> = new Set();
  expandedFlags: Set<number> = new Set();
  
  // Search and filtering
  searchTerm: string = '';
  statusFilter: string = '';
  clientFilter: string = '';
  
  // Tab management
  activeTab: 'all' | 'pending' | 'banned' = 'all';
  
  // Loading states
  loading: boolean = false;
  actionLoading: Set<number> = new Set();
  
  // Admin details for actions
  adminName: string = 'Admin';

  constructor(
    private clientManagementService: ClientManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/admin']);
      return;
    }

    this.loadReviewFlags();
  }

  loadReviewFlags(): void {
    this.loading = true;
    this.clientManagementService.getClientReviewFlags().subscribe({
      next: (flags) => {
        this.reviewFlags = flags.sort((a, b) => 
          new Date(b.flagDate).getTime() - new Date(a.flagDate).getTime()
        );
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading review flags:', error);
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.reviewFlags];

    // Apply tab filter
    if (this.activeTab === 'pending') {
      filtered = filtered.filter(f => f.status === 'Pending');
    } else if (this.activeTab === 'banned') {
      filtered = filtered.filter(f => f.status === 'Banned');
    }

    // Apply search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(f => 
        f.client.firstName.toLowerCase().includes(term) ||
        f.client.lastName.toLowerCase().includes(term) ||
        f.client.email.toLowerCase().includes(term) ||
        f.flagReason.toLowerCase().includes(term)
      );
    }

    // Apply status filter
    if (this.statusFilter) {
      filtered = filtered.filter(f => f.status === this.statusFilter);
    }

    // Apply client filter
    if (this.clientFilter) {
      const clientTerm = this.clientFilter.toLowerCase().trim();
      filtered = filtered.filter(f => 
        f.client.firstName.toLowerCase().includes(clientTerm) ||
        f.client.lastName.toLowerCase().includes(clientTerm) ||
        f.client.email.toLowerCase().includes(clientTerm)
      );
    }

    this.filteredFlags = filtered;
  }

  // Tab management
  switchTab(tab: 'all' | 'pending' | 'banned'): void {
    this.activeTab = tab;
    this.applyFilters();
  }

  // Selection methods
  toggleFlagSelection(flagId: number, event: any): void {
    if (event.target.checked) {
      this.selectedFlags.add(flagId);
    } else {
      this.selectedFlags.delete(flagId);
    }
  }

  toggleAllFiltered(event: any): void {
    if (event.target.checked) {
      this.filteredFlags.forEach(f => this.selectedFlags.add(f.id));
    } else {
      this.filteredFlags.forEach(f => this.selectedFlags.delete(f.id));
    }
  }

  allFilteredSelected(): boolean {
    return this.filteredFlags.length > 0 && 
           this.filteredFlags.every(f => this.selectedFlags.has(f.id));
  }

  someFilteredSelected(): boolean {
    return this.filteredFlags.some(f => this.selectedFlags.has(f.id));
  }

  // Individual flag actions
  toggleFlagDetails(flagId: number): void {
    if (this.expandedFlags.has(flagId)) {
      this.expandedFlags.delete(flagId);
    } else {
      this.expandedFlags.add(flagId);
    }
  }

  approveFlag(flagId: number): void {
    const comments = prompt('Enter approval comments (optional):');
    if (comments === null) return; // User cancelled

    this.actionLoading.add(flagId);
    this.clientManagementService.approveClientFlag(flagId, this.adminName, comments || undefined).subscribe({
      next: (success) => {
        if (success) {
          const flag = this.reviewFlags.find(f => f.id === flagId);
          if (flag) {
            flag.status = 'Approved';
            flag.reviewedBy = this.adminName;
            flag.reviewDate = new Date().toISOString();
            flag.adminComments = comments || 'Approved by admin';
          }
          this.applyFilters();
        }
        this.actionLoading.delete(flagId);
      },
      error: (error) => {
        console.error('Error approving flag:', error);
        this.actionLoading.delete(flagId);
      }
    });
  }

  rejectFlag(flagId: number): void {
    const comments = prompt('Enter rejection reason:');
    if (!comments) return;

    this.actionLoading.add(flagId);
    this.clientManagementService.rejectClientFlag(flagId, this.adminName, comments).subscribe({
      next: (success) => {
        if (success) {
          const flag = this.reviewFlags.find(f => f.id === flagId);
          if (flag) {
            flag.status = 'Rejected';
            flag.reviewedBy = this.adminName;
            flag.reviewDate = new Date().toISOString();
            flag.adminComments = comments;
          }
          this.applyFilters();
        }
        this.actionLoading.delete(flagId);
      },
      error: (error) => {
        console.error('Error rejecting flag:', error);
        this.actionLoading.delete(flagId);
      }
    });
  }

  banClient(clientId: number, flagId?: number): void {
    const reason = prompt('Enter ban reason:');
    if (!reason) return;

    const comments = prompt('Enter additional comments (optional):');
    if (comments === null) return; // User cancelled

    if (flagId) {
      this.actionLoading.add(flagId);
    }

    this.clientManagementService.banClient(clientId, reason, this.adminName, comments || undefined).subscribe({
      next: (success) => {
        if (success) {
          // Update all flags for this client to banned status
          this.reviewFlags.forEach(flag => {
            if (flag.clientId === clientId) {
              flag.status = 'Banned';
              flag.reviewedBy = this.adminName;
              flag.reviewDate = new Date().toISOString();
              flag.adminComments = comments || reason;
            }
          });
          this.applyFilters();
        }
        if (flagId) {
          this.actionLoading.delete(flagId);
        }
      },
      error: (error) => {
        console.error('Error banning client:', error);
        if (flagId) {
          this.actionLoading.delete(flagId);
        }
      }
    });
  }

  unbanClient(clientId: number): void {
    if (!confirm('Are you sure you want to unban this client? This will allow them to make appointments again.')) {
      return;
    }

    this.loading = true;
    this.clientManagementService.unbanClient(clientId).subscribe({
      next: (response) => {
        if (response) {
          // Update flags for this client
          this.reviewFlags.forEach(flag => {
            if (flag.clientId === clientId && flag.status === 'Banned') {
              flag.status = 'Approved';
              flag.reviewedBy = this.adminName;
              flag.reviewDate = new Date().toISOString();
              flag.adminComments = 'Ban removed by administrator';
            }
          });
          this.applyFilters();
          alert(response.message);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error unbanning client:', error);
        this.loading = false;
        alert('Error unbanning client. Please try again.');
      }
    });
  }

  // Bulk actions
  bulkApprove(): void {
    if (this.selectedFlags.size === 0) return;
    
    const comments = prompt('Enter approval comments for selected flags (optional):');
    if (comments === null) return;

    this.loading = true;
    const promises = Array.from(this.selectedFlags).map(flagId => 
      this.clientManagementService.approveClientFlag(flagId, this.adminName, comments || undefined).toPromise()
    );

    Promise.all(promises).then(() => {
      this.selectedFlags.forEach(flagId => {
        const flag = this.reviewFlags.find(f => f.id === flagId);
        if (flag) {
          flag.status = 'Approved';
          flag.reviewedBy = this.adminName;
          flag.reviewDate = new Date().toISOString();
          flag.adminComments = comments || 'Approved by admin';
        }
      });
      this.selectedFlags.clear();
      this.applyFilters();
      this.loading = false;
    }).catch(error => {
      console.error('Error bulk approving flags:', error);
      this.loading = false;
    });
  }

  bulkReject(): void {
    if (this.selectedFlags.size === 0) return;
    
    const comments = prompt('Enter rejection reason for selected flags:');
    if (!comments) return;

    this.loading = true;
    const promises = Array.from(this.selectedFlags).map(flagId => 
      this.clientManagementService.rejectClientFlag(flagId, this.adminName, comments).toPromise()
    );

    Promise.all(promises).then(() => {
      this.selectedFlags.forEach(flagId => {
        const flag = this.reviewFlags.find(f => f.id === flagId);
        if (flag) {
          flag.status = 'Rejected';
          flag.reviewedBy = this.adminName;
          flag.reviewDate = new Date().toISOString();
          flag.adminComments = comments;
        }
      });
      this.selectedFlags.clear();
      this.applyFilters();
      this.loading = false;
    }).catch(error => {
      console.error('Error bulk rejecting flags:', error);
      this.loading = false;
    });
  }

  // Utility methods
  getPendingCount(): number {
    return this.reviewFlags.filter(f => f.status === 'Pending').length;
  }

  getBannedClientsCount(): number {
    const bannedClientIds = new Set();
    this.reviewFlags.forEach(f => {
      if (f.status === 'Banned') {
        bannedClientIds.add(f.clientId);
      }
    });
    return bannedClientIds.size;
  }

  getClientFlags(clientId: number): ClientReviewFlag[] {
    return this.reviewFlags.filter(f => f.clientId === clientId);
  }

  isClientBanned(clientId: number): boolean {
    return this.getClientFlags(clientId).some(f => f.status === 'Banned');
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
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

  trackByFlag(index: number, flag: ClientReviewFlag): number {
    return flag.id;
  }

  getNoFlagsMessage(): string {
    if (this.searchTerm || this.statusFilter || this.clientFilter) {
      return 'No flags match your current filters.';
    }
    if (this.activeTab === 'pending') {
      return 'No flags are currently pending review.';
    }
    if (this.activeTab === 'banned') {
      return 'No clients are currently banned.';
    }
    return 'No client review flags have been created yet.';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
