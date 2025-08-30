import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import {
  GalleryImage,
  GalleryImageResponse,
  CreateGalleryImageRequest,
  UpdateGalleryImageRequest,
  GalleryCategory,
  UploadImageResponse
} from '../../models/gallery.models';

@Component({
  selector: 'app-admin-gallery',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-gallery.component.html',
  styleUrls: ['./admin-gallery.component.css']
})
export class AdminGalleryComponent implements OnInit {
  // Gallery data
  images: GalleryImage[] = [];
  categories: GalleryCategory[] = [];
  filteredImages: GalleryImage[] = [];
  
  // UI State
  loading = false;
  uploading = false;
  selectedCategory = 'all';
  selectedImages: Set<number> = new Set();
  
  // Edit/Create modes
  editMode = false;
  createMode = false;
  editingImage: GalleryImage | null = null;
  
  // Form data
  imageForm: CreateGalleryImageRequest = {
    src: '',
    alt: '',
    category: 'facials',
    title: '',
    description: '',
    isActive: true,
    sortOrder: 0
  };
  
  // Upload data
  selectedFile: File | null = null;
  uploadProgress = 0;
  
  // Messages
  successMessage = '';
  errorMessage = '';
  
  // Available categories for dropdown
  availableCategories = [
    { id: 'facials', name: 'Facials' },
    { id: 'body', name: 'Body Treatments' },
    { id: 'waxing', name: 'Waxing' },
    { id: 'makeup', name: 'Makeup' },
    { id: 'before-after', name: 'Before & After' },
    { id: 'products', name: 'Products' },
    { id: 'studio', name: 'Studio' }
  ];

