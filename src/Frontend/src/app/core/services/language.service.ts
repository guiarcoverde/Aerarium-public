import { inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import localeEn from '@angular/common/locales/en';

export type Language = 'pt-BR' | 'en';

const STORAGE_KEY = 'aerarium_lang';
const DEFAULT_LANG: Language = 'pt-BR';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  private readonly _current = signal<Language>(this.readInitial());
  readonly current = this._current.asReadonly();

  readonly available: ReadonlyArray<{ code: Language; labelKey: string }> = [
    { code: 'pt-BR', labelKey: 'language.pt' },
    { code: 'en', labelKey: 'language.en' },
  ];

  constructor() {
    registerLocaleData(localePt, 'pt-BR');
    registerLocaleData(localeEn, 'en');
    this.translate.addLangs(['pt-BR', 'en']);
    this.translate.setFallbackLang('pt-BR');
    queueMicrotask(() => this.translate.use(this._current()));
  }

  use(lang: Language): void {
    this._current.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    this.translate.use(lang);
  }

  private readInitial(): Language {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'pt-BR' || stored === 'en' ? stored : DEFAULT_LANG;
  }
}
