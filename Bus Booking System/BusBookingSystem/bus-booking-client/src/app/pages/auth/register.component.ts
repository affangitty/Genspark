import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

/** Matches BusBooking.Application.Validators (RegisterRequestValidator / OperatorRegistrationValidator). */
const PHONE_PATTERN = /^\+?[0-9]{10,15}$/;
const PASSWORD_VALIDATORS = [
  Validators.required,
  Validators.minLength(8),
  Validators.pattern(/[A-Z]/),
  Validators.pattern(/[a-z]/),
  Validators.pattern(/[0-9]/)
];

function passwordsMatchValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const pass = group.get('password')?.value as string | undefined;
    const confirm = group.get('confirmPassword')?.value as string | undefined;
    if (pass == null || confirm == null || confirm === '') return null;
    return pass === confirm ? null : { passwordsMismatch: true };
  };
}

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  standalone: false
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  isOperator = false;
  loading = false;
  message = '';
  error = '';

  userForm = this.fb.group(
    {
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
      password: ['', PASSWORD_VALIDATORS],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatchValidator() }
  );

  operatorForm = this.fb.group(
    {
      companyName: ['', Validators.required],
      contactPersonName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
      password: ['', PASSWORD_VALIDATORS],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatchValidator() }
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isOperator = this.route.snapshot.url.some((s) => s.path.includes('operator'));
  }

  /** Form bound in the template (user vs operator). */
  get activeForm(): FormGroup {
    return this.isOperator ? this.operatorForm : this.userForm;
  }

  /** Aligns with API: optional leading +, then digits only (10–15). */
  private normalizePhone(value: string | null | undefined): string {
    const raw = String(value ?? '').trim();
    if (raw.startsWith('+')) {
      return '+' + raw.slice(1).replace(/\D/g, '');
    }
    return raw.replace(/\D/g, '');
  }

  private formatHttpError(err: unknown, fallback: string): string {
    const e = err as {
      error?: unknown;
      message?: string;
      name?: string;
      status?: number;
      statusText?: string;
    };
    if (e?.name === 'TimeoutError') {
      return 'Request timed out. Start the API (http://localhost:5153) and try again.';
    }
    const body = e?.error;
    if (typeof body === 'string') return body;
    if (body && typeof body === 'object') {
      const o = body as Record<string, unknown>;
      const rawErrors = o['errors'];
      if (rawErrors && typeof rawErrors === 'object' && !Array.isArray(rawErrors)) {
        const msgs: string[] = [];
        for (const v of Object.values(rawErrors as Record<string, unknown>)) {
          if (typeof v === 'string') msgs.push(v);
          else if (Array.isArray(v)) {
            for (const item of v) {
              if (typeof item === 'string') msgs.push(item);
            }
          }
        }
        if (msgs.length) return msgs.join(' ');
      }
      if (typeof o['detail'] === 'string') return o['detail'];
      if (typeof o['message'] === 'string') return o['message'];
      if (typeof o['title'] === 'string') return o['title'];
    }
    if (typeof e.status === 'number' && e.status >= 400) {
      return `${fallback} (HTTP ${e.status}${e.statusText ? ` ${e.statusText}` : ''})`;
    }
    return e?.message ?? fallback;
  }

  private endSubmit(): void {
    this.loading = false;
    this.cdr.detectChanges();
  }

  submit(): void {
    const form: FormGroup = this.isOperator ? this.operatorForm : this.userForm;
    const phoneCtrl = form.get('phoneNumber');
    if (phoneCtrl) {
      phoneCtrl.setValue(this.normalizePhone(phoneCtrl.value as string), { emitEvent: false });
      phoneCtrl.updateValueAndValidity({ emitEvent: false });
    }
    if (form.invalid) {
      form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';

    const request$ = this.isOperator
      ? this.authService.registerOperator(this.operatorForm.getRawValue() as any)
      : this.authService.register(this.userForm.getRawValue() as any);

    request$.pipe(timeout(20_000), finalize(() => this.endSubmit())).subscribe({
      next: () => {
        this.message = this.isOperator
          ? 'Operator registration submitted. Awaiting admin approval.'
          : 'Registration successful. You can now login.';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.formatHttpError(err, 'Registration failed.');
        this.cdr.detectChanges();
      }
    });
  }
}
