import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ServiceManagementService } from '../../services/service-management.service';
import { ApiService } from '../../services/api.service';
import { Service, Category, CreateServiceRequest, UpdateServiceRequest } from '../../models/api-models';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './admin-services.component.html',
  styleUrls: ['./admin-services.component.css']
})
export class AdminServicesComponent implements OnInit {
  // Data
  services: Service[] = [];
  categories: Category[] = [];
  filteredServices: Service[] = [];
  
  // UI State
  loading = false;
  isEditMode = false;
  showAddForm = false;
  selectedCategoryFilter: number | null = null;
  searchQuery = '';
  
  // Forms
  serviceForm: FormGroup;
  editingServiceId: number | null = null;
  
  // Messages
  successMessage = '';
  errorMessage = '';

  constructor(
    private authService: AuthService,
    private serviceManagementService: ServiceManagementService,
    private apiService: ApiService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.serviceForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(255)]],
      description: ['', [Validators.maxLength(2000)]],
      price: ['', [Validators.min(0), Validators.max(9999.99)]],
      duration: ['', [Validators.min(1), Validators.max(480)]],
      categoryId: ['', [Validators.required]],
      website: ['', [Validators.pattern(/^https?:\/\/.+/)]]
    });
  }

  ngOnInit(): void {
    // Check authentication
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/admin']);
      return;
    }

    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.clearMessages();

    // Load services and categories
    Promise.all([
      this.serviceManagementService.loadServices().toPromise(),
      this.serviceManagementService.loadCategories().toPromise()
    ]).then(([services, categories]) => {
      this.services = services || [];
      this.categories = categories || [];
      this.applyFilters();
      this.loading = false;
    }).catch((error) => {
      console.error('Error loading data:', error);
      this.errorMessage = 'Failed to load services and categories.';
      this.loading = false;
    });
  }

  applyFilters(): void {
    let filtered = [...this.services];

    // Apply category filter
    if (this.selectedCategoryFilter) {
      filtered = filtered.filter(service => service.category.id === this.selectedCategoryFilter);
    }

    // Apply search filter
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(service => 
        service.name.toLowerCase().includes(query) ||
        service.description?.toLowerCase().includes(query) ||
        service.category.name.toLowerCase().includes(query)
      );
    }

    this.filteredServices = filtered.sort((a, b) => {
      // Sort by category name first, then by service name
      if (a.category.name !== b.category.name) {
        return a.category.name.localeCompare(b.category.name);
      }
      return a.name.localeCompare(b.name);
    });
  }

  onCategoryFilterChange(): void {
    this.applyFilters();
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.selectedCategoryFilter = null;
    this.searchQuery = '';
    this.applyFilters();
  }

  showAddServiceForm(): void {
    this.showAddForm = true;
    this.isEditMode = false;
    this.editingServiceId = null;
    this.serviceForm.reset();
    this.clearMessages();
  }

  editService(service: Service): void {
    this.showAddForm = true;
    this.isEditMode = true;
    this.editingServiceId = service.id;
    
    this.serviceForm.patchValue({
      name: service.name,
      description: service.description || '',
      price: service.price || '',
      duration: service.duration || '',
      categoryId: service.category.id,
      website: service.website || ''
    });
    
    this.clearMessages();
  }

  cancelForm(): void {
    this.showAddForm = false;
    this.isEditMode = false;
    this.editingServiceId = null;
    this.serviceForm.reset();
    this.clearMessages();
  }

  submitForm(): void {
    if (this.serviceForm.invalid) {
      this.serviceForm.markAllAsTouched();
      return;
    }

    const formData = this.serviceForm.value;
    
    // Prepare the request data
    const serviceData = {
      name: formData.name.trim(),
      description: formData.description?.trim() || null,
      price: formData.price ? parseFloat(formData.price) : null,
      duration: formData.duration ? parseInt(formData.duration) : null,
      categoryId: parseInt(formData.categoryId),
      website: formData.website?.trim() || null
    };

    this.loading = true;
    this.clearMessages();

    if (this.isEditMode && this.editingServiceId) {
      // Update existing service
      this.apiService.updateService(this.editingServiceId, serviceData as UpdateServiceRequest).subscribe({
        next: (response) => {
          this.successMessage = 'Service updated successfully!';
          this.loadData();
          this.cancelForm();
        },
        error: (error) => {
          console.error('Error updating service:', error);
          this.errorMessage = error.error?.message || 'Failed to update service.';
          this.loading = false;
        }
      });
    } else {
      // Create new service
      this.apiService.createService(serviceData as CreateServiceRequest).subscribe({
        next: (response) => {
          this.successMessage = 'Service created successfully!';
          this.loadData();
          this.cancelForm();
        },
        error: (error) => {
          console.error('Error creating service:', error);
          this.errorMessage = error.error?.message || 'Failed to create service.';
          this.loading = false;
        }
      });
    }
  }

  deleteService(service: Service): void {
    if (!confirm(`Are you sure you want to delete "${service.name}"? This action cannot be undone.`)) {
      return;
    }

    this.loading = true;
    this.clearMessages();

    this.apiService.deleteService(service.id).subscribe({
      next: (response) => {
        this.successMessage = `Service "${service.name}" deleted successfully!`;
        this.loadData();
      },
      error: (error) => {
        console.error('Error deleting service:', error);
        this.errorMessage = error.error?.message || 'Failed to delete service.';
        this.loading = false;
      }
    });
  }

  getCategoryName(categoryId: number): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category ? category.name : 'Unknown Category';
  }

  formatPrice(price: number | null | undefined): string {
    if (price === null || price === undefined) {
      return 'No price set';
    }
    return `$${price.toFixed(2)}`;
  }

  formatDuration(duration: number | null | undefined): string {
    if (duration === null || duration === undefined) {
      return 'No duration set';
    }
    if (duration < 60) {
      return `${duration} min`;
    }
    const hours = Math.floor(duration / 60);
    const minutes = duration % 60;
    return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
  }

  getServicesByCategory(categoryId: number): Service[] {
    return this.filteredServices.filter(service => service.category.id === categoryId);
  }

  getUniqueCategories(): Category[] {
    const categoryIds = new Set(this.filteredServices.map(service => service.category.id));
    return this.categories.filter(category => categoryIds.has(category.id));
  }

  clearMessages(): void {
    this.successMessage = '';
    this.errorMessage = '';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
