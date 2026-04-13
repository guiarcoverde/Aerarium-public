import { Component, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-list-investments',
  imports: [TranslatePipe],
  templateUrl: './list-investments.html',
  styleUrl: './list-investments.scss',
})
export class ListInvestments {
  protected readonly investments = signal<readonly never[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
}
