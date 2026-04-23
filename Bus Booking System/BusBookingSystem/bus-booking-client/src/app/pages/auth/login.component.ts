import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  error = '';
  loading = false;
  role: 'User' | 'Operator' | 'Admin' = 'User';

  form = this.fb.group({
    loginId: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.applyLoginValidators();
  }

  onRoleChange(): void {
    this.applyLoginValidators();
  }

  private applyLoginValidators(): void {
    const c = this.form.get('loginId');
    if (!c) return;
    if (this.role === 'User') {
      c.setValidators([Validators.required, Validators.minLength(3)]);
    } else {
      c.setValidators([Validators.required, Validators.email]);
    }
    c.updateValueAndValidity({ emitEvent: false });
  }

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
    const { loginId, password } = this.form.getRawValue();
    if (!loginId || !password) {
      this.loading = false;
      return;
    }

    const request$ =
      this.role === 'Admin'
        ? this.authService.loginAdmin(loginId, password)
        : this.role === 'Operator'
        ? this.authService.loginOperator(loginId, password)
        : this.authService.loginUser(loginId, password);

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
