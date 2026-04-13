export interface UserProfileResponse {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
}

export interface UpdateProfileRequest {
  firstName: string | null;
  lastName: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
}
