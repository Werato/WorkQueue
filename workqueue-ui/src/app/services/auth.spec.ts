import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

// Описываем интерфейс ответа от нашего бекенда
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
  // Замени порт на тот, на котором запускается твой бекенд
  private apiUrl = 'https://localhost:7142/api/auth/login'; 

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(this.apiUrl, { email, password }).pipe(
      tap(response => {
        // ТЗ: Store the JWT in a reasonable client-side auth service
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