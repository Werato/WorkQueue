import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { WorkItemsComponent } from './components/work-items/work-items';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'work-items', component: WorkItemsComponent, canActivate: [authGuard] }
];