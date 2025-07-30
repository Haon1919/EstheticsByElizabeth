import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { TestimonialComponent } from '../testimonial/testimonial.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, CommonModule, TestimonialComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit, OnDestroy {
  // Parallax variables
  private tetheredDelay = 100; // Delay before content follows (in pixels)
  private scrollY = 0;
  private heroBackgroundEl: HTMLElement | null = null;
  private heroContentEl: HTMLElement | null = null;
  private parallaxRatio = 0.4; // How fast the background moves compared to scroll
  public backgroundStyle: SafeStyle;

  constructor(private sanitizer: DomSanitizer) {
    // Create a safe style that includes the background image
    this.backgroundStyle = this.sanitizer.bypassSecurityTrustStyle(
      `linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url('../../../../../../../assets/images/image7.jpeg')`
    );
  }

  ngOnInit() {
    // Get references to the elements
    this.heroBackgroundEl = document.querySelector('.hero-background');
    this.heroContentEl = document.querySelector('.hero-content');
    
    // Apply the background style directly
    if (this.heroBackgroundEl) {
      this.heroBackgroundEl.style.backgroundImage = this.backgroundStyle as string;
    }
    
    // Set initial positions
    this.updateParallaxPositions();
  }

  ngOnDestroy() {
    // Clean up if needed
  }

  @HostListener('window:scroll', ['$event'])
  onWindowScroll() {
    this.scrollY = window.scrollY;
    this.updateParallaxPositions();
  }

  private updateParallaxPositions() {
    if (!this.heroBackgroundEl || !this.heroContentEl) return;

    // Calculate background movement (starts immediately)
    const backgroundY = this.scrollY * this.parallaxRatio;
    
    // Calculate content movement (starts after tetheredDelay)
    let contentY = 0;
    if (this.scrollY > this.tetheredDelay) {
      // Once the "tether" pulls, content starts moving too
      contentY = (this.scrollY - this.tetheredDelay) * this.parallaxRatio;
    }
    
    // Apply transformations
    this.heroBackgroundEl.style.transform = `translateY(${backgroundY}px)`;
    this.heroContentEl.style.transform = `translateY(${contentY}px)`;
  }

  featuredServices = [
    {
      id: 1,
      title: 'Facial Treatments',
      description: 'Rejuvenate your skin with our customized facial treatments designed for your specific skin needs.',
      image: 'assets/images/facial.jpg',
      price: 85
    },
    {
      id: 2,
      title: 'Body Treatments',
      description: 'Relax and detoxify with our luxurious body treatments, including scrubs, wraps, and massages.',
      image: 'assets/images/body.jpg',
      price: 95
    },
    {
      id: 3,
      title: 'Waxing Services',
      description: 'Achieve smooth, hair-free skin with our gentle and effective waxing services.',
      image: 'assets/images/waxing.jpg',
      price: 45
    }
  ];

  testimonials = [
    {
      id: 1,
      name: 'Sarah Johnson',
      quote: 'The facial treatment was amazing! My skin has never looked better. The esthetician was knowledgeable and made me feel completely relaxed.',
      rating: 5,
      image: 'https://images.unsplash.com/photo-1494790108755-2616b612b977?w=150&h=150&fit=crop&crop=face'
    },
    {
      id: 2,
      name: 'Michael Rodriguez',
      quote: 'I was hesitant about getting a massage, but the staff made me feel comfortable. The experience was incredible and I left feeling rejuvenated.',
      rating: 5,
      image: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face'
    },
    {
      id: 3,
      name: 'Emma Thompson',
      quote: "I've been coming here for waxing for over a year. The service is always consistent and the results are fantastic. Highly recommend!",
      rating: 4,
      image: 'https://images.unsplash.com/photo-1489424731084-a5d8b219a5bb?w=150&h=150&fit=crop&crop=face'
    }
  ];
}