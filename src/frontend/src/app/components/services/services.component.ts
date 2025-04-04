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
          name: 'Signature Facial',
          description: 'A personalized facial treatment tailored to your specific skin needs and concerns.',
          duration: '60 min',
          price: 95
        },
        {
          id: 102,
          name: 'Dermaplane + mini facial',
          description: 'A dual treatment combining dermaplaning to remove dead skin cells and peach fuzz, followed by a mini facial to cleanse and hydrate the skin.',
          duration: '50-60 min',
          price: 100
        },
        {
          id: 103,
          name: 'Back Facial',
          description: 'A specialized treatment for the back area, focusing on cleansing, exfoliating, and hydrating.',
          duration: '60 min',
          price: 115
        }
      ]
    },
    {
      id: 2,
      category: 'Waxing',
      items: [
        {
          id: 201,
          name: 'Upper lip wax',
          description: 'Quick and precise removal of unwanted hair from the upper lip area.',
          duration: '5 min',
          price: 15
        },
        {
          id: 202,
          name: 'Eyebrow wax',
          description: 'Precise shaping and grooming of eyebrows for a clean, defined look.',
          duration: '10 min',
          price: 20
        }
      ]
    },
    {
      id: 3,
      category: 'Addons',
      items: [
        {
          id: 301,
          name: 'Chemical peels',
          description: 'An exfoliating treatment that improves skin texture and tone for a more radiant complexion.',
          duration: 'Varies',
          price: 15
        }
      ]
    }
  ];

  skincareBrands = [
    { name: 'Skinceuticals', website: 'https://www.skinceuticals.com/' },
    { name: 'Bioelements', website: 'https://www.bioelements.com/' }
  ];
}