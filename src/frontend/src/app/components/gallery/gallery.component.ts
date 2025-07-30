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
        
        // Fallback to dummy data for GitHub Pages
        this.loadDummyGalleryData();
      }
    });
  }

  loadDummyGalleryData(): void {
    // Dummy gallery data for GitHub Pages display
    this.images = [
      {
        id: 1,
        src: 'https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=400&h=400&fit=crop&crop=face',
        alt: 'Facial Treatment - Before & After',
        category: 'facials',
        title: 'Facial Treatment - Before & After',
        description: 'Amazing transformation with our signature facial treatment',
        isActive: true,
        sortOrder: 1,
        uploadedAt: new Date().toISOString()
      },
      {
        id: 2,
        src: 'https://images.unsplash.com/photo-1616394584738-fc6e612e71b9?w=400&h=400&fit=crop&crop=face',
        alt: 'Glowing Skin Results',
        category: 'facials',
        title: 'Glowing Skin Results',
        description: 'Beautiful glowing skin after our customized facial',
        isActive: true,
        sortOrder: 2,
        uploadedAt: new Date().toISOString()
      },
      {
        id: 3,
        src: 'https://images.unsplash.com/photo-1560750588-73207b1ef5b8?w=400&h=400&fit=crop',
        alt: 'Professional Waxing Service',
        category: 'waxing',
        title: 'Professional Waxing Service',
        description: 'Smooth results from our professional waxing treatments',
        isActive: true,
        sortOrder: 3,
        uploadedAt: new Date().toISOString()
      },
      {
        id: 4,
        src: 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=400&h=400&fit=crop',
        alt: 'Relaxing Studio Environment',
        category: 'studio',
        title: 'Relaxing Studio Environment',
        description: 'Our peaceful and professional treatment room',
        isActive: true,
        sortOrder: 4,
        uploadedAt: new Date().toISOString()
      },
      {
        id: 5,
        src: 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop',
        alt: 'Premium Skincare Products',
        category: 'products',
        title: 'Premium Skincare Products',
        description: 'High-quality products we use in our treatments',
        isActive: true,
        sortOrder: 5,
        uploadedAt: new Date().toISOString()
      },
      {
        id: 6,
        src: 'https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=400&h=400&fit=crop',
        alt: 'Body Treatment Session',
        category: 'body',
        title: 'Body Treatment Session',
        description: 'Luxurious body treatments for complete relaxation',
        isActive: true,
        sortOrder: 6,
        uploadedAt: new Date().toISOString()
      }
    ];

    const categories = [...new Set(this.images.map(img => img.category))];
    this.updateCategories(categories);
    this.loading = false;
    this.error = '';
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