import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);

  protected onSubmit(): void {
    this.error.set(null);
    this.loading.set(true);

    this.authService.login({ email: this.email(), password: this.password() }).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.loading.set(false);
        const detail = err.error?.title ?? 'Invalid email or password.';
        this.error.set(detail);
      },
    });
  }
}
