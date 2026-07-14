import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

export interface AuthResponse {
  token: string;
  userProfile: {
    id: string;
    name: string;
    email: string;
    role: string;
    organizationId: string;
    organizationName: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7122/api/auth/login'; 

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(this.apiUrl, { email, password }).pipe(
      tap(response => {
        localStorage.setItem('jwt_token', response.token);
        localStorage.setItem('user_profile', JSON.stringify(response.userProfile));
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  logout() {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_profile');
  }
}