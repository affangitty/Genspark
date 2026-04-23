import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/auth/login.component';
import { RegisterComponent } from './pages/auth/register.component';
import { SeatSelectionComponent } from './pages/booking/seat-selection.component';
import { BookingFlowComponent } from './pages/booking/booking-flow.component';
import { UserDashboardComponent } from './pages/user/user-dashboard.component';
import { OperatorDashboardComponent } from './pages/operator/operator-dashboard.component';
import { AdminDashboardComponent } from './pages/admin/admin-dashboard.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: RegisterComponent },
  { path: 'auth/operator-register', component: RegisterComponent },
  { path: 'booking/seat-selection/:busId', component: SeatSelectionComponent },
  { path: 'booking/flow', component: BookingFlowComponent, canActivate: [authGuard, roleGuard], data: { roles: ['User'] } },
  { path: 'user/dashboard', component: UserDashboardComponent, canActivate: [authGuard, roleGuard], data: { roles: ['User'] } },
  { path: 'operator/dashboard', component: OperatorDashboardComponent, canActivate: [authGuard, roleGuard], data: { roles: ['Operator'] } },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
