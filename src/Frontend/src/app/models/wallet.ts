export enum CardType {
  Debit = 1,
  Credit = 2,
}

export interface BankAccountDto {
  id: string;
  name: string;
  balance: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CardDto {
  id: string;
  name: string;
  creditLimit: number;
  availableLimit: number;
  type: CardType;
  linkedBankAccountId: string | null;
  linkedBankAccountName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateBankAccountRequest {
  name: string;
  balance: number;
}

export interface UpdateBankAccountRequest {
  name: string;
}

export interface CreateCardRequest {
  name: string;
  creditLimit: number;
  type: CardType;
  linkedBankAccountId: string | null;
}

export interface UpdateCardRequest {
  name: string;
  type: CardType;
  creditLimit: number;
  linkedBankAccountId: string | null;
}
