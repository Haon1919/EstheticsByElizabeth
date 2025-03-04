import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-service-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './service-card.component.html',
  styleUrls: ['./service-card.component.css']
})
export class ServiceCardComponent {
  @Input() service: any;
}