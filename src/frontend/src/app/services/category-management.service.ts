import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, map, catchError, tap } from 'rxjs';
import { ApiService } from './api.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest, CategoryServiceCount } from '../models/services.models';

@Injectable({
  providedIn: 'root'
})
export class CategoryManagementService {
  private categoriesSubject = new BehaviorSubject<Category[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(false);

  public categories$ = this.categoriesSubject.asObservable();
  public loading$ = this.loadingSubject.asObservable();

  constructor(private apiService: ApiService) { }

  /**
   * Load all categories from the API
   */
  loadCategories(): Observable<Category[]> {
    this.loadingSubject.next(true);
    
    return this.apiService.getCategories().pipe(
      tap(categories => {
        this.categoriesSubject.next(categories);
        this.loadingSubject.next(false);
      }),
      catchError(error => {
        this.loadingSubject.next(false);
        throw error;
      })
    );
  }

  /**
   * Create a new category
   */
  createCategory(categoryData: CreateCategoryRequest): Observable<Category> {
    this.loadingSubject.next(true);
    
    return this.apiService.createCategory(categoryData).pipe(
      tap(newCategory => {
        // Add new category to the list
        const currentCategories = this.categoriesSubject.value;
        const updatedCategories = [...currentCategories, newCategory].sort((a, b) => a.name.localeCompare(b.name));
        this.categoriesSubject.next(updatedCategories);
        this.loadingSubject.next(false);
      }),
      catchError(error => {
        this.loadingSubject.next(false);
        throw error;
      })
    );
  }

  /**
   * Update an existing category
   */
  updateCategory(categoryId: number, categoryData: UpdateCategoryRequest): Observable<Category> {
    this.loadingSubject.next(true);
    
    return this.apiService.updateCategory(categoryId, categoryData).pipe(
      tap(updatedCategory => {
        // Update category in the list
        const currentCategories = this.categoriesSubject.value;
        const updatedCategories = currentCategories.map(cat => 
          cat.id === categoryId ? updatedCategory : cat
        ).sort((a, b) => a.name.localeCompare(b.name));
        this.categoriesSubject.next(updatedCategories);
        this.loadingSubject.next(false);
      }),
      catchError(error => {
        this.loadingSubject.next(false);
        throw error;
      })
    );
  }

  /**
   * Delete a category
   */
  deleteCategory(categoryId: number): Observable<any> {
    this.loadingSubject.next(true);
    
    return this.apiService.deleteCategory(categoryId).pipe(
      tap(() => {
        // Remove category from the list
        const currentCategories = this.categoriesSubject.value;
        const updatedCategories = currentCategories.filter(cat => cat.id !== categoryId);
        this.categoriesSubject.next(updatedCategories);
        this.loadingSubject.next(false);
      }),
      catchError(error => {
        this.loadingSubject.next(false);
        throw error;
      })
    );
  }

  /**
   * Get a specific category by ID
   */
  getCategoryById(id: number): Category | undefined {
    return this.categoriesSubject.value.find(category => category.id === id);
  }

  /**
   * Get current categories without subscribing
   */
  getCurrentCategories(): Category[] {
    return this.categoriesSubject.value;
  }

  /**
   * Check if categories are currently loading
   */
  isLoading(): boolean {
    return this.loadingSubject.value;
  }

  /**
   * Refresh categories from the API
   */
  refresh(): void {
    this.loadCategories().subscribe();
  }

  /**
   * Check if a category name already exists (case-insensitive)
   */
  categoryNameExists(name: string, excludeId?: number): boolean {
    const normalizedName = name.toLowerCase().trim();
    return this.categoriesSubject.value.some(cat => 
      cat.name.toLowerCase() === normalizedName && cat.id !== excludeId
    );
  }

  /**
   * Get categories sorted by name
   */
  getCategoriesSorted(): Category[] {
    return [...this.categoriesSubject.value].sort((a, b) => a.name.localeCompare(b.name));
  }

  /**
   * Get service counts by category
   */
  getServiceCountsByCategory(): Observable<CategoryServiceCount[]> {
    return this.apiService.getServiceCountByCategory();
  }

  /**
   * Get categories with their associated service counts in a single optimized call
   */
  getCategoriesWithServiceCounts(): Observable<{categories: Category[], serviceCounts: CategoryServiceCount[]}> {
    // Load both categories and service counts simultaneously for better performance
    return new Observable(observer => {
      Promise.all([
        this.loadCategories().toPromise(),
        this.getServiceCountsByCategory().toPromise()
      ]).then(([categories, serviceCounts]) => {
        observer.next({
          categories: categories || [],
          serviceCounts: serviceCounts || []
        });
        observer.complete();
      }).catch(error => {
        observer.error(error);
      });
    });
  }
}
