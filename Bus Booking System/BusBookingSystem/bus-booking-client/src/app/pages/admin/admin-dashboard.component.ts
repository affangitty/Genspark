import { ApplicationRef, ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AdminApprovalQueueItem, AdminRevenueDashboard } from '../../core/models';
import { AdminService } from '../../core/services/admin.service';
import { httpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  standalone: false
})
export class AdminDashboardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly appRef = inject(ApplicationRef);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly ngZone = inject(NgZone);
  queue: AdminApprovalQueueItem[] = [];
  revenue: AdminRevenueDashboard | null = null;
  routes: Array<{ id: string; sourceCity: string; destinationCity: string; sourceState: string; destinationState: string; isActive: boolean }> =
    [];
  operators: Array<{
    id: string;
    companyName: string;
    contactPersonName: string;
    email: string;
    phoneNumber: string;
    status: string;
    createdAt: string;
  }> = [];
  approvedAssignments: Array<{
    id: string;
    busId: string;
    busNumber: string;
    busName: string;
    busStatus: string;
    seatCount: number;
    operatorName: string;
    operatorStatus: string;
    sourceCity: string;
    destinationCity: string;
    baseFare: number;
    departureTime: string;
    arrivalTime: string;
    reviewedAt: string | null;
  }> = [];
  loading = false;
  /** Shown when one of the parallel admin API calls fails (forkJoin used to abort the whole batch before). */
  loadError = '';

  feeForm = this.fb.group({
    convenienceFeePercentage: [5, [Validators.required, Validators.min(0), Validators.max(100)]],
    useFlatConvenienceFee: [false],
    flatConvenienceFeePerPassenger: [0, [Validators.required, Validators.min(0)]],
    seatLockDurationMinutes: [10, [Validators.required, Validators.min(1), Validators.max(120)]]
  });

  routeForm = this.fb.group({
    sourceCity: ['', Validators.required],
    destinationCity: ['', Validators.required],
    sourceState: ['', Validators.required],
    destinationState: ['', Validators.required]
  });

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    const emptyQueue = {
      items: [] as AdminApprovalQueueItem[],
      totalPending: 0,
      pendingOperators: 0,
      pendingBuses: 0,
      pendingRouteAssignments: 0
    };
    const tag = (label: string, err: unknown) => {
      const msg = httpErrorMessage(err, 'Request failed');
      this.loadError = this.loadError ? `${this.loadError} · ${label}: ${msg}` : `${label}: ${msg}`;
    };

    forkJoin({
      queue: this.adminService.getApprovalQueue().pipe(
        catchError((err) => {
          tag('Approvals', err);
          return of(emptyQueue);
        })
      ),
      revenue: this.adminService.getRevenueDashboard().pipe(
        catchError((err) => {
          tag('Revenue', err);
          return of(null);
        })
      ),
      config: this.adminService.getPlatformConfig().pipe(
        catchError((err) => {
          tag('Platform settings', err);
          return of({
            convenienceFeePercentage: 5,
            useFlatConvenienceFee: false,
            flatConvenienceFeePerPassenger: 0,
            seatLockDurationMinutes: 10
          });
        })
      ),
      routes: this.adminService.getRoutes().pipe(
        catchError((err) => {
          tag('Routes', err);
          return of([]);
        })
      ),
      approvedAssignments: this.adminService.getApprovedRouteAssignments().pipe(
        catchError((err) => {
          tag('Approved assignments', err);
          return of([]);
        })
      ),
      operators: this.adminService.getOperators().pipe(
        catchError((err) => {
          tag('Operators', err);
          return of([]);
        })
      )
    })
      .pipe(
        finalize(() => {
          this.ngZone.run(() => {
            this.loading = false;
            this.appRef.tick();
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: ({ queue, revenue, config, routes, approvedAssignments, operators }) => {
          this.queue = queue.items ?? [];
          this.revenue = revenue;
          this.feeForm.patchValue({
            convenienceFeePercentage: config.convenienceFeePercentage,
            useFlatConvenienceFee: config.useFlatConvenienceFee ?? false,
            flatConvenienceFeePerPassenger: config.flatConvenienceFeePerPassenger ?? 0,
            seatLockDurationMinutes: config.seatLockDurationMinutes
          });
          this.routes = routes;
          this.approvedAssignments = approvedAssignments ?? [];
          this.operators = operators ?? [];
          this.cdr.detectChanges();
        }
      });
  }

  approve(item: AdminApprovalQueueItem, isApproved: boolean): void {
    const request$ =
      item.type === 'Operator'
        ? this.adminService.approveOperator(item.id, isApproved, isApproved ? undefined : 'Rejected by admin')
        : item.type === 'RouteAssignment'
        ? this.adminService.approveRouteAssignment(
            item.id,
            isApproved,
            isApproved ? 'Approved by admin' : 'Rejected by admin'
          )
        : this.adminService.approveBus(item.id, isApproved, isApproved ? 'Approved by admin' : 'Rejected by admin');

    request$.subscribe({
      next: () => this.load(),
      error: (err) => {
        const msg = httpErrorMessage(err, 'Approval failed.');
        // Surface conflicts like missing operator offices in source/destination cities.
        this.loadError = this.loadError ? `${this.loadError} · ${msg}` : msg;
        this.cdr.detectChanges();
      }
    });
  }

  updateFee(): void {
    if (this.feeForm.invalid) return;
    const form = this.feeForm.getRawValue();
    this.adminService
      .updatePlatformConfig({
        convenienceFeePercentage: Number(form.convenienceFeePercentage),
        useFlatConvenienceFee: !!form.useFlatConvenienceFee,
        flatConvenienceFeePerPassenger: Number(form.flatConvenienceFeePerPassenger),
        seatLockDurationMinutes: Number(form.seatLockDurationMinutes)
      })
      .subscribe(() => this.load());
  }

  createRoute(): void {
    if (this.routeForm.invalid) return;
    this.adminService.createRoute(this.routeForm.getRawValue() as any).subscribe(() => {
      this.routeForm.reset();
      this.load();
    });
  }

  disableOperator(operatorId: string): void {
    const reason = window.prompt('Reason for disabling this operator?') ?? '';
    if (!reason.trim()) return;
    this.adminService.disableOperator(operatorId, reason.trim()).subscribe({
      next: () => this.load(),
      error: (err) => {
        const msg = httpErrorMessage(err, 'Disable operator failed.');
        this.loadError = this.loadError ? `${this.loadError} · ${msg}` : msg;
        this.cdr.detectChanges();
      }
    });
  }

  enableOperator(operatorId: string): void {
    this.adminService.enableOperator(operatorId).subscribe({
      next: () => this.load(),
      error: (err) => {
        const msg = httpErrorMessage(err, 'Enable operator failed.');
        this.loadError = this.loadError ? `${this.loadError} · ${msg}` : msg;
        this.cdr.detectChanges();
      }
    });
  }
}
