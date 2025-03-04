import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
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
      image: 'assets/images/testimonial1.jpg'
    },
    {
      id: 2,
      name: 'Michael Rodriguez',
      quote: 'I was hesitant about getting a massage, but the staff made me feel comfortable. The experience was incredible and I left feeling rejuvenated.',
      rating: 5,
      image: 'assets/images/testimonial2.jpg'
    },
    {
      id: 3,
      name: 'Emma Thompson',
      quote: "I've been coming here for waxing for over a year. The service is always consistent and the results are fantastic. Highly recommend!",
      rating: 4,
      image: 'assets/images/testimonial3.jpg'
    }
  ];
}