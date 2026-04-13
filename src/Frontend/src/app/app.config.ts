import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { AerariumTitleStrategy, routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { languageInterceptor } from './core/interceptors/language.interceptor';
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, languageInterceptor])),
    provideCharts(withDefaultRegisterables()),
    provideTranslateService({
      fallbackLang: 'pt-BR',
      lang: 'pt-BR',
    }),
    provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' }),
    {
      provide: LOCALE_ID,
      useValue: (typeof localStorage !== 'undefined' && localStorage.getItem('aerarium_lang')) || 'pt-BR',
    },
    { provide: TitleStrategy, useClass: AerariumTitleStrategy },
  ],
};
