import { ApplicationRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { Bus } from '../../core/models';
import { BusService } from '../../core/services/bus.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: false
})
export class HomeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly appRef = inject(ApplicationRef);
  loading = false;
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
      .pipe(timeout(25_000), finalize(() => this.appRef.tick()))
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
      .pipe(timeout(25_000), finalize(() => this.appRef.tick()))
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
    this.loading = true;
    this.error = '';
    this.buses = [];
    const form = this.searchForm.getRawValue();
    this.busService
      .searchBuses(form.sourceCity ?? '', form.destinationCity ?? '', form.journeyDate ?? '', form.passengerCount ?? 1)
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.loading = false;
          this.appRef.tick();
        })
      )
      .subscribe({
        next: (res) => {
          this.buses = res;
        },
        error: (err) => {
          if (err?.name === 'TimeoutError') {
            this.error = 'Search timed out. Is the API running on http://localhost:5153?';
            return;
          }
          const body = err?.error;
          this.error =
            typeof body === 'string'
              ? body
              : body?.detail ?? body?.message ?? err?.message ?? 'Failed to search buses.';
        }
      });
  }

  selectBus(bus: Bus): void {
    const journeyDate = this.searchForm.value.journeyDate;
    if (!journeyDate) return;
    this.router.navigate(['/booking/seat-selection', bus.id], { queryParams: { journeyDate } });
  }
}
