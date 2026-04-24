import { isPlatformBrowser } from '@angular/common';
import { ApplicationRef, ChangeDetectorRef, Component, NgZone, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, firstValueFrom, timeout } from 'rxjs';
import { Bus } from '../../core/models';
import { BusService } from '../../core/services/bus.service';
import { httpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: false
})
export class HomeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly appRef = inject(ApplicationRef);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly ngZone = inject(NgZone);
  private readonly platformId = inject(PLATFORM_ID);
  /** Monotonic id so overlapping searches do not clear loading or show stale errors. */
  private searchRequestId = 0;
  loading = false;
  /** True after a search request finishes (success, error, or timeout). */
  hasCompletedSearch = false;
  error = '';
  buses: Bus[] = [];
  sourceSuggestions: string[] = [];
  destinationSuggestions: string[] = [];

  searchForm = this.fb.group({
    sourceCity: ['', Validators.required],
    destinationCity: ['', Validators.required],
    journeyDate: ['', Validators.required],
    passengerCount: [1]
  });

  constructor(private busService: BusService, private router: Router) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    this.busService
      .getSourceCities()
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.ngZone.run(() => {
            this.appRef.tick();
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: (cities) => {
          // Defer to next tick to avoid NG0100 during SSR hydration / fast responses.
          this.ngZone.run(() => {
            queueMicrotask(() => {
              this.sourceSuggestions = cities.map((c) => c.sourceCity);
              this.cdr.detectChanges();
            });
          });
        },
        error: () => {
          this.ngZone.run(() => {
            queueMicrotask(() => {
              this.sourceSuggestions = [];
              this.cdr.detectChanges();
            });
          });
        }
      });
  }

  onSourceInput(): void {
    const sourceCity = this.searchForm.value.sourceCity;
    if (!sourceCity) return;
    this.busService
      .getDestinationCities(sourceCity)
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.ngZone.run(() => {
            this.appRef.tick();
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: (data) => {
          this.ngZone.run(() => {
            queueMicrotask(() => {
              this.destinationSuggestions = data.map((d) => d.destinationCity);
              this.cdr.detectChanges();
            });
          });
        },
        error: () => {
          this.ngZone.run(() => {
            queueMicrotask(() => {
              this.destinationSuggestions = [];
              this.cdr.detectChanges();
            });
          });
        }
      });
  }

  async search(): Promise<void> {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const requestId = ++this.searchRequestId;

    this.loading = true;
    this.hasCompletedSearch = false;
    this.error = '';
    this.buses = [];
    this.cdr.detectChanges();

    const form = this.searchForm.getRawValue();
    const source = (form.sourceCity ?? '').trim();
    const dest = (form.destinationCity ?? '').trim();
    const journeyDate = form.journeyDate ?? '';
    const passengerCount = form.passengerCount ?? 1;

    try {
      const res = await firstValueFrom(
        this.busService.searchBuses(source, dest, journeyDate, passengerCount).pipe(timeout(25_000))
      );
      this.finishSearchInZone(requestId, () => {
        this.buses = res;
      });
    } catch (err: unknown) {
      this.finishSearchInZone(requestId, () => {
        const e = err as { name?: string };
        if (e?.name === 'TimeoutError') {
          this.error = 'Search timed out. Is the API running on http://localhost:5153?';
        } else {
          this.error = httpErrorMessage(err, 'Failed to search buses.');
        }
      });
    }
  }

  /** Http/async completion can resume outside NgZone — run UI updates inside the zone so the template refreshes without a click. */
  private finishSearchInZone(requestId: number, applyResult: () => void): void {
    this.ngZone.run(() => {
      if (requestId !== this.searchRequestId) {
        return;
      }
      applyResult();
      this.loading = false;
      this.hasCompletedSearch = true;
      this.cdr.detectChanges();
    });
  }

  selectBus(bus: Bus): void {
    const journeyDate = this.searchForm.value.journeyDate;
    if (!journeyDate) return;
    this.router.navigate(['/booking/seat-selection', bus.id], { queryParams: { journeyDate } });
  }
}
