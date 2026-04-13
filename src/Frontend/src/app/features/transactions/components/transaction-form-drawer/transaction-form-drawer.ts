import {
  Component,
  computed,
  DestroyRef,
  effect,
  HostListener,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { TransactionService } from '../../../../core/services/transaction.service';
import { WalletService } from '../../../../core/services/wallet.service';
import {
  CreateTransactionRequest,
  EXPENSE_CATEGORIES,
  INCOME_CATEGORIES,
  PaymentMethod,
  PAYMENT_METHODS,
  Recurrence,
  SalaryScheduleMode,
  TransactionCategory,
  TransactionDto,
  TransactionType,
} from '../../../../models/transaction';
import { BankAccountDto, CardDto, CardType } from '../../../../models/wallet';

export type DrawerMode = 'create' | 'edit';

@Component({
  selector: 'app-transaction-form-drawer',
  imports: [CurrencyPipe, ReactiveFormsModule, TranslatePipe],
  templateUrl: './transaction-form-drawer.html',
  styleUrl: './transaction-form-drawer.scss',
})
export class TransactionFormDrawer {
  private readonly transactionService = inject(TransactionService);
  private readonly walletService = inject(WalletService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly bankAccounts = signal<BankAccountDto[]>([]);
  protected readonly cards = signal<CardDto[]>([]);

  readonly open = input<boolean>(false);
  readonly mode = input<DrawerMode>('create');
  readonly transaction = input<TransactionDto | null>(null);

  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly recurringEnabled = signal(false);

  protected readonly form = new FormGroup({
    type: new FormControl<TransactionType | null>(null, Validators.required),
    category: new FormControl<TransactionCategory | null>(
      null,
      Validators.required,
    ),
    amount: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0.01),
    ]),
    description: new FormControl<string>('', [
      Validators.required,
      Validators.maxLength(500),
    ]),
    date: new FormControl<string>('', Validators.required),
    recurrence: new FormControl<Recurrence>(Recurrence.None, {
      nonNullable: true,
    }),
    recurrenceEndDate: new FormControl<string | null>(null),
    recurrenceCount: new FormControl<number | null>(null, Validators.min(1)),
    salaryScheduleMode: new FormControl<SalaryScheduleMode | null>(null),
    businessDayNumber: new FormControl<number | null>(null, [
      Validators.min(1),
      Validators.max(23),
    ]),
    fixedDay: new FormControl<number | null>(null, [
      Validators.min(1),
      Validators.max(31),
    ]),
    splitFirstAmount: new FormControl<number | null>(null, Validators.min(0.01)),
    splitFirstPercentage: new FormControl<number | null>(null, [
      Validators.min(0.01),
      Validators.max(100),
    ]),
    paymentMethod: new FormControl<PaymentMethod | null>(null),
    bankAccountId: new FormControl<string | null>(null),
    cardId: new FormControl<string | null>(null),
  });

  private readonly typeValue = toSignal(this.form.controls.type.valueChanges);
  private readonly recurrenceValue = toSignal(
    this.form.controls.recurrence.valueChanges,
    { initialValue: Recurrence.None },
  );
  private readonly categoryValue = toSignal(
    this.form.controls.category.valueChanges,
  );
  private readonly salaryModeValue = toSignal(
    this.form.controls.salaryScheduleMode.valueChanges,
  );
  private readonly paymentMethodValue = toSignal(
    this.form.controls.paymentMethod.valueChanges,
  );

  protected readonly filteredCategories = computed(() => {
    const type = this.typeValue();
    if (type === TransactionType.Income) return INCOME_CATEGORIES;
    if (type === TransactionType.Expense) return EXPENSE_CATEGORIES;
    return [];
  });

  protected readonly showSalarySchedule = computed(
    () =>
      this.categoryValue() === TransactionCategory.Salary &&
      this.recurrenceValue() === Recurrence.Monthly,
  );

  protected readonly salaryMode = computed(() => this.salaryModeValue());

  protected readonly showPaymentMethod = computed(
    () => this.typeValue() === TransactionType.Expense,
  );

  protected readonly currentType = computed(() => this.typeValue() ?? null);
  protected readonly currentPaymentMethod = computed(
    () => this.paymentMethodValue() ?? null,
  );

  protected readonly showBankSelect = computed(() => {
    const type = this.typeValue();
    const pm = this.paymentMethodValue();
    return type === TransactionType.Income || (type === TransactionType.Expense && pm === PaymentMethod.Pix);
  });

  protected readonly showCardSelect = computed(() => {
    const type = this.typeValue();
    const pm = this.paymentMethodValue();
    return type === TransactionType.Expense && (pm === PaymentMethod.Credit || pm === PaymentMethod.Debit);
  });

  protected readonly filteredCards = computed(() => {
    const pm = this.paymentMethodValue();
    const all = this.cards();
    if (pm === PaymentMethod.Credit) return all.filter((c) => (c.type & CardType.Credit) !== 0);
    if (pm === PaymentMethod.Debit) return all.filter((c) => (c.type & CardType.Debit) !== 0);
    return all;
  });

  protected readonly paymentMethods = PAYMENT_METHODS;

  protected readonly TransactionType = TransactionType;
  protected readonly Recurrence = Recurrence;
  protected readonly SalaryScheduleMode = SalaryScheduleMode;

  /** Tracks whether we are in the middle of programmatic patching to avoid resetting fields. */
  private patching = false;

  constructor() {
    this.form.controls.type.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        if (!this.patching) {
          this.form.controls.category.reset();
          this.form.controls.bankAccountId.reset();
          this.form.controls.cardId.reset();
        }
        if (val !== TransactionType.Expense) {
          if (!this.patching) this.form.controls.paymentMethod.reset();
          this.form.controls.paymentMethod.clearValidators();
        } else {
          this.form.controls.paymentMethod.setValidators(Validators.required);
        }
        this.form.controls.paymentMethod.updateValueAndValidity({ emitEvent: false });
      });

    this.form.controls.paymentMethod.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (!this.patching) {
          this.form.controls.bankAccountId.reset();
          this.form.controls.cardId.reset();
        }
      });

    // React to open/mode/transaction changes: reset or populate the form.
    effect(() => {
      const isOpen = this.open();
      if (!isOpen) return;

      const mode = this.mode();
      const tx = this.transaction();

      this.error.set('');
      this.submitting.set(false);
      this.loadWalletData();

      if (mode === 'edit' && tx) {
        this.populateForm(tx);
        this.recurringEnabled.set(tx.recurrence !== Recurrence.None);
      } else {
        this.recurringEnabled.set(false);
        this.form.reset({ recurrence: Recurrence.None });
      }
    });
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.open()) this.requestClose();
  }

  protected requestClose(): void {
    if (this.submitting()) return;
    this.closed.emit();
  }

  protected setType(type: TransactionType): void {
    this.form.controls.type.setValue(type);
    this.form.controls.type.markAsTouched();
  }

  protected setPaymentMethod(pm: PaymentMethod): void {
    this.form.controls.paymentMethod.setValue(pm);
    this.form.controls.paymentMethod.markAsTouched();
  }

  protected toggleRecurring(): void {
    const next = !this.recurringEnabled();
    this.recurringEnabled.set(next);
    if (next) {
      if (this.form.controls.recurrence.value === Recurrence.None) {
        this.form.controls.recurrence.setValue(Recurrence.Monthly);
      }
    } else {
      this.form.controls.recurrence.setValue(Recurrence.None);
      this.form.controls.recurrenceEndDate.reset();
      this.form.controls.recurrenceCount.reset();
      this.form.controls.salaryScheduleMode.reset();
      this.form.controls.businessDayNumber.reset();
      this.form.controls.fixedDay.reset();
      this.form.controls.splitFirstAmount.reset();
      this.form.controls.splitFirstPercentage.reset();
    }
  }

  protected onSubmit(): void {
    this.form.markAllAsTouched();
    this.error.set('');

    if (this.form.invalid) return;

    const f = this.form.getRawValue();
    if (
      f.recurrence !== Recurrence.None &&
      !f.recurrenceEndDate &&
      !f.recurrenceCount
    ) {
      this.error.set(
        'Provide either an end date or occurrence count for recurring transactions.',
      );
      return;
    }

    const request: CreateTransactionRequest = {
      amount: f.amount!,
      description: f.description!.trim(),
      date: f.date!,
      type: f.type!,
      category: f.category!,
      recurrence: f.recurrence,
      recurrenceEndDate:
        f.recurrence !== Recurrence.None ? (f.recurrenceEndDate ?? null) : null,
      recurrenceCount:
        f.recurrence !== Recurrence.None ? (f.recurrenceCount ?? null) : null,
      salarySchedule: this.showSalarySchedule()
        ? {
            mode: f.salaryScheduleMode!,
            businessDayNumber:
              f.salaryScheduleMode === SalaryScheduleMode.BusinessDay
                ? f.businessDayNumber
                : null,
            fixedDay:
              f.salaryScheduleMode === SalaryScheduleMode.FixedDate ||
              f.salaryScheduleMode === SalaryScheduleMode.FixedDateSplit
                ? f.fixedDay
                : null,
            splitFirstAmount:
              f.salaryScheduleMode === SalaryScheduleMode.FixedDateSplit
                ? f.splitFirstAmount
                : null,
            splitFirstPercentage:
              f.salaryScheduleMode === SalaryScheduleMode.FixedDateSplit
                ? f.splitFirstPercentage
                : null,
          }
        : null,
      paymentMethod: f.type === TransactionType.Expense ? f.paymentMethod : null,
      bankAccountId: f.bankAccountId ?? null,
      cardId: f.cardId ?? null,
    };

    this.submitting.set(true);

    const tx = this.transaction();
    const obs =
      this.mode() === 'edit' && tx
        ? this.transactionService.update(tx.id, request)
        : this.transactionService.create(request);

    obs.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.emit();
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(
          err.error?.detail ?? err.error?.title ?? 'Failed to save transaction.',
        );
      },
    });
  }

  private populateForm(tx: TransactionDto): void {
    this.patching = true;
    try {
      this.form.reset({ recurrence: tx.recurrence });
      this.form.patchValue({
        type: tx.type,
        category: tx.category,
        amount: tx.amount,
        description: tx.description,
        date: tx.date,
        recurrence: tx.recurrence,
        recurrenceEndDate: tx.recurrenceEndDate,
        recurrenceCount: tx.recurrenceCount,
        salaryScheduleMode: tx.salarySchedule?.mode ?? null,
        businessDayNumber: tx.salarySchedule?.businessDayNumber ?? null,
        fixedDay: tx.salarySchedule?.fixedDay ?? null,
        splitFirstAmount: tx.salarySchedule?.splitFirstAmount ?? null,
        splitFirstPercentage: tx.salarySchedule?.splitFirstPercentage ?? null,
        paymentMethod: tx.paymentMethod,
        bankAccountId: tx.bankAccountId,
        cardId: tx.cardId,
      });
    } finally {
      this.patching = false;
    }
  }

  private loadWalletData(): void {
    this.walletService
      .loadBankAccounts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((banks) => this.bankAccounts.set(banks));

    this.walletService
      .loadCards()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((cards) => this.cards.set(cards));
  }
}
