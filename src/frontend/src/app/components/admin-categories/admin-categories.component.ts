import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CategoryManagementService } from '../../services/category-management.service';
import { ServiceManagementService } from '../../services/service-management.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest, CategoryServiceCount } from '../../models/api-models';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './admin-categories.component.html',
  styleUrls: ['./admin-categories.component.css']
})
export class AdminCategoriesComponent implements OnInit {
  // Data
  categories: Category[] = [];
  filteredCategories: Category[] = [];
  serviceCounts: CategoryServiceCount[] = [];
  
  // UI State
  loading = false;
  isEditMode = false;
  showAddForm = false;
  searchQuery = '';
  
  // Forms
  categoryForm: FormGroup;
  editingCategoryId: number | null = null;
  
  // Messages
  successMessage = '';
  errorMessage = '';

  constructor(
    private authService: AuthService,
    private categoryManagementService: CategoryManagementService,
    private serviceManagementService: ServiceManagementService,
    private router: Router,
    private fb: FormBuilder
  ) {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(255)]]
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

    // Load categories with service counts using the optimized method
    this.categoryManagementService.getCategoriesWithServiceCounts().subscribe({
      next: (data) => {
        this.categories = data.categories || [];
        this.serviceCounts = data.serviceCounts || [];
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading data:', error);
        this.errorMessage = 'Failed to load categories and service counts.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.categories];

    // Apply search filter
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(category => 
        category.name.toLowerCase().includes(query)
      );
    }

    this.filteredCategories = filtered.sort((a, b) => a.name.localeCompare(b.name));
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.applyFilters();
  }

  showAddCategoryForm(): void {
    this.showAddForm = true;
    this.isEditMode = false;
    this.editingCategoryId = null;
    this.categoryForm.reset();
    this.clearMessages();
  }

  editCategory(category: Category): void {
    this.showAddForm = true;
    this.isEditMode = true;
    this.editingCategoryId = category.id;
    
    this.categoryForm.patchValue({
      name: category.name
    });
    
    this.clearMessages();
  }

  cancelForm(): void {
    this.showAddForm = false;
    this.isEditMode = false;
    this.editingCategoryId = null;
    this.categoryForm.reset();
    this.clearMessages();
  }

  submitForm(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const formData = this.categoryForm.value;
    const categoryName = formData.name.trim();

    // Check for duplicate names
    if (this.categoryManagementService.categoryNameExists(categoryName, this.editingCategoryId || undefined)) {
      this.errorMessage = 'A category with this name already exists.';
      return;
    }

    // Prepare the request data
    const categoryData = {
      name: categoryName
    };

    this.loading = true;
    this.clearMessages();

    if (this.isEditMode && this.editingCategoryId) {
      // Update existing category
      this.categoryManagementService.updateCategory(this.editingCategoryId, categoryData as UpdateCategoryRequest).subscribe({
        next: (response) => {
          this.successMessage = 'Category updated successfully!';
          this.loadData();
          this.cancelForm();
        },
        error: (error) => {
          console.error('Error updating category:', error);
          this.errorMessage = error.error?.message || 'Failed to update category.';
          this.loading = false;
        }
      });
    } else {
      // Create new category
      this.categoryManagementService.createCategory(categoryData as CreateCategoryRequest).subscribe({
        next: (response) => {
          this.successMessage = 'Category created successfully!';
          this.loadData();
          this.cancelForm();
        },
        error: (error) => {
          console.error('Error creating category:', error);
          this.errorMessage = error.error?.message || 'Failed to create category.';
          this.loading = false;
        }
      });
    }
  }

  deleteCategory(category: Category): void {
    if (!confirm(`Are you sure you want to delete "${category.name}"? This action cannot be undone.`)) {
      return;
    }

    this.loading = true;
    this.clearMessages();

    this.categoryManagementService.deleteCategory(category.id).subscribe({
      next: (response) => {
        this.successMessage = `Category "${category.name}" deleted successfully!`;
        this.loadData();
      },
      error: (error) => {
        console.error('Error deleting category:', error);
        this.errorMessage = error.error?.message || 'Failed to delete category.';
        this.loading = false;
      }
    });
  }

  getServiceCount(categoryId: number): number {
    const serviceCount = this.serviceCounts.find(sc => sc.categoryId === categoryId);
    return serviceCount ? serviceCount.serviceCount : 0;
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
