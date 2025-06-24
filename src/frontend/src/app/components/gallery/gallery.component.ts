import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { GalleryImage } from '../../models/api-models';

interface Category {
  id: string;
  name: string;
}

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './gallery.component.html',
  styleUrls: ['./gallery.component.css']
})
export class GalleryComponent implements OnInit {
  images: GalleryImage[] = [];
  categories: Category[] = [{ id: 'all', name: 'All' }];
  selectedCategory = 'all';
  loading = false;
  error = '';

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadGalleryImages();
  }

  loadGalleryImages(): void {
    this.loading = true;
    this.error = '';

    this.apiService.getPublicGalleryImages().subscribe({
      next: (response: any) => {
        if (response.success) {
          this.images = response.data;
          this.updateCategories(response.categories);
        } else {
          this.error = 'Failed to load gallery images';
        }
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading gallery images:', error);
        this.error = 'Failed to load gallery images. Please try again later.';
        this.loading = false;
      }
    });
  }

  updateCategories(apiCategories: string[]): void {
    // Create category objects with proper display names
    const categoryDisplayNames: { [key: string]: string } = {
      'facials': 'Facials',
      'body': 'Body Treatments',
      'waxing': 'Waxing',
      'makeup': 'Makeup',
      'before-after': 'Before & After',
      'products': 'Products',
      'studio': 'Studio'
    };

    this.categories = [
      { id: 'all', name: 'All' },
      ...apiCategories.map(categoryId => ({
        id: categoryId,
        name: categoryDisplayNames[categoryId] || categoryId.charAt(0).toUpperCase() + categoryId.slice(1)
      }))
    ];
  }

  filterImages(category: string): void {
    this.selectedCategory = category;
    this.loading = true;
    this.error = '';

    this.apiService.getPublicGalleryImages(category).subscribe({
      next: (response: any) => {
        if (response.success) {
          this.images = response.data;
        } else {
          this.error = 'Failed to filter gallery images';
        }
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error filtering gallery images:', error);
        this.error = 'Failed to filter gallery images. Please try again later.';
        this.loading = false;
      }
    });
  }

  get filteredImages() {
    // Since we're now filtering on the server side, just return all images
    return this.images;
  }

  trackByImageId(index: number, image: GalleryImage): number {
    return image.id;
  }

  getCategoryDisplayName(category: string): string {
    const categoryDisplayNames: { [key: string]: string } = {
      'facials': 'Facials',
      'body': 'Body Treatments',
      'waxing': 'Waxing',
      'makeup': 'Makeup',
      'before-after': 'Before & After',
      'products': 'Products',
      'studio': 'Studio'
    };

    return categoryDisplayNames[category] || category.charAt(0).toUpperCase() + category.slice(1);
  }

  onImageError(event: any): void {
    // Handle broken images by hiding them or showing a placeholder
    const imgElement = event.target as HTMLImageElement;
    imgElement.style.display = 'none';
    console.warn('Failed to load image:', imgElement.src);
  }
}