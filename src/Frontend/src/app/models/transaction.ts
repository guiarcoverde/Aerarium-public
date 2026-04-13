export enum TransactionType {
  Income = 1,
  Expense = 2,
}

export enum Recurrence {
  None = 0,
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
  Yearly = 4,
}

export enum TransactionCategory {
  Salary = 100,
  Bonus = 101,
  Loan = 102,
  Investment = 103,
  ExtraIncome = 104,
  Gift = 105,
  BankTransfer = 107,
  OtherIncome = 108,
  Housing = 200,
  Education = 201,
  Electronics = 202,
  Leisure = 203,
  OtherExpense = 204,
  Restaurant = 205,
  Health = 206,
  Services = 207,
  Grocery = 208,
  Transportation = 209,
  Clothing = 210,
  Travel = 211,
}

export enum SalaryScheduleMode {
  BusinessDay = 1,
  FixedDate = 2,
  FixedDateSplit = 3,
}

export enum PaymentMethod {
  Pix = 1,
  Credit = 2,
  Debit = 3,
}

export interface PaymentMethodOption {
  value: PaymentMethod;
  label: string;
}

export const PAYMENT_METHODS: PaymentMethodOption[] = [
  { value: PaymentMethod.Pix, label: 'Pix' },
  { value: PaymentMethod.Credit, label: 'Credit' },
  { value: PaymentMethod.Debit, label: 'Debit' },
];

export interface CategoryOption {
  value: TransactionCategory;
  label: string;
}

export const INCOME_CATEGORIES: CategoryOption[] = [
  { value: TransactionCategory.Salary, label: 'transactions.categories.salary' },
  { value: TransactionCategory.Bonus, label: 'transactions.categories.bonus' },
  { value: TransactionCategory.Loan, label: 'transactions.categories.loan' },
  { value: TransactionCategory.Investment, label: 'transactions.categories.investment' },
  { value: TransactionCategory.ExtraIncome, label: 'transactions.categories.extraIncome' },
  { value: TransactionCategory.Gift, label: 'transactions.categories.gift' },
  { value: TransactionCategory.BankTransfer, label: 'transactions.categories.bankTransfer' },
  { value: TransactionCategory.OtherIncome, label: 'transactions.categories.otherIncome' },
];

export const EXPENSE_CATEGORIES: CategoryOption[] = [
  { value: TransactionCategory.Housing, label: 'transactions.categories.housing' },
  { value: TransactionCategory.Education, label: 'transactions.categories.education' },
  { value: TransactionCategory.Electronics, label: 'transactions.categories.electronics' },
  { value: TransactionCategory.Leisure, label: 'transactions.categories.leisure' },
  { value: TransactionCategory.OtherExpense, label: 'transactions.categories.otherExpense' },
  { value: TransactionCategory.Restaurant, label: 'transactions.categories.restaurant' },
  { value: TransactionCategory.Health, label: 'transactions.categories.health' },
  { value: TransactionCategory.Services, label: 'transactions.categories.services' },
  { value: TransactionCategory.Grocery, label: 'transactions.categories.grocery' },
  { value: TransactionCategory.Transportation, label: 'transactions.categories.transportation' },
  { value: TransactionCategory.Clothing, label: 'transactions.categories.clothing' },
  { value: TransactionCategory.Travel, label: 'transactions.categories.travel' },
];

export interface SalaryScheduleRequest {
  mode: SalaryScheduleMode;
  businessDayNumber?: number | null;
  fixedDay?: number | null;
  splitFirstAmount?: number | null;
  splitFirstPercentage?: number | null;
}

export interface CreateTransactionRequest {
  amount: number;
  description: string;
  date: string;
  type: TransactionType;
  category: TransactionCategory;
  recurrence: Recurrence;
  recurrenceEndDate?: string | null;
  recurrenceCount?: number | null;
  salarySchedule?: SalaryScheduleRequest | null;
  paymentMethod?: PaymentMethod | null;
  bankAccountId?: string | null;
  cardId?: string | null;
}

export interface SalaryScheduleDto {
  mode: SalaryScheduleMode;
  businessDayNumber: number | null;
  fixedDay: number | null;
  splitFirstAmount: number | null;
  splitFirstPercentage: number | null;
}

export interface TransactionDto {
  id: string;
  amount: number;
  description: string;
  date: string;
  type: TransactionType;
  category: TransactionCategory;
  categoryDisplayName: string;
  paymentMethod: PaymentMethod | null;
  paymentMethodDisplayName: string | null;
  recurrence: Recurrence;
  recurrenceGroupId: string | null;
  recurrenceEndDate: string | null;
  recurrenceCount: number | null;
  salarySchedule: SalaryScheduleDto | null;
  bankAccountId: string | null;
  bankAccountName: string | null;
  cardId: string | null;
  cardName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CategoryBreakdownDto {
  category: number;
  categoryDisplayName: string;
  total: number;
  count: number;
}

export interface DashboardSummaryDto {
  totalIncome: number;
  totalExpenses: number;
  balance: number;
  totalInvested?: number;
  incomeByCategory: CategoryBreakdownDto[];
  expenseByCategory: CategoryBreakdownDto[];
}
