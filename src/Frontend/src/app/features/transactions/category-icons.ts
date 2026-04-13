import { TransactionCategory } from '../../models/transaction';

export interface CategoryIcon {
  readonly pathD: string;
  readonly tint: string;
}

// Feather-style 24×24 single-path icons, rendered with stroke="currentColor".
const WALLET =
  'M20 12V8H6a2 2 0 0 1 0-4h12v4 M4 6v12a2 2 0 0 0 2 2h14v-4 M18 12a2 2 0 0 0 0 4h4v-4Z';
const TRENDING_UP = 'M23 6l-9.5 9.5-5-5L1 18 M17 6h6v6';
const GIFT = 'M20 12v10H4V12 M2 7h20v5H2z M12 22V7 M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7z M12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7z';
const HOME = 'M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M9 22V12h6v10';
const BOOK = 'M4 19.5A2.5 2.5 0 0 1 6.5 17H20 M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z';
const SMARTPHONE = 'M5 2h14a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z M12 18h.01';
const PLANE = 'M17.8 19.2L16 11l3.5-3.5C21 6 21.5 4 21 3c-1-.5-3 0-4.5 1.5L13 8 4.8 6.2c-.5-.1-.9.1-1.1.5l-.3.5c-.2.5-.1 1 .3 1.3L9 12l-2 3H4l-1 1 3 2 2 3 1-1v-3l3-2 3.5 5.3c.3.4.8.5 1.3.3l.5-.2c.4-.3.6-.7.5-1.2z';
const UTENSILS = 'M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2 M7 2v20 M21 15V2v0a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h3zm0 0v7';
const HEART = 'M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z';
const TOOL = 'M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z';
const CAR = 'M5 17h14 M3 17V9l2-5h14l2 5v8 M5 17v3 M19 17v3 M7 13h.01 M17 13h.01';
const SHIRT = 'M20.38 3.46L16 2a4 4 0 0 1-8 0L3.62 3.46a2 2 0 0 0-1.34 2.23l.58 3.47a1 1 0 0 0 .99.84H6v10c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2V10h2.15a1 1 0 0 0 .99-.84l.58-3.47a2 2 0 0 0-1.34-2.23z';
const SHOPPING_CART = 'M9 20a1 1 0 1 1-2 0 1 1 0 0 1 2 0z M20 20a1 1 0 1 1-2 0 1 1 0 0 1 2 0z M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6';
const TAG = 'M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z M7 7h.01';

const INCOME = '#22c55e';
const VIOLET = '#a78bfa';
const PINK = '#ec4899';
const BLUE = '#3b82f6';
const ORANGE = '#f59e0b';
const TEAL = '#14b8a6';
const RED = '#ef4444';
const CYAN = '#06b6d4';
const LIME = '#84cc16';
const SKY = '#0ea5e9';

export const CATEGORY_ICONS: Record<TransactionCategory, CategoryIcon> = {
  [TransactionCategory.Salary]: { pathD: WALLET, tint: INCOME },
  [TransactionCategory.Bonus]: { pathD: WALLET, tint: INCOME },
  [TransactionCategory.Loan]: { pathD: GIFT, tint: VIOLET },
  [TransactionCategory.Investment]: { pathD: TRENDING_UP, tint: INCOME },
  [TransactionCategory.ExtraIncome]: { pathD: WALLET, tint: INCOME },
  [TransactionCategory.Gift]: { pathD: GIFT, tint: PINK },
  [TransactionCategory.BankTransfer]: { pathD: WALLET, tint: INCOME },
  [TransactionCategory.OtherIncome]: { pathD: WALLET, tint: INCOME },
  [TransactionCategory.Housing]: { pathD: HOME, tint: ORANGE },
  [TransactionCategory.Education]: { pathD: BOOK, tint: BLUE },
  [TransactionCategory.Electronics]: { pathD: SMARTPHONE, tint: CYAN },
  [TransactionCategory.Leisure]: { pathD: PLANE, tint: SKY },
  [TransactionCategory.OtherExpense]: { pathD: TAG, tint: VIOLET },
  [TransactionCategory.Restaurant]: { pathD: UTENSILS, tint: ORANGE },
  [TransactionCategory.Health]: { pathD: HEART, tint: RED },
  [TransactionCategory.Services]: { pathD: TOOL, tint: TEAL },
  [TransactionCategory.Grocery]: { pathD: SHOPPING_CART, tint: LIME },
  [TransactionCategory.Transportation]: { pathD: CAR, tint: BLUE },
  [TransactionCategory.Clothing]: { pathD: SHIRT, tint: PINK },
  [TransactionCategory.Travel]: { pathD: PLANE, tint: SKY },
};
