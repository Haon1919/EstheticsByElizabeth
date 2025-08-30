import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  title = 'Esthetics by Elizabeth';
  isAuthenticated = false;
  isDropdownOpen = false;
  isMobileMenuOpen = false;
  private authSubscription?: Subscription;
  private routerSubscription?: Subscription;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to authentication state changes
    this.authSubscription = this.authService.session$.subscribe(session => {
      this.isAuthenticated = session.isAuthenticated;
    });

    // Subscribe to router events to close dropdown and mobile menu on navigation
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.closeDropdown();
        this.closeMobileMenu();
      });
  }

  ngOnDestroy(): void {
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    // Close dropdown when clicking outside
    this.closeDropdown();
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKeyPress(event: KeyboardEvent): void {
    // Close mobile menu and dropdown on escape key
    this.closeMobileMenu();
    this.closeDropdown();
  }

  @HostListener('window:resize', ['$event'])
  onWindowResize(): void {
    // Close mobile menu on window resize (orientation change)
    if (window.innerWidth > 768) {
      this.closeMobileMenu();
    }
  }

  logout(): void {
    this.closeDropdown();
    this.closeMobileMenu();
    this.authService.logout();
    this.router.navigate(['/']);
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    
    // Prevent body scroll when mobile menu is open
    if (this.isMobileMenuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
    document.body.style.overflow = '';
  }

  onDropdownItemClick(): void {
    // Close dropdown and mobile menu immediately when item is clicked
    this.closeDropdown();
    this.closeMobileMenu();
  }
}