import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../models/auth';

const TOKEN_KEY = 'aerarium_token';
const EMAIL_KEY = 'aerarium_email';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _token = signal<string | null>(sessionStorage.getItem(TOKEN_KEY));
  private readonly _email = signal<string | null>(sessionStorage.getItem(EMAIL_KEY));

  readonly isAuthenticated = computed(() => !!this._token());
  readonly email = computed(() => this._email());
  readonly token = computed(() => this._token());

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, request).pipe(
      tap((response) => this.setSession(response)),
    );
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.setSession(response)),
    );
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(EMAIL_KEY);
    this._token.set(null);
    this._email.set(null);
    this.router.navigate(['/login']);
  }

  private setSession(response: AuthResponse): void {
    sessionStorage.setItem(TOKEN_KEY, response.accessToken);
    sessionStorage.setItem(EMAIL_KEY, response.email);
    this._token.set(response.accessToken);
    this._email.set(response.email);
  }
}
