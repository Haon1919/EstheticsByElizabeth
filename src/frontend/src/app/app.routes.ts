import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { ServicesComponent } from './components/services/services.component';
import { BookingComponent } from './components/booking/booking.component';
import { GalleryComponent } from './components/gallery/gallery.component';
import { ContactComponent } from './components/contact/contact.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'services', component: ServicesComponent },
  { path: 'booking', component: BookingComponent },
  { path: 'gallery', component: GalleryComponent },
  { path: 'contact', component: ContactComponent },
  { path: '**', redirectTo: '' }
];
