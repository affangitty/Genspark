import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { UserDashboardReloadService } from './core/services/user-dashboard-reload.service';
import { User } from './core/models';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss'
})
export class App {
  constructor(
    private authService: AuthService,
    private router: Router,
    private userDashboardReload: UserDashboardReloadService
  ) {}

  /** Re-fetch bookings when the user clicks "My bookings" while already on that page. */
  onMyBookingsNavClick(): void {
    if (this.router.url.split('?')[0] === '/user/dashboard') {
      this.userDashboardReload.requestReload();
    }
  }

  get user(): User | null {
    return this.authService.getCurrentUser();
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
