import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BankAccountDto,
  CardDto,
  CreateBankAccountRequest,
  CreateCardRequest,
  UpdateBankAccountRequest,
  UpdateCardRequest,
} from '../../models/wallet';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly http = inject(HttpClient);
  private readonly banksUrl = `${environment.apiUrl}/bank-accounts`;
  private readonly cardsUrl = `${environment.apiUrl}/cards`;

  readonly bankAccounts = signal<BankAccountDto[]>([]);
  readonly cards = signal<CardDto[]>([]);
  readonly loading = signal(false);

  loadBankAccounts(): Observable<BankAccountDto[]> {
    return this.http.get<BankAccountDto[]>(this.banksUrl).pipe(
      tap((accounts) => this.bankAccounts.set(accounts)),
    );
  }

  loadCards(): Observable<CardDto[]> {
    return this.http.get<CardDto[]>(this.cardsUrl).pipe(
      tap((cards) => this.cards.set(cards)),
    );
  }

  createBankAccount(request: CreateBankAccountRequest): Observable<BankAccountDto> {
    return this.http.post<BankAccountDto>(this.banksUrl, request);
  }

  updateBankAccount(id: string, request: UpdateBankAccountRequest): Observable<BankAccountDto> {
    return this.http.put<BankAccountDto>(`${this.banksUrl}/${id}`, request);
  }

  deleteBankAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.banksUrl}/${id}`);
  }

  createCard(request: CreateCardRequest): Observable<CardDto> {
    return this.http.post<CardDto>(this.cardsUrl, request);
  }

  updateCard(id: string, request: UpdateCardRequest): Observable<CardDto> {
    return this.http.put<CardDto>(`${this.cardsUrl}/${id}`, request);
  }

  deleteCard(id: string): Observable<void> {
    return this.http.delete<void>(`${this.cardsUrl}/${id}`);
  }
}
