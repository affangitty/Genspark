import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { forkJoin, finalize } from 'rxjs';
import { AdminApprovalQueueItem, AdminRevenueDashboard } from '../../core/models';
import { AdminService } from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  standalone: false
})
export class AdminDashboardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  queue: AdminApprovalQueueItem[] = [];
  revenue: AdminRevenueDashboard | null = null;
  routes: Array<{ id: string; sourceCity: string; destinationCity: string; sourceState: string; destinationState: string; isActive: boolean }> =
    [];
  loading = false;

  feeForm = this.fb.group({
    convenienceFeePercentage: [5, [Validators.required, Validators.min(0), Validators.max(100)]],
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
    forkJoin({
      queue: this.adminService.getApprovalQueue(),
      revenue: this.adminService.getRevenueDashboard(),
      config: this.adminService.getPlatformConfig(),
      routes: this.adminService.getRoutes()
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ queue, revenue, config, routes }) => {
          this.queue = queue.items;
          this.revenue = revenue;
          this.feeForm.patchValue({
            convenienceFeePercentage: config.convenienceFeePercentage,
            seatLockDurationMinutes: config.seatLockDurationMinutes
          });
          this.routes = routes;
        }
      });
  }

  approve(item: AdminApprovalQueueItem, isApproved: boolean): void {
    const request$ =
      item.type === 'Operator'
        ? this.adminService.approveOperator(item.id, isApproved, isApproved ? undefined : 'Rejected by admin')
        : this.adminService.approveBus(item.id, isApproved, isApproved ? 'Approved by admin' : 'Rejected by admin');

    request$.subscribe(() => this.load());
  }

  updateFee(): void {
    if (this.feeForm.invalid) return;
    const form = this.feeForm.getRawValue();
    this.adminService
      .updatePlatformConfig(Number(form.convenienceFeePercentage), Number(form.seatLockDurationMinutes))
      .subscribe(() => this.load());
  }

  createRoute(): void {
    if (this.routeForm.invalid) return;
    this.adminService.createRoute(this.routeForm.getRawValue() as any).subscribe(() => {
      this.routeForm.reset();
      this.load();
    });
  }
}
