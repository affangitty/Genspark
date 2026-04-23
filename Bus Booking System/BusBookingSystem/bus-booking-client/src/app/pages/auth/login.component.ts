import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  standalone: false
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  error = '';
  loading = false;
  role: 'User' | 'Operator' | 'Admin' = 'User';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  constructor(private authService: AuthService, private router: Router) {}

  private endSubmit(): void {
    this.loading = false;
    this.cdr.detectChanges();
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    const { email, password } = this.form.getRawValue();
    if (!email || !password) {
      this.loading = false;
      return;
    }

    const request$ =
      this.role === 'Admin'
        ? this.authService.loginAdmin(email, password)
        : this.role === 'Operator'
        ? this.authService.loginOperator(email, password)
        : this.authService.loginUser(email, password);

    request$.pipe(timeout(20_000), finalize(() => this.endSubmit())).subscribe({
      next: () => {
        const signedInRole = this.authService.getRole();
        if (signedInRole === 'Admin') this.router.navigate(['/admin/dashboard']);
        else if (signedInRole === 'Operator') this.router.navigate(['/operator/dashboard']);
        else this.router.navigate(['/user/dashboard']);
        this.cdr.detectChanges();
      },
      error: (err) => {
        if (err?.name === 'TimeoutError') {
          this.error = 'Request timed out. Start the API on http://localhost:5153 and try again.';
        } else {
          const body = err?.error;
          this.error =
            typeof body === 'string'
              ? body
              : body?.detail ?? body?.message ?? err?.message ?? 'Login failed.';
        }
        this.cdr.detectChanges();
      }
    });
  }
}
