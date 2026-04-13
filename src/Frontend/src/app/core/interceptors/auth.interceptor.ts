import { HttpInterceptorFn } from '@angular/common/http';

const TOKEN_KEY = 'aerarium_token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(TOKEN_KEY) : null;

  if (token) {
    const cloned = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
    return next(cloned);
  }

  return next(req);
};
