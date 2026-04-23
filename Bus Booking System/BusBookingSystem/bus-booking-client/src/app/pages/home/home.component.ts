import { ApplicationRef, ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription, finalize, timeout } from 'rxjs';
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
  private searchSub?: Subscription;
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
          this.sourceSuggestions = cities.map((c) => c.sourceCity);
        },
        error: () => {
          this.sourceSuggestions = [];
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
          this.destinationSuggestions = data.map((d) => d.destinationCity);
        },
        error: () => {
          this.destinationSuggestions = [];
        }
      });
  }

  search(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }
    this.searchSub?.unsubscribe();
    this.loading = true;
    this.hasCompletedSearch = false;
    this.error = '';
    this.buses = [];
    this.cdr.detectChanges();
    const form = this.searchForm.getRawValue();
    this.searchSub = this.busService
      .searchBuses(form.sourceCity ?? '', form.destinationCity ?? '', form.journeyDate ?? '', form.passengerCount ?? 1)
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.ngZone.run(() => {
            this.loading = false;
            this.hasCompletedSearch = true;
            this.searchSub = undefined;
            this.appRef.tick();
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: (res) => {
          this.buses = res;
          this.cdr.detectChanges();
        },
        error: (err) => {
          if (err?.name === 'TimeoutError') {
            this.error = 'Search timed out. Is the API running on http://localhost:5153?';
            return;
          }
          this.error = httpErrorMessage(err, 'Failed to search buses.');
          this.cdr.detectChanges();
        }
      });
  }

  selectBus(bus: Bus): void {
    const journeyDate = this.searchForm.value.journeyDate;
    if (!journeyDate) return;
    this.router.navigate(['/booking/seat-selection', bus.id], { queryParams: { journeyDate } });
  }
}
