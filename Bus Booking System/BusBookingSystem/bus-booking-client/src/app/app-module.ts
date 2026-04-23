import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule, provideClientHydration } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { CoreModule } from './core/core.module';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/auth/login.component';
import { RegisterComponent } from './pages/auth/register.component';
import { SeatSelectionComponent } from './pages/booking/seat-selection.component';
import { BookingFlowComponent } from './pages/booking/booking-flow.component';
import { UserDashboardComponent } from './pages/user/user-dashboard.component';
import { OperatorDashboardComponent } from './pages/operator/operator-dashboard.component';
import { AdminDashboardComponent } from './pages/admin/admin-dashboard.component';

@NgModule({
  declarations: [
    App,
    HomeComponent,
    LoginComponent,
    RegisterComponent,
    SeatSelectionComponent,
    BookingFlowComponent,
    UserDashboardComponent,
    OperatorDashboardComponent,
    AdminDashboardComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CoreModule,
    FormsModule,
    ReactiveFormsModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withEventReplay() can defer DOM updates and leave HTTP-driven spinners stuck; keep hydration only.
    provideClientHydration(),
  ],
  bootstrap: [App]
})
export class AppModule { }
