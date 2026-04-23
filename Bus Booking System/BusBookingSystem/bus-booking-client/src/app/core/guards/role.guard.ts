import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const expectedRoles = (route.data['roles'] as string[] | undefined) ?? [];
  if (!authService.isLoggedIn()) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (expectedRoles.length === 0 || authService.hasRole(expectedRoles)) {
    return true;
  }

  router.navigate(['/']);
  return false;
};
