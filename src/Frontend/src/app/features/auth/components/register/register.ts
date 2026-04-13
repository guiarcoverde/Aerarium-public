import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);

  protected onSubmit(): void {
    this.error.set(null);

    if (this.password() !== this.confirmPassword()) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.loading.set(true);

    this.authService.register({ email: this.email(), password: this.password() }).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.loading.set(false);
        const detail = err.error?.errors?.Identity?.join(', ') ?? err.error?.title ?? 'Registration failed.';
        this.error.set(detail);
      },
    });
  }
}
