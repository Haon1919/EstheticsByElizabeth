import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ServiceManagementService } from '../../services/service-management.service';
import { Service } from '../../models/api-models';

@Component({
  selector: 'app-aftercare',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './aftercare.component.html',
  styleUrls: ['./aftercare.component.css']
})
export class AftercareComponent implements OnInit {
  services: Service[] = [];
  filteredServices: Service[] = [];
  categories: { id: number; name: string; services: Service[] }[] = [];
  isLoading = false;
  errorMessage = '';
  selectedCategory = '';

  constructor(
    private serviceManagementService: ServiceManagementService
  ) {}

  ngOnInit(): void {
    this.loadServicesWithAftercare();
  }

  loadServicesWithAftercare(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.serviceManagementService.loadServices().subscribe({
      next: (services: Service[]) => {
        // Filter services that have aftercare instructions
        this.services = services.filter(service => 
          service.afterCareInstructions && service.afterCareInstructions.trim() !== ''
        );
        this.organizeServicesByCategory();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading services:', error);
        this.errorMessage = 'Failed to load aftercare information. Please try again later.';
        this.loadDummyAftercareData();
        this.isLoading = false;
      }
    });
  }

  loadDummyAftercareData(): void {
    // Load dummy services with aftercare instructions for GitHub Pages demo
    this.services = [
      {
        id: 1,
        name: 'Signature Facial',
        description: 'A personalized facial treatment tailored to your specific skin needs and concerns.',
        duration: 60,
        price: 95,
        category: { id: 1, name: 'Facial Treatments' },
        afterCareInstructions: `• Avoid touching your face for at least 6 hours after treatment
• Do not wash your face for 4-6 hours post-treatment
• Avoid makeup for 24 hours if possible
• Use gentle, fragrance-free products for 48 hours
• Apply SPF 30+ daily and avoid direct sun exposure
• Stay hydrated and avoid alcohol for 24 hours
• Do not use retinoids or exfoliating products for 48-72 hours
• Avoid hot showers, saunas, or steam rooms for 24 hours`
      },
      {
        id: 2,
        name: 'Dermaplane + Mini Facial',
        description: 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial.',
        duration: 60,
        price: 100,
        category: { id: 1, name: 'Facial Treatments' },
        afterCareInstructions: `• Avoid touching the treated area for 6-8 hours
• Do not apply makeup for 24 hours
• Use gentle, alcohol-free skincare products
• Apply broad-spectrum SPF 30+ and avoid sun exposure for 48 hours
• Avoid exfoliating products for 3-5 days
• Do not use retinoids for 48-72 hours
• Keep skin moisturized with gentle, fragrance-free products
• Avoid sweating heavily or hot environments for 24 hours`
      },
      {
        id: 3,
        name: 'Chemical Peel',
        description: 'An exfoliating treatment that improves skin texture and tone for a more radiant complexion.',
        duration: 30,
        price: 15,
        category: { id: 1, name: 'Facial Treatments' },
        afterCareInstructions: `• Do not pick, scratch, or peel the skin
• Avoid direct sun exposure and wear SPF 30+ daily
• Use gentle, hydrating products only
• Avoid all exfoliating products for 1 week
• Do not use retinoids, AHA/BHA, or vitamin C for 1 week
• Keep skin moisturized at all times
• Avoid makeup for 24-48 hours if possible
• Expected peeling may occur 2-5 days post-treatment - this is normal`
      },
      {
        id: 4,
        name: 'Brazilian Wax',
        description: 'Complete hair removal from the bikini area for smooth, long-lasting results.',
        duration: 30,
        price: 65,
        category: { id: 2, name: 'Body Waxing' },
        afterCareInstructions: `• Avoid hot showers, baths, saunas, and steam rooms for 24 hours
• Do not apply lotions, creams, or deodorants for 24 hours
• Wear loose, breathable clothing
• Avoid exercise and sweating for 24 hours
• No swimming or hot tubs for 24-48 hours
• Gently exfoliate 2-3 days after treatment to prevent ingrown hairs
• Apply soothing aloe vera gel if needed
• Avoid sexual activity for 24 hours`
      },
      {
        id: 5,
        name: 'Leg Wax (Full)',
        description: 'Complete hair removal from upper and lower legs for silky smooth skin.',
        duration: 45,
        price: 75,
        category: { id: 2, name: 'Body Waxing' },
        afterCareInstructions: `• Avoid hot showers and baths for 12-24 hours
• Do not apply lotions immediately after treatment
• Wear loose-fitting clothing to avoid irritation
• Avoid sun exposure to treated areas for 24 hours
• Gently exfoliate 2-3 days post-treatment
• Apply cool compresses if experiencing redness
• Avoid exercise that causes excessive sweating for 24 hours
• Moisturize gently after 24 hours with fragrance-free products`
      },
      {
        id: 6,
        name: 'Eyebrow Wax & Shape',
        description: 'Precise shaping and grooming of eyebrows for a clean, defined look.',
        duration: 15,
        price: 20,
        category: { id: 2, name: 'Body Waxing' },
        afterCareInstructions: `• Avoid touching the eyebrow area for 2-4 hours
• Do not apply makeup to the area for 4-6 hours
• Avoid sun exposure and tanning for 24 hours
• Use cool compresses if experiencing redness
• Avoid exfoliating the area for 24-48 hours
• Apply soothing aloe vera gel if needed
• Avoid swimming and sweating for 12 hours
• Do not pluck or tweeze for at least 1 week`
      }
    ];

    this.organizeServicesByCategory();
    this.errorMessage = 'Demo mode: Showing sample aftercare instructions.';
  }

  organizeServicesByCategory(): void {
    const categoryMap = new Map<number, { id: number; name: string; services: Service[] }>();

    this.services.forEach(service => {
      if (service.category) {
        const categoryId = service.category.id;
        if (!categoryMap.has(categoryId)) {
          categoryMap.set(categoryId, {
            id: categoryId,
            name: service.category.name,
            services: []
          });
        }
        categoryMap.get(categoryId)!.services.push(service);
      }
    });

    // Sort categories with specific order: Facial Treatments, Waxing, Addons, then alphabetical
    this.categories = Array.from(categoryMap.values()).sort((a, b) => {
      const aName = a.name.toLowerCase();
      const bName = b.name.toLowerCase();
      
      // Get priority for each category (lower number = higher priority)
      const getPriority = (categoryName: string): number => {
        if (categoryName.includes('facial')) return 1;
        if (categoryName.includes('wax')) return 2;
        if (categoryName.includes('addon') || categoryName.includes('add-on')) return 3;
        return 999; // Unknown categories go to the end
      };

      const aPriority = getPriority(aName);
      const bPriority = getPriority(bName);
      
      if (aPriority !== bPriority) {
        return aPriority - bPriority;
      }
      
      // If same priority, sort alphabetically
      return aName.localeCompare(bName);
    });
  }

  filterByCategory(categoryName: string): void {
    this.selectedCategory = categoryName;
    if (!categoryName) {
      this.filteredServices = [...this.services];
    } else {
      this.filteredServices = this.services.filter(service => 
        service.category?.name === categoryName
      );
    }
  }

  scrollToCategory(categoryId: number): void {
    const element = document.getElementById(`category-${categoryId}`);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  formatAftercareText(text: string): string[] {
    // Split by line breaks and filter out empty lines
    return text.split(/\r?\n/).filter(line => line.trim() !== '');
  }

  hasAftercareServices(): boolean {
    return this.services.length > 0;
  }
}
