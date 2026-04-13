import { Component, inject } from '@angular/core';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly authService = inject(AuthService);

  protected logout(): void {
    this.authService.logout();
  }
}
