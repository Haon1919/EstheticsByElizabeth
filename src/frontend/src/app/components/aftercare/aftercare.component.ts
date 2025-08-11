import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ServiceManagementService } from '../../services/service-management.service';
import { Service } from '../../models/services.models';

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
        this.isLoading = false;
      }
    });
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
