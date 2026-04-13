import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { UpdateProfileRequest, UserProfileResponse } from '../../../../models/user-profile';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  private readonly userProfileService = inject(UserProfileService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly editing = signal(false);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly firstName = signal('');
  protected readonly lastName = signal('');
  protected readonly dateOfBirth = signal('');
  protected readonly mobileNumber = signal('');
  protected readonly email = signal('');

  private snapshot: UserProfileResponse | null = null;

  constructor() {
    this.loadProfile();
  }

  protected enterEdit(): void {
    this.error.set(null);
    this.successMessage.set(null);
    this.editing.set(true);
  }

  protected cancel(): void {
    if (this.snapshot) {
      this.applyProfile(this.snapshot);
    }
    this.error.set(null);
    this.editing.set(false);
  }

  protected save(): void {
    if (this.saving()) return;

    this.saving.set(true);
    this.error.set(null);
    this.successMessage.set(null);

    const request: UpdateProfileRequest = {
      firstName: this.toNullable(this.firstName()),
      lastName: this.toNullable(this.lastName()),
      dateOfBirth: this.toNullable(this.dateOfBirth()),
      phoneNumber: this.toNullable(this.mobileNumber()),
    };

    this.userProfileService
      .updateProfile(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.snapshot = profile;
          this.applyProfile(profile);
          this.saving.set(false);
          this.editing.set(false);
          this.successMessage.set('settings.saveSuccess');
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.error.set(this.extractError(err));
        },
      });
  }

  private loadProfile(): void {
    this.loading.set(true);
    this.userProfileService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.snapshot = profile;
          this.applyProfile(profile);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(this.extractError(err));
        },
      });
  }

  private applyProfile(profile: UserProfileResponse): void {
    this.firstName.set(profile.firstName ?? '');
    this.lastName.set(profile.lastName ?? '');
    this.dateOfBirth.set(profile.dateOfBirth ?? '');
    this.mobileNumber.set(profile.phoneNumber ?? '');
    this.email.set(profile.email ?? '');
  }

  private toNullable(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  }

  private extractError(err: HttpErrorResponse): string {
    const problem = err.error;
    if (problem && typeof problem === 'object') {
      if (problem.errors && typeof problem.errors === 'object') {
        const messages = Object.values(problem.errors as Record<string, string[]>).flat();
        if (messages.length > 0) return messages.join(' ');
      }
      if (typeof problem.title === 'string') return problem.title;
    }
    return 'settings.saveError';
  }
}
