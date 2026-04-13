import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { WalletService } from '../../../../core/services/wallet.service';
import { BankAccountDto, CardDto, CardType } from '../../../../models/wallet';

@Component({
  selector: 'app-wallet-page',
  imports: [CurrencyPipe, TranslatePipe],
  templateUrl: './wallet-page.html',
  styleUrl: './wallet-page.scss',
})
export class WalletPage implements OnInit {
  private readonly walletService = inject(WalletService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly bankAccounts = signal<BankAccountDto[]>([]);
  protected readonly cards = signal<CardDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected readonly editingBank = signal<BankAccountDto | null>(null);
  protected readonly editingCard = signal<CardDto | null>(null);
  protected readonly showBankForm = signal(false);
  protected readonly showCardForm = signal(false);

  protected readonly bankFormName = signal('');
  protected readonly bankFormBalance = signal<number>(0);
  protected readonly bankFormSaving = signal(false);

  protected readonly cardFormName = signal('');
  protected readonly cardFormCreditLimit = signal<number>(0);
  protected readonly cardFormType = signal<CardType>(CardType.Credit);
  protected readonly cardFormLinkedBankId = signal<string | null>(null);
  protected readonly cardFormSaving = signal(false);

  protected readonly CardType = CardType;

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin([
      this.walletService.loadBankAccounts(),
      this.walletService.loadCards(),
    ])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ([banks, cards]) => {
          this.bankAccounts.set(banks);
          this.cards.set(cards);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.detail ?? err.error?.title ?? 'Failed to load wallet data.');
          this.loading.set(false);
        },
      });
  }

  // --- Bank Account CRUD ---

  protected openCreateBank(): void {
    this.editingBank.set(null);
    this.bankFormName.set('');
    this.bankFormBalance.set(0);
    this.showBankForm.set(true);
  }

  protected openEditBank(bank: BankAccountDto): void {
    this.editingBank.set(bank);
    this.bankFormName.set(bank.name);
    this.bankFormBalance.set(bank.balance);
    this.showBankForm.set(true);
  }

  protected closeBankForm(): void {
    this.showBankForm.set(false);
    this.editingBank.set(null);
  }

  protected saveBank(): void {
    const name = this.bankFormName().trim();
    if (!name) return;

    this.bankFormSaving.set(true);
    const editing = this.editingBank();

    const obs$ = editing
      ? this.walletService.updateBankAccount(editing.id, { name })
      : this.walletService.createBankAccount({ name, balance: this.bankFormBalance() });

    obs$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.closeBankForm();
        this.bankFormSaving.set(false);
        this.loadData();
      },
      error: (err) => {
        this.error.set(err.error?.detail ?? err.error?.title ?? 'Failed to save bank account.');
        this.bankFormSaving.set(false);
      },
    });
  }

  protected deleteBank(bank: BankAccountDto): void {
    if (!confirm(`Delete "${bank.name}"?`)) return;

    this.walletService
      .deleteBankAccount(bank.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadData(),
        error: (err) =>
          this.error.set(err.error?.detail ?? err.error?.title ?? 'Failed to delete bank account.'),
      });
  }

  // --- Card CRUD ---

  protected openCreateCard(): void {
    this.editingCard.set(null);
    this.cardFormName.set('');
    this.cardFormCreditLimit.set(0);
    this.cardFormType.set(CardType.Credit);
    this.cardFormLinkedBankId.set(null);
    this.showCardForm.set(true);
  }

  protected openEditCard(card: CardDto): void {
    this.editingCard.set(card);
    this.cardFormName.set(card.name);
    this.cardFormCreditLimit.set(card.creditLimit);
    this.cardFormType.set(card.type);
    this.cardFormLinkedBankId.set(card.linkedBankAccountId);
    this.showCardForm.set(true);
  }

  protected closeCardForm(): void {
    this.showCardForm.set(false);
    this.editingCard.set(null);
  }

  protected saveCard(): void {
    const name = this.cardFormName().trim();
    if (!name) return;

    this.cardFormSaving.set(true);
    const editing = this.editingCard();
    const type = this.cardFormType();
    const creditLimit = this.cardFormCreditLimit();
    const linkedBankAccountId = this.cardFormLinkedBankId();

    const obs$ = editing
      ? this.walletService.updateCard(editing.id, { name, type, creditLimit, linkedBankAccountId })
      : this.walletService.createCard({ name, type, creditLimit, linkedBankAccountId });

    obs$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.closeCardForm();
        this.cardFormSaving.set(false);
        this.loadData();
      },
      error: (err) => {
        this.error.set(err.error?.detail ?? err.error?.title ?? 'Failed to save card.');
        this.cardFormSaving.set(false);
      },
    });
  }

  protected deleteCard(card: CardDto): void {
    if (!confirm(`Delete "${card.name}"?`)) return;

    this.walletService
      .deleteCard(card.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadData(),
        error: (err) =>
          this.error.set(err.error?.detail ?? err.error?.title ?? 'Failed to delete card.'),
      });
  }

  protected cardTypeLabel(type: CardType): string {
    if (type === (CardType.Debit | CardType.Credit)) return 'wallet.cardType.both';
    if (type === CardType.Debit) return 'wallet.cardType.debit';
    return 'wallet.cardType.credit';
  }

  protected isDebit(type: CardType): boolean {
    return (type & CardType.Debit) !== 0;
  }

  protected isCredit(type: CardType): boolean {
    return (type & CardType.Credit) !== 0;
  }

  protected onTypeToggle(flag: CardType, checked: boolean): void {
    let current = this.cardFormType();
    if (checked) {
      current = current | flag;
    } else {
      current = current & ~flag;
    }
    if (!current) return;
    this.cardFormType.set(current);
  }
}
