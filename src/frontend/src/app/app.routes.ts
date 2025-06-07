import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { ServicesComponent } from './components/services/services.component';
import { BookingComponent } from './components/booking/booking.component';
import { GalleryComponent } from './components/gallery/gallery.component';
import { ContactComponent } from './components/contact/contact.component';
import { AdminLoginComponent } from './components/admin-login/admin-login.component';
import { ContactSubmissionsComponent } from './components/contact-submissions/contact-submissions.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'services', component: ServicesComponent },
  { path: 'booking', component: BookingComponent },
  { path: 'gallery', component: GalleryComponent },
  { path: 'contact', component: ContactComponent },
  { path: 'admin', component: AdminLoginComponent },
  { 
    path: 'admin/submissions', 
    component: ContactSubmissionsComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'admin/dashboard', 
    component: ContactSubmissionsComponent,
    canActivate: [AuthGuard]
  },
  { path: '**', redirectTo: '' }
];
