import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { Language, LanguageService } from '../../core/services/language.service';
import { UserProfileService } from '../../core/services/user-profile.service';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout {
  protected readonly authService = inject(AuthService);
  protected readonly language = inject(LanguageService);
  private readonly userProfileService = inject(UserProfileService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly firstName = signal<string | null>(null);

  protected readonly displayName = computed(() => {
    const name = this.firstName();
    if (name && name.trim().length > 0) return name;
    return this.authService.email() ?? '';
  });

  protected readonly initials = computed(() => {
    const source = this.displayName();
    const base = source.includes('@') ? (source.split('@')[0] ?? '') : source;
    return base.slice(0, 2).toUpperCase() || '?';
  });

  constructor() {
    this.userProfileService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => this.firstName.set(profile.firstName),
        error: () => {},
      });
  }

  protected logout(): void {
    this.authService.logout();
  }

  protected onLanguageChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as Language;
    this.language.use(value);
  }
}
