import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/** Lets the shell nav re-trigger "My bookings" when the user is already on that route. */
@Injectable({ providedIn: 'root' })
export class UserDashboardReloadService {
  private readonly reload$ = new Subject<void>();

  /** Subscribed by `UserDashboardComponent`. */
  get onReload() {
    return this.reload$.asObservable();
  }

  requestReload(): void {
    this.reload$.next();
  }
}
