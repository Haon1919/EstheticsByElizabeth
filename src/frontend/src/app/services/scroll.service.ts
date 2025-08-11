import { Injectable, inject } from '@angular/core';
import { Router, NavigationStart, NavigationEnd, Event } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ScrollService {
  private router = inject(Router);

  constructor() {
    if (typeof window === 'undefined') return;

    try {
      if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
      }
    } catch {}

    this.router.events
      .pipe(filter((e: Event) => e instanceof NavigationStart || e instanceof NavigationEnd))
      .subscribe(e => {
        if (e instanceof NavigationStart) {
          // Immediate pre-navigation jump
          this.jumpToTop();
        } else if (e instanceof NavigationEnd) {
          // After new component render: multiple enforced jumps (no animation)
          requestAnimationFrame(() => this.jumpToTop());
          setTimeout(() => this.jumpToTop(), 30);
          setTimeout(() => this.jumpToTop(), 120);
        }
      });
  }

  private jumpToTop(): void {
    // Primary viewport
    window.scrollTo(0, 0);
    // Standard roots
    document.documentElement.scrollTop = 0;
    document.body.scrollTop = 0;
    // Common app containers
    const main = document.querySelector('main');
    if (main) (main as HTMLElement).scrollTop = 0;
    // Any designated scroll containers
    document.querySelectorAll('[data-scroll-container]').forEach(el => (el as HTMLElement).scrollTop = 0);
  }
}