  // Getter methods
  get activeImagesCount(): number {
    return this.images.filter(img => img.isActive).length;
  }

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/admin']);
      return;
    }
    
    this.loadGalleryData();
  }

  // Data loading methods
  loadGalleryData(): void {
    this.loading = true;
    this.clearMessages();
    
    this.apiService.getGalleryImages().subscribe({
      next: (response: GalleryImageResponse) => {
        this.images = response.data;
        this.filteredImages = this.images;
        this.updateCategoryCounts();
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading gallery:', error);
        this.errorMessage = 'Failed to load gallery images. Please try again.';
        this.loading = false;
      }
    });
  }

  updateCategoryCounts(): void {
    const categoryCounts = this.availableCategories.map(cat => ({
      id: cat.id,
      name: cat.name,
      count: this.images.filter(img => img.category === cat.id).length
    }));
    
    this.categories = [
      { id: 'all', name: 'All', count: this.images.length },
      ...categoryCounts
    ];
  }

  // Filter methods
  filterByCategory(categoryId: string): void {
    this.selectedCategory = categoryId;
    this.clearSelection();
    
    if (categoryId === 'all') {
      this.filteredImages = this.images;
    } else {
      this.filteredImages = this.images.filter(img => img.category === categoryId);
    }
  }

  // Selection methods
  toggleImageSelection(imageId: number): void {
    if (this.selectedImages.has(imageId)) {
      this.selectedImages.delete(imageId);
    } else {
      this.selectedImages.add(imageId);
    }
  }

  isImageSelected(imageId: number): boolean {
    return this.selectedImages.has(imageId);
  }

  selectAllVisible(): void {
    this.filteredImages.forEach(img => this.selectedImages.add(img.id));
  }

  clearSelection(): void {
    this.selectedImages.clear();
  }

  // File upload methods
  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.errorMessage = 'Please select a valid image file.';
        return;
      }
      
      // Validate file size (5MB limit)
      if (file.size > 5 * 1024 * 1024) {
        this.errorMessage = 'File size must be less than 5MB.';
        return;
      }
      
      this.selectedFile = file;
      this.clearMessages();
      
      // Clear URL field since we'll upload with metadata in one step
      this.imageForm.src = '';
      this.successMessage = `File selected: ${file.name}. Complete the form and save to upload and create gallery item.`;
    }
  }

  // CRUD operations
  startCreate(): void {
    this.createMode = true;
    this.editMode = false;
    this.editingImage = null;
    this.resetForm();
    this.clearMessages();
  }

  startEdit(image: GalleryImage): void {
    this.editMode = true;
    this.createMode = false;
    this.editingImage = image;
    this.imageForm = {
      src: image.src,
      alt: image.alt,
      category: image.category,
      title: image.title || '',
      description: image.description || '',
      isActive: image.isActive,
      sortOrder: image.sortOrder
    };
    this.clearMessages();
  }

  saveImage(): void {
    if (!this.validateForm()) {
      return;
    }
    
    this.loading = true;
    this.clearMessages();
    
    if (this.createMode) {
      // Check if we have a file to upload (new one-step approach)
      if (this.selectedFile) {
        console.log('fart')
        this.createImageWithFileUpload();
      } else {
        console.log('fart2')
        // Fallback to metadata-only approach (for manual URL entry)
        this.apiService.createGalleryImage(this.imageForm).subscribe({
          next: (image: GalleryImage) => {
            this.images.push(image);
            this.filterByCategory(this.selectedCategory);
            this.updateCategoryCounts();
            this.successMessage = 'Image created successfully!';
            this.cancelEdit();
            this.loading = false;
          },
          error: (error: any) => {
            console.error('Create error:', error);
            this.errorMessage = 'Failed to create image. Please try again.';
            this.loading = false;
          }
        });
      }
    } else if (this.editMode && this.editingImage) {
      this.apiService.updateGalleryImage(this.editingImage.id, this.imageForm).subscribe({
        next: (updatedImage: GalleryImage) => {
          const index = this.images.findIndex(img => img.id === updatedImage.id);
          if (index !== -1) {
            this.images[index] = updatedImage;
            this.filterByCategory(this.selectedCategory);
            this.updateCategoryCounts();
          }
          this.successMessage = 'Image updated successfully!';
          this.cancelEdit();
          this.loading = false;
        },
        error: (error: any) => {
          console.error('Update error:', error);
          this.errorMessage = 'Failed to update image. Please try again.';
          this.loading = false;
        }
      });
    }
  }

  // New method: Create image with file upload in one step
  private createImageWithFileUpload(): void {
    if (!this.selectedFile) {
      this.errorMessage = 'No file selected for upload.';
      this.loading = false;
      return;
    }

    // Create FormData with file + metadata
    const formData = new FormData();
    formData.append('image', this.selectedFile);
    formData.append('alt', this.imageForm.alt);
    formData.append('category', this.imageForm.category);
    formData.append('title', this.imageForm.title || '');
    formData.append('description', this.imageForm.description || '');
    formData.append('isActive', (this.imageForm.isActive ?? true).toString());
    formData.append('sortOrder', (this.imageForm.sortOrder ?? 0).toString());

    // Send FormData directly to createGalleryImage endpoint
    // The backend will detect multipart form data and handle both upload + database creation
    this.apiService.createGalleryImageWithFile(formData).subscribe({
      next: (image: GalleryImage) => {
        this.images.push(image);
        this.filterByCategory(this.selectedCategory);
        this.updateCategoryCounts();
        this.successMessage = 'Image uploaded and created successfully!';
        this.cancelEdit();
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Create with file error:', error);
        this.errorMessage = 'Failed to upload and create image. Please try again.';
        this.loading = false;
      }
    });
  }

  deleteImage(imageId: number): void {
    if (!confirm('Are you sure you want to delete this image? This action cannot be undone.')) {
      return;
    }
    
    this.loading = true;
    this.clearMessages();
    
    this.apiService.deleteGalleryImage(imageId).subscribe({
      next: () => {
        this.images = this.images.filter(img => img.id !== imageId);
        this.filterByCategory(this.selectedCategory);
        this.updateCategoryCounts();
        this.selectedImages.delete(imageId);
        this.successMessage = 'Image deleted successfully!';
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Delete error:', error);
        this.errorMessage = 'Failed to delete image. Please try again.';
        this.loading = false;
      }
    });
  }

  deleteSelectedImages(): void {
    if (this.selectedImages.size === 0) {
      this.errorMessage = 'Please select images to delete.';
      return;
    }
    
    const count = this.selectedImages.size;
    if (!confirm(`Are you sure you want to delete ${count} selected image(s)? This action cannot be undone.`)) {
      return;
    }
    
    this.loading = true;
    this.clearMessages();
    
    const deletePromises = Array.from(this.selectedImages).map(imageId =>
      this.apiService.deleteGalleryImage(imageId).toPromise()
    );
    
    Promise.all(deletePromises).then(() => {
      this.images = this.images.filter(img => !this.selectedImages.has(img.id));
      this.filterByCategory(this.selectedCategory);
      this.updateCategoryCounts();
      this.successMessage = `${count} image(s) deleted successfully!`;
      this.clearSelection();
      this.loading = false;
    }).catch((error: any) => {
      console.error('Bulk delete error:', error);
      this.errorMessage = 'Failed to delete some images. Please try again.';
      this.loading = false;
    });
  }

  toggleImageStatus(image: GalleryImage): void {
    const updateData: UpdateGalleryImageRequest = {
      isActive: !image.isActive
    };
    
    this.apiService.updateGalleryImage(image.id, updateData).subscribe({
      next: (updatedImage: GalleryImage) => {
        const index = this.images.findIndex(img => img.id === updatedImage.id);
        if (index !== -1) {
          this.images[index] = updatedImage;
          this.filterByCategory(this.selectedCategory);
        }
        this.successMessage = `Image ${updatedImage.isActive ? 'activated' : 'deactivated'} successfully!`;
      },
      error: (error: any) => {
        console.error('Status toggle error:', error);
        this.errorMessage = 'Failed to update image status. Please try again.';
      }
    });
  }

  // Form methods
  validateForm(): boolean {
    // For create mode, require either a selected file OR a manual URL
    if (this.createMode) {
      if (!this.selectedFile && !this.imageForm.src.trim()) {
        this.errorMessage = 'Please either select a file to upload or provide an image URL.';
        return false;
      }
    } else {
      // For edit mode, require URL
      if (!this.imageForm.src.trim()) {
        this.errorMessage = 'Image URL is required.';
        return false;
      }
    }
    
    if (!this.imageForm.alt.trim()) {
      this.errorMessage = 'Alt text is required.';
      return false;
    }
    
    if (!this.imageForm.category.trim()) {
      this.errorMessage = 'Category is required.';
      return false;
    }
    
    return true;
  }

  resetForm(): void {
    this.imageForm = {
      src: '',
      alt: '',
      category: 'facials',
      title: '',
      description: '',
      isActive: true,
      sortOrder: 0
    };
    this.selectedFile = null;
    
    // Reset file input
    const fileInput = document.getElementById('imageFile') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
  }

  cancelEdit(): void {
    this.editMode = false;
    this.createMode = false;
    this.editingImage = null;
    this.selectedFile = null;
    
    // Reset file input
    const fileInput = document.getElementById('imageFile') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
    
    this.resetForm();
    this.clearMessages();
  }

  // Utility methods
  clearMessages(): void {
    this.successMessage = '';
    this.errorMessage = '';
  }

  getCategoryName(categoryId: string): string {
    const category = this.availableCategories.find(cat => cat.id === categoryId);
    return category ? category.name : categoryId;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  // Drag and drop for reordering (basic implementation)
  onDragStart(event: DragEvent, imageId: number): void {
    event.dataTransfer?.setData('text/plain', imageId.toString());
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onDrop(event: DragEvent, targetImageId: number): void {
    event.preventDefault();
    const draggedImageId = parseInt(event.dataTransfer?.getData('text/plain') || '0');
    
    if (draggedImageId && draggedImageId !== targetImageId) {
      // Simple reordering logic - this could be enhanced
      const draggedIndex = this.filteredImages.findIndex(img => img.id === draggedImageId);
      const targetIndex = this.filteredImages.findIndex(img => img.id === targetImageId);
      
      if (draggedIndex !== -1 && targetIndex !== -1) {
        // Swap positions
        const draggedImage = this.filteredImages[draggedIndex];
        const targetImage = this.filteredImages[targetIndex];
        
        [draggedImage.sortOrder, targetImage.sortOrder] = [targetImage.sortOrder, draggedImage.sortOrder];
        
        // Update order on server
        const imageIds = this.filteredImages
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map(img => img.id);
        
        this.apiService.updateGalleryImageOrder(imageIds).subscribe({
          next: () => {
            this.loadGalleryData(); // Reload to get updated order
            this.successMessage = 'Image order updated successfully!';
          },
          error: (error: any) => {
            console.error('Reorder error:', error);
            this.errorMessage = 'Failed to update image order. Please try again.';
          }
        });
      }
    }
  }

  trackByImageId(index: number, image: GalleryImage): number {
    return image.id;
  }
}
