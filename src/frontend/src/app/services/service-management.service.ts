import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { ApiService } from './api.service';
import { Service, Category } from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class ServiceManagementService {
  private servicesSubject = new BehaviorSubject<Service[]>([]);
  private categoriesSubject = new BehaviorSubject<Category[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(false);

  public services$ = this.servicesSubject.asObservable();
  public categories$ = this.categoriesSubject.asObservable();
  public loading$ = this.loadingSubject.asObservable();

  constructor(private apiService: ApiService) { }

  /**
   * Load all services from the API
   */
  loadServices(): Observable<Service[]> {
    this.loadingSubject.next(true);
    
    return this.apiService.getServices().pipe(
      tap(services => {
        this.servicesSubject.next(services);
        this.loadingSubject.next(false);
      })
    );
  }

  /**
   * Load all categories from the API
   */
  loadCategories(): Observable<Category[]> {
    this.loadingSubject.next(true);
    
    return this.apiService.getCategories().pipe(
      tap(categories => {
        this.categoriesSubject.next(categories);
        this.loadingSubject.next(false);
      })
    );
  }

  /**
   * Get services grouped by category
   */
  getServicesGroupedByCategory(): Observable<{category: Category, services: Service[]}[]> {
    return new Observable(observer => {
      // Load both services and categories if not already loaded
      const currentServices = this.servicesSubject.value;
      const currentCategories = this.categoriesSubject.value;

      if (currentServices.length === 0) {
        this.loadServices().subscribe();
      }
      
      if (currentCategories.length === 0) {
        this.loadCategories().subscribe();
      }

      // Wait for both to be loaded and group them
      this.services$.subscribe(services => {
        this.categories$.subscribe(categories => {
          const grouped = categories.map(category => ({
            category: category,
            services: services.filter(service => service.category.id === category.id)
          })).filter(group => group.services.length > 0); // Only include categories with services

          observer.next(grouped);
          observer.complete();
        });
      });
    });
  }

  /**
   * Get a specific service by ID
   */
  getServiceById(id: number): Service | undefined {
    return this.servicesSubject.value.find(service => service.id === id);
  }

  /**
   * Get a specific category by ID
   */
  getCategoryById(id: number): Category | undefined {
    return this.categoriesSubject.value.find(category => category.id === id);
  }

  /**
   * Get services by category ID
   */
  getServicesByCategoryId(categoryId: number): Service[] {
    return this.servicesSubject.value.filter(service => service.category.id === categoryId);
  }

  /**
   * Check if services are currently loading
   */
  isLoading(): boolean {
    return this.loadingSubject.value;
  }

  /**
   * Get current services without subscribing
   */
  getCurrentServices(): Service[] {
    return this.servicesSubject.value;
  }

  /**
   * Get current categories without subscribing
   */
  getCurrentCategories(): Category[] {
    return this.categoriesSubject.value;
  }

  /**
   * Refresh services and categories from the API
   */
  refresh(): void {
    this.loadServices().subscribe();
    this.loadCategories().subscribe();
  }

  /**
   * Filter services by bookable status (services with prices)
   */
  getBookableServices(): Service[] {
    return this.servicesSubject.value.filter(service => service.price !== undefined && service.price !== null);
  }

  /**
   * Filter services by non-bookable status (services without prices, like product brands)
   */
  getNonBookableServices(): Service[] {
    return this.servicesSubject.value.filter(service => service.price === undefined || service.price === null);
  }
}
