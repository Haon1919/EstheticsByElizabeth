import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterModule } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { FooterComponent } from './components/footer/footer.component';
import { AuthService } from './services/auth.service';
import { ScrollService } from './services/scroll.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet, 
    RouterModule,
    NavbarComponent, 
    FooterComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Esthetics by Elizabeth';
  isAuthenticated = false;
  private authSub?: Subscription;

  constructor(private authService: AuthService, private _scroll: ScrollService) {}

  ngOnInit(): void {
    this.authSub = this.authService.session$.subscribe(session => {
      this.isAuthenticated = session.isAuthenticated;
    });
  }

  ngOnDestroy(): void {
    this.authSub?.unsubscribe();
  }
}
