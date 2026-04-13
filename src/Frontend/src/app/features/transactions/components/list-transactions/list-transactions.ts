import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { TransactionService } from '../../../../core/services/transaction.service';
import {
  EXPENSE_CATEGORIES,
  INCOME_CATEGORIES,
  PagedResult,
  Recurrence,
  TransactionCategory,
  TransactionDto,
  TransactionType,
} from '../../../../models/transaction';
import { CATEGORY_ICONS, CategoryIcon } from '../../category-icons';
import {
  DrawerMode,
  TransactionFormDrawer,
} from '../transaction-form-drawer/transaction-form-drawer';

@Component({
  selector: 'app-list-transactions',
  imports: [CurrencyPipe, DatePipe, FormsModule, TranslatePipe, TransactionFormDrawer],
  templateUrl: './list-transactions.html',
  styleUrl: './list-transactions.scss',
})
export class ListTransactions implements OnInit {
  private readonly transactionService = inject(TransactionService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly transactions = signal<TransactionDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);

  protected readonly filterMonth = signal<number | null>(
    new Date().getMonth() + 1,
  );
  protected readonly filterYear = signal<number | null>(
    new Date().getFullYear(),
  );
  protected readonly filterType = signal<TransactionType | null>(null);
  protected readonly filterCategory = signal<TransactionCategory | null>(null);

  protected readonly drawerOpen = signal(false);
  protected readonly drawerMode = signal<DrawerMode>('create');
  protected readonly editingTx = signal<TransactionDto | null>(null);

  protected readonly categoryOptions = [...INCOME_CATEGORIES, ...EXPENSE_CATEGORIES];

  /** Client-side search over the current page (no backend search endpoint yet). */
  protected readonly searchTerm = signal('');

  protected readonly filteredTransactions = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const rows = this.transactions();
    if (!term) return rows;
    return rows.filter(
      (tx) =>
        tx.description.toLowerCase().includes(term) ||
        tx.categoryDisplayName.toLowerCase().includes(term),
    );
  });

  protected readonly TransactionType = TransactionType;
  protected readonly Recurrence = Recurrence;

  protected get totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize()) || 1;
  }

  ngOnInit(): void {
    this.loadTransactions();
  }

  protected iconFor(category: TransactionCategory): CategoryIcon {
    return CATEGORY_ICONS[category];
  }

  protected openCreate(): void {
    this.editingTx.set(null);
    this.drawerMode.set('create');
    this.drawerOpen.set(true);
  }

  protected openEdit(tx: TransactionDto): void {
    this.editingTx.set(tx);
    this.drawerMode.set('edit');
    this.drawerOpen.set(true);
  }

  protected onDrawerClosed(): void {
    this.drawerOpen.set(false);
  }

  protected onDrawerSaved(): void {
    this.drawerOpen.set(false);
    this.loadTransactions();
  }

  protected onFilterChange(): void {
    this.page.set(1);
    this.loadTransactions();
  }

  protected clearFilters(): void {
    this.filterMonth.set(null);
    this.filterYear.set(null);
    this.filterType.set(null);
    this.filterCategory.set(null);
    this.page.set(1);
    this.loadTransactions();
  }

  protected goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page.set(p);
    this.loadTransactions();
  }

  protected deleteTransaction(tx: TransactionDto): void {
    if (!confirm(`Delete "${tx.description}"?`)) return;

    this.transactionService
      .delete(tx.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadTransactions(),
        error: (err) =>
          this.error.set(
            err.error?.detail ?? err.error?.title ?? 'Failed to delete transaction.',
          ),
      });
  }

  protected deleteSeries(tx: TransactionDto): void {
    if (!tx.recurrenceGroupId) return;
    if (!confirm(`Delete all transactions in this recurring series?`)) return;

    this.transactionService
      .deleteSeries(tx.recurrenceGroupId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadTransactions(),
        error: (err) =>
          this.error.set(
            err.error?.detail ?? err.error?.title ?? 'Failed to delete series.',
          ),
      });
  }

  protected recurrenceLabel(r: Recurrence): string {
    switch (r) {
      case Recurrence.Daily:
        return 'Daily';
      case Recurrence.Weekly:
        return 'Weekly';
      case Recurrence.Monthly:
        return 'Monthly';
      case Recurrence.Yearly:
        return 'Yearly';
      default:
        return '';
    }
  }

  private loadTransactions(): void {
    this.loading.set(true);
    this.error.set('');

    this.transactionService
      .list(
        this.page(),
        this.pageSize(),
        this.filterMonth(),
        this.filterYear(),
        this.filterType(),
        this.filterCategory(),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: PagedResult<TransactionDto>) => {
          this.transactions.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(
            err.error?.detail ?? err.error?.title ?? 'Failed to load transactions.',
          );
          this.loading.set(false);
        },
      });
  }
}
