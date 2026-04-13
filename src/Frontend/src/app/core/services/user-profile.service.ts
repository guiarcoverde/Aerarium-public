import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UpdateProfileRequest, UserProfileResponse } from '../../models/user-profile';

@Injectable({ providedIn: 'root' })
export class UserProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.baseUrl}/me`);
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>(`${this.baseUrl}/me`, request);
  }
}
