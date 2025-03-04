import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gallery.component.html',
  styleUrls: ['./gallery.component.css']
})
export class GalleryComponent {
  images = [
    {
      id: 1,
      src: 'assets/images/gallery1.jpg',
      category: 'facials',
      alt: 'Facial treatment with client relaxing'
    },
    {
      id: 2,
      src: 'assets/images/gallery2.jpg',
      category: 'body',
      alt: 'Body treatment session'
    },
    {
      id: 3,
      src: 'assets/images/gallery3.jpg',
      category: 'makeup',
      alt: 'Professional makeup application'
    },
    {
      id: 4,
      src: 'assets/images/gallery4.jpg',
      category: 'facials',
      alt: 'Facial massage procedure'
    },
    {
      id: 5,
      src: 'assets/images/gallery5.jpg',
      category: 'waxing',
      alt: 'Waxing service being performed'
    },
    {
      id: 6,
      src: 'assets/images/gallery6.jpg',
      category: 'makeup',
      alt: 'Bridal makeup session'
    },
    {
      id: 7,
      src: 'assets/images/gallery7.jpg',
      category: 'body',
      alt: 'Relaxing body wrap treatment'
    },
    {
      id: 8,
      src: 'assets/images/gallery8.jpg',
      category: 'facials',
      alt: 'Advanced facial treatment'
    },
    {
      id: 9,
      src: 'assets/images/gallery9.jpg',
      category: 'waxing',
      alt: 'Eyebrow waxing procedure'
    },
    {
      id: 10,
      src: 'assets/images/gallery10.jpg',
      category: 'makeup',
      alt: 'Natural makeup look'
    },
    {
      id: 11,
      src: 'assets/images/gallery11.jpg',
      category: 'body',
      alt: 'Full body massage'
    },
    {
      id: 12,
      src: 'assets/images/gallery12.jpg',
      category: 'facials',
      alt: 'Facial cleansing treatment'
    }
  ];

  categories = [
    { id: 'all', name: 'All' },
    { id: 'facials', name: 'Facials' },
    { id: 'body', name: 'Body Treatments' },
    { id: 'waxing', name: 'Waxing' },
    { id: 'makeup', name: 'Makeup' }
  ];

  selectedCategory = 'all';

  filterImages(category: string): void {
    this.selectedCategory = category;
  }

  get filteredImages() {
    return this.selectedCategory === 'all' 
      ? this.images 
      : this.images.filter(image => image.category === this.selectedCategory);
  }
}