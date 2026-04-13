import { Routes, TitleStrategy } from '@angular/router';
import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { authGuard, guestGuard } from './core/guards/auth.guard';

@Injectable({ providedIn: 'root' })
export class AerariumTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly translate = inject(TranslateService);
  private lastSnapshot: RouterStateSnapshot | null = null;

  constructor() {
    super();
    this.translate.onLangChange.subscribe(() => {
      if (this.lastSnapshot) this.updateTitle(this.lastSnapshot);
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.lastSnapshot = snapshot;
    const key = this.buildTitle(snapshot);
    if (!key) {
      this.title.setTitle('Aerarium');
      return;
    }
    const translated = this.translate.instant(key);
    this.title.setTitle(`Aerarium • ${translated}`);
  }
}

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/layout/layout').then((m) => m.Layout),
    children: [
      {
        path: '',
        title: 'pageTitles.dashboard',
        loadComponent: () =>
          import('./features/dashboard/components/dashboard/dashboard').then(
            (m) => m.Dashboard,
          ),
      },
      {
        path: 'transactions',
        title: 'pageTitles.transactions',
        loadComponent: () =>
          import(
            './features/transactions/components/list-transactions/list-transactions'
          ).then((m) => m.ListTransactions),
      },
      {
        path: 'investments',
        title: 'pageTitles.investments',
        loadComponent: () =>
          import(
            './features/investments/components/list-investments/list-investments'
          ).then((m) => m.ListInvestments),
      },
      {
        path: 'wallet',
        title: 'pageTitles.wallet',
        loadComponent: () =>
          import(
            './features/wallet/components/wallet-page/wallet-page'
          ).then((m) => m.WalletPage),
      },
      {
        path: 'settings',
        title: 'pageTitles.settings',
        loadComponent: () =>
          import('./features/settings/components/settings/settings').then(
            (m) => m.Settings,
          ),
      },
    ],
  },
  {
    path: 'login',
    title: 'pageTitles.login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/components/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'pageTitles.register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/components/register/register').then(
        (m) => m.Register,
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
