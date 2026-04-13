export enum InvestmentType {
  Stocks = 1,
  FixedIncome = 2,
  Crypto = 3,
  RealEstate = 4,
  FundShares = 5,
  Other = 99,
}

export interface InvestmentDto {
  id: string;
  name: string;
  type: InvestmentType;
  typeDisplayName: string;
  initialAmount: number;
  currentValue: number;
  purchaseDate: string;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateInvestmentRequest {
  name: string;
  type: InvestmentType;
  initialAmount: number;
  currentValue: number;
  purchaseDate: string;
  notes?: string | null;
}
