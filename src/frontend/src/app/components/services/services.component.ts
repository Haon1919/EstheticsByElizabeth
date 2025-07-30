import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ServiceManagementService } from '../../services/service-management.service';
import { Service, Category } from '../../models/api-models';

interface ServiceCategory {
  id: number;
  category: string;
  items: Service[];
}

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './services.component.html',
  styleUrls: ['./services.component.css']
})
export class ServicesComponent implements OnInit {
  clickedItems: {[key: number]: boolean} = {};
  services: ServiceCategory[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(private serviceManagementService: ServiceManagementService) {}

  ngOnInit(): void {
    this.loadServicesAndCategories();
  }

  loadServicesAndCategories(): void {
    this.isLoading = true;
    
    // Load both services and categories
    Promise.all([
      this.serviceManagementService.loadServices().toPromise(),
      this.serviceManagementService.loadCategories().toPromise()
    ]).then(([services, categories]) => {
      this.organizeServicesByCategory(services || [], categories || []);
      this.isLoading = false;
    }).catch((error) => {
      console.error('Error loading services and categories:', error);
      this.errorMessage = 'Failed to load services. Please try again later.';
      this.loadFallbackData();
      this.isLoading = false;
    });
  }

  organizeServicesByCategory(services: Service[], categories: Category[]): void {
    // Create a map of category ID to category object
    const categoryMap = new Map<number, Category>();
    categories.forEach(cat => {
      categoryMap.set(cat.id, cat);
    });

    // Group services by category
    const servicesByCategory = new Map<number, Service[]>();
    services.forEach(service => {
      const categoryId = service.category.id;
      if (!servicesByCategory.has(categoryId)) {
        servicesByCategory.set(categoryId, []);
      }
      servicesByCategory.get(categoryId)?.push(service);
    });

    // Convert to the expected format
    const unsortedServices = Array.from(servicesByCategory.entries()).map(([categoryId, categoryServices]) => ({
      id: categoryId,
      category: categoryMap.get(categoryId)?.name || 'Unknown Category',
      items: categoryServices
    }));

    // Sort services according to the desired order: Facial Treatments, Waxing, Addons, Skincare Brands
    this.services = unsortedServices.sort((a, b) => {
      const aCategory = a.category.toLowerCase();
      const bCategory = b.category.toLowerCase();
      
      // Get priority for each category (lower number = higher priority)
      const getPriority = (categoryName: string): number => {
        if (categoryName.includes('facial')) return 1;
        if (categoryName.includes('wax')) return 2;
        if (categoryName.includes('addon') || categoryName.includes('add-on')) return 3;
        if (categoryName.includes('brand') || categoryName.includes('skincare')) return 4;
        return 999; // Unknown categories go to the end
      };

      const aPriority = getPriority(aCategory);
      const bPriority = getPriority(bCategory);
      
      if (aPriority !== bPriority) {
        return aPriority - bPriority;
      }
      
      // If same priority, sort alphabetically
      return aCategory.localeCompare(bCategory);
    });

    // Add skincare brands category if not present (special category for display)
    if (!this.services.find(cat => cat.category.toLowerCase().includes('brand'))) {
      this.services.push({
        id: 999,
        category: 'Skincare Brands I Use',
        items: [
          {
            id: 401,
            name: 'SkinCeuticals',
            description: 'A skincare brand known for its advanced skincare products backed by science.',
            category: { id: 999, name: 'Skincare Brands I Use' },
            duration: 0,
            price: 0,
            website: 'https://www.skinceuticals.com/'
          } as Service & { website: string },
          {
            id: 402,
            name: 'Bioelements',
            description: 'A professional skincare brand offering customized skincare solutions for all skin types.',
            category: { id: 999, name: 'Skincare Brands I Use' },
            duration: 0,
            price: 0,
            website: 'https://www.bioelements.com/'
          } as Service & { website: string }
        ]
      });
    }
  }

  loadFallbackData(): void {
    // Fallback to hardcoded data structure matching the API format
    this.services = [
      {
        id: 1,
        category: 'Facial Treatments',
        items: [
          {
            id: 101,
            name: 'Signature Facial',
            description: 'A personalized facial treatment tailored to your specific skin needs and concerns.',
            duration: 60,
            price: 95,
            category: { id: 1, name: 'Facial Treatments' }
          },
          {
            id: 102,
            name: 'Dermaplane + Mini Facial',
            description: 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial to cleanse and hydrate the skin.',
            duration: 60,
            price: 100,
            category: { id: 1, name: 'Facial Treatments' }
          },
          {
            id: 103,
            name: 'Back Facial',
            description: 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.',
            duration: 60,
            price: 115,
            category: { id: 1, name: 'Facial Treatments' }
          },
          {
            id: 104,
            name: 'HydraFacial',
            description: 'A medical-grade resurfacing treatment that immediately delivers hydrating and anti-aging benefits.',
            duration: 45,
            price: 150,
            category: { id: 1, name: 'Facial Treatments' }
          },
          {
            id: 105,
            name: 'Express Facial',
            description: 'A quick, refreshing facial perfect for busy schedules. Includes cleansing, exfoliation, and moisturizing.',
            duration: 30,
            price: 65,
            category: { id: 1, name: 'Facial Treatments' }
          }
        ]
      },
      {
        id: 2,
        category: 'Body Waxing',
        items: [
          {
            id: 201,
            name: 'Upper Lip Wax',
            description: 'Quick and precise removal of unwanted hair from the upper lip area.',
            duration: 5,
            price: 15,
            category: { id: 2, name: 'Body Waxing' }
          },
          {
            id: 202,
            name: 'Eyebrow Wax',
            description: 'Precise shaping and grooming of eyebrows for a clean, defined look.',
            duration: 10,
            price: 20,
            category: { id: 2, name: 'Body Waxing' }
          },
          {
            id: 203,
            name: 'Brazilian Wax',
            description: 'Complete hair removal from the bikini area for smooth, long-lasting results.',
            duration: 30,
            price: 65,
            category: { id: 2, name: 'Body Waxing' }
          },
          {
            id: 204,
            name: 'Bikini Line Wax',
            description: 'Hair removal along the bikini line for a clean, comfortable fit in swimwear.',
            duration: 20,
            price: 45,
            category: { id: 2, name: 'Body Waxing' }
          },
          {
            id: 205,
            name: 'Leg Wax (Full)',
            description: 'Complete hair removal from upper and lower legs for silky smooth skin.',
            duration: 45,
            price: 75,
            category: { id: 2, name: 'Body Waxing' }
          },
          {
            id: 206,
            name: 'Underarm Wax',
            description: 'Quick and effective hair removal from the underarm area.',
            duration: 15,
            price: 25,
            category: { id: 2, name: 'Body Waxing' }
          }
        ]
      },
      {
        id: 3,
        category: 'Add-On Services',
        items: [
          {
            id: 301,
            name: 'Chemical Peel',
            description: 'An exfoliating treatment that improves skin texture and tone for a more radiant complexion.',
            duration: 30,
            price: 15,
            category: { id: 3, name: 'Add-On Services' }
          },
          {
            id: 302,
            name: 'LED Light Therapy',
            description: 'Non-invasive treatment using different wavelengths of light to promote healing and rejuvenation.',
            duration: 20,
            price: 25,
            category: { id: 3, name: 'Add-On Services' }
          },
          {
            id: 303,
            name: 'Enzyme Mask',
            description: 'Natural enzyme treatment to gently exfoliate and brighten the skin.',
            duration: 15,
            price: 20,
            category: { id: 3, name: 'Add-On Services' }
          }
        ]
      },
      {
        id: 4,
        category: 'Skincare Brands I Use',
        items: [
          {
            id: 401,
            name: 'SkinCeuticals',
            description: 'A skincare brand known for its advanced skincare products backed by science.',
            category: { id: 4, name: 'Skincare Brands I Use' },
            duration: 0,
            price: 0,
            website: 'https://www.skinceuticals.com/'
          } as Service & { website: string },
          {
            id: 402,
            name: 'Bioelements',
            description: 'A professional skincare brand offering customized skincare solutions for all skin types.',
            category: { id: 4, name: 'Skincare Brands I Use' },
            duration: 0,
            price: 0,
            website: 'https://www.bioelements.com/'
          } as Service & { website: string },
          {
            id: 403,
            name: 'Dermalogica',
            description: 'Professional skincare products designed to improve skin health through education and innovation.',
            category: { id: 4, name: 'Skincare Brands I Use' },
            duration: 0,
            price: 0,
            website: 'https://www.dermalogica.com/'
          } as Service & { website: string }
        ]
      }
    ];
    
    this.errorMessage = 'Demo mode: Showing sample services data.';
  }

  handleItemClick(item: Service): void {
    if ((item as any).website) {
      this.clickedItems[item.id] = !this.clickedItems[item.id];
    }
  }

  navigateToWebsite(url: string | undefined): void {
    if (url) {
      window.open(url, '_blank');
    }
  }

  formatDuration(minutes: number): string {
    if (minutes === 0) return '';
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}min` : `${hours}h`;
  }

  hasWebsite(item: Service): boolean {
    return !!(item as any).website;
  }

  getWebsiteUrl(item: Service): string {
    return (item as any).website || '';
  }
}