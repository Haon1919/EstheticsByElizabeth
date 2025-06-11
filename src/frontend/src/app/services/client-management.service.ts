import { Injectable } from '@angular/core';
import { Observable, of, map, catchError } from 'rxjs';
import { ApiService } from './api.service';
import { ClientReviewFlag, UpdateReviewFlagRequest, BanClientRequest, ClientBanResponse } from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class ClientManagementService {

  constructor(private apiService: ApiService) { }

  /**
   * Get all client review flags with optional status filtering
   */
  getClientReviewFlags(status?: string): Observable<ClientReviewFlag[]> {
    return this.apiService.getClientReviewFlags(status).pipe(
      catchError(error => {
        console.error('Error fetching client review flags:', error);
        return of([]);
      })
    );
  }

  /**
   * Get a specific client review flag
   */
  getClientReviewFlag(flagId: number): Observable<ClientReviewFlag | null> {
    return this.apiService.getClientReviewFlag(flagId).pipe(
      catchError(error => {
        console.error(`Error fetching client review flag ${flagId}:`, error);
        return of(null);
      })
    );
  }

  /**
   * Update a client review flag status
   */
  updateClientReviewFlag(flagId: number, updateData: UpdateReviewFlagRequest): Observable<boolean> {
    return this.apiService.updateClientReviewFlag(flagId, updateData).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error updating client review flag ${flagId}:`, error);
        return of(false);
      })
    );
  }

  /**
   * Approve a client review flag
   */
  approveClientFlag(flagId: number, adminName: string, comments?: string): Observable<boolean> {
    const updateData: UpdateReviewFlagRequest = {
      status: 'Approved',
      reviewedBy: adminName,
      adminComments: comments || 'Approved by admin'
    };
    return this.updateClientReviewFlag(flagId, updateData);
  }

  /**
   * Reject a client review flag
   */
  rejectClientFlag(flagId: number, adminName: string, comments?: string): Observable<boolean> {
    const updateData: UpdateReviewFlagRequest = {
      status: 'Rejected',
      reviewedBy: adminName,
      adminComments: comments || 'Rejected by admin'
    };
    return this.updateClientReviewFlag(flagId, updateData);
  }

  /**
   * Ban a client
   */
  banClient(clientId: number, reason: string, adminName: string, comments?: string): Observable<boolean> {
    const banData: BanClientRequest = {
      isBanned: true,
      reason: reason,
      adminName: adminName,
      comments: comments
    };
    
    return this.apiService.banClient(clientId, banData).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error banning client ${clientId}:`, error);
        return of(false);
      })
    );
  }

  /**
   * Unban a client
   */
  unbanClient(clientId: number): Observable<ClientBanResponse | null> {
    return this.apiService.unbanClient(clientId).pipe(
      catchError(error => {
        console.error(`Error unbanning client ${clientId}:`, error);
        return of(null);
      })
    );
  }

  /**
   * Get pending reviews for a specific client
   */
  getClientPendingReviews(clientId: number): Observable<ClientReviewFlag[]> {
    return this.apiService.getClientPendingReviews(clientId).pipe(
      catchError(error => {
        console.error(`Error fetching pending reviews for client ${clientId}:`, error);
        return of([]);
      })
    );
  }

  /**
   * Check if a client is currently banned
   */
  isClientBanned(flags: ClientReviewFlag[]): boolean {
    return flags.some(flag => flag.status === 'Banned');
  }

  /**
   * Check if a client has pending reviews
   */
  hasClientPendingReviews(flags: ClientReviewFlag[]): boolean {
    return flags.some(flag => flag.status === 'Pending');
  }

  /**
   * Get the latest flag for a client
   */
  getLatestFlag(flags: ClientReviewFlag[]): ClientReviewFlag | null {
    if (flags.length === 0) return null;
    
    return flags.reduce((latest, current) => {
      return new Date(current.flagDate) > new Date(latest.flagDate) ? current : latest;
    });
  }
}
