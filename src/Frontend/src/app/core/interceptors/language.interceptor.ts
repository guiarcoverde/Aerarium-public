import { HttpInterceptorFn } from '@angular/common/http';

const STORAGE_KEY = 'aerarium_lang';
const DEFAULT_LANG = 'pt-BR';

export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const lang =
    (typeof localStorage !== 'undefined' && localStorage.getItem(STORAGE_KEY)) || DEFAULT_LANG;
  const cloned = req.clone({
    setHeaders: { 'Accept-Language': lang },
  });
  return next(cloned);
};
