import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration, ChartData, ChartOptions } from 'chart.js';
import { DashboardService } from '../../../../core/services/dashboard.service';
import { TransactionService } from '../../../../core/services/transaction.service';
import { DashboardSummaryDto, TransactionDto } from '../../../../models/transaction';

interface MockWallet {
  readonly label: string;
  readonly bank: string;
  readonly number: string;
  readonly expiry: string;
  readonly brand: 'mastercard' | 'visa';
}

const EXPENSE_PALETTE = [
  '#a78bfa',
  '#c4b5fd',
  '#f472b6',
  '#fb923c',
  '#facc15',
  '#4ade80',
  '#22d3ee',
  '#60a5fa',
  '#f87171',
  '#a3e635',
  '#e879f9',
  '#34d399',
];

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, RouterLink, BaseChartDirective, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly transactionService = inject(TransactionService);
  private readonly translate = inject(TranslateService);

  protected readonly month = signal(new Date().getMonth() + 1);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly summary = signal<DashboardSummaryDto | null>(null);
  protected readonly recentTransactions = signal<TransactionDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  // TODO: remove once the backend exposes `totalInvested` on DashboardSummaryDto.
  protected readonly mockTotalInvested = 550.25;

  protected readonly mockWallets: readonly MockWallet[] = [
    {
      label: 'Aerarium',
      bank: 'Universal Bank',
      number: '5495 7381 3759 2321',
      expiry: '09/25',
      brand: 'mastercard',
    },
    {
      label: 'Aerarium',
      bank: 'Commercial Bank',
      number: '8595 2548 **** ****',
      expiry: '09/25',
      brand: 'visa',
    },
  ];

  protected readonly expenseBreakdown = computed(() => {
    const buckets = this.summary()?.expenseByCategory ?? [];
    return [...buckets]
      .filter((b) => b.total > 0)
      .sort((a, b) => b.total - a.total);
  });

  protected readonly hasExpenses = computed(() => this.expenseBreakdown().length > 0);

  protected readonly chartData = computed<ChartData<'doughnut'>>(() => {
    const breakdown = this.expenseBreakdown();
    return {
      labels: breakdown.map((b) => b.categoryDisplayName),
      datasets: [
        {
          data: breakdown.map((b) => b.total),
          backgroundColor: breakdown.map((_, i) => EXPENSE_PALETTE[i % EXPENSE_PALETTE.length]),
          borderColor: '#1d1a33',
          borderWidth: 2,
          hoverOffset: 8,
        },
      ],
    };
  });

  protected readonly chartOptions: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '62%',
    plugins: {
      legend: {
        position: 'right',
        labels: {
          color: '#9b95a8',
          padding: 12,
          boxWidth: 12,
          boxHeight: 12,
          usePointStyle: true,
          pointStyle: 'circle',
        },
      },
      tooltip: {
        backgroundColor: '#1d1a33',
        borderColor: '#352e4a',
        borderWidth: 1,
        padding: 10,
        titleColor: '#f1f0f5',
        bodyColor: '#9b95a8',
        callbacks: {
          label: (ctx) => {
            const value = Number(ctx.parsed) || 0;
            const formatted = new Intl.NumberFormat(this.translate.currentLang ?? 'pt-BR', {
              style: 'currency',
              currency: 'BRL',
            }).format(value);
            return ` ${ctx.label}: ${formatted}`;
          },
        },
      },
    },
  };

  protected readonly chartType: ChartConfiguration<'doughnut'>['type'] = 'doughnut';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    this.error.set('');

    this.dashboardService.getSummary(this.month(), this.year()).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.detail ?? 'Failed to load dashboard data');
        this.loading.set(false);
      },
    });

    this.transactionService.list(1, 5, this.month(), this.year()).subscribe({
      next: (data) => this.recentTransactions.set(data.items),
    });
  }
}
