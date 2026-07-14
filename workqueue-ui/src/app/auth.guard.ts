import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('jwt_token');

  if (token) {
    return true; // Разрешаем переход, если токен есть
  } else {
    router.navigate(['/login']); // Перекидываем на логин, если токена нет
    return false;
  }
};