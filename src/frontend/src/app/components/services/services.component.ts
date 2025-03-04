import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './services.component.html',
  styleUrls: ['./services.component.css']
})
export class ServicesComponent {
  services = [
    {
      id: 1,
      category: 'Facial Treatments',
      items: [
        {
          id: 101,
          name: 'Custom Facial',
          description: 'A personalized facial treatment tailored to your specific skin needs and concerns.',
          duration: '60 min',
          price: 85
        },
        {
          id: 102,
          name: 'Deep Cleansing Facial',
          description: 'A thorough cleansing treatment that removes impurities, unclogs pores, and prevents breakouts.',
          duration: '75 min',
          price: 95
        },
        {
          id: 103,
          name: 'Anti-Aging Facial',
          description: 'A specialized treatment that targets fine lines, wrinkles, and loss of elasticity for more youthful skin.',
          duration: '90 min',
          price: 120
        },
        {
          id: 104,
          name: 'Hydrating Facial',
          description: 'Intense hydration for dry or dehydrated skin to restore moisture and radiance.',
          duration: '60 min',
          price: 90
        }
      ]
    },
    {
      id: 2,
      category: 'Body Treatments',
      items: [
        {
          id: 201,
          name: 'Body Scrub',
          description: 'An exfoliating treatment that removes dead skin cells, leaving your skin smooth and refreshed.',
          duration: '45 min',
          price: 70
        },
        {
          id: 202,
          name: 'Body Wrap',
          description: 'A detoxifying treatment that helps eliminate toxins, reduce water retention, and moisturize the skin.',
          duration: '60 min',
          price: 95
        },
        {
          id: 203,
          name: 'Massage Therapy',
          description: 'A relaxing massage that relieves muscle tension and promotes overall well-being.',
          duration: '60 min',
          price: 85
        },
        {
          id: 204,
          name: 'Back Facial',
          description: 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.',
          duration: '45 min',
          price: 75
        }
      ]
    },
    {
      id: 3,
      category: 'Waxing Services',
      items: [
        {
          id: 301,
          name: 'Eyebrow Waxing',
          description: 'Precise shaping and grooming of eyebrows for a clean, defined look.',
          duration: '15 min',
          price: 20
        },
        {
          id: 302,
          name: 'Lip & Chin Waxing',
          description: 'Quick and effective removal of unwanted facial hair.',
          duration: '15 min',
          price: 18
        },
        {
          id: 303,
          name: 'Full Leg Waxing',
          description: 'Complete hair removal from the entire leg for smooth, hair-free skin.',
          duration: '45 min',
          price: 65
        },
        {
          id: 304,
          name: 'Brazilian Waxing',
          description: 'Complete hair removal in the bikini area for a clean, smooth result.',
          duration: '30 min',
          price: 70
        }
      ]
    },
    {
      id: 4,
      category: 'Makeup Services',
      items: [
        {
          id: 401,
          name: 'Special Event Makeup',
          description: 'Professional makeup application for special occasions, parties, or events.',
          duration: '60 min',
          price: 75
        },
        {
          id: 402,
          name: 'Bridal Makeup',
          description: 'Exquisite makeup application for brides, including a consultation and trial.',
          duration: '90 min',
          price: 150
        },
        {
          id: 403,
          name: 'Makeup Lesson',
          description: 'A personalized tutorial teaching you techniques and products suited for your features.',
          duration: '75 min',
          price: 100
        }
      ]
    }
  ];
}