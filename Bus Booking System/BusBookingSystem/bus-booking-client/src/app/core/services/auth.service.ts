import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse, User } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly userStorageKey = 'user';
  private readonly tokenStorageKey = 'token';
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly operatorApiUrl = `${environment.apiUrl}/operator`;
  private readonly adminApiUrl = `${environment.apiUrl}/admin`;
  private readonly currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    if (isPlatformBrowser(this.platformId)) {
      this.currentUserSubject.next(this.readStoredUser());
    }
  }

  loginUser(identifier: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { identifier, password }).pipe(
      tap((res) => this.persistSession(res))
    );
  }

  loginOperator(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.operatorApiUrl}/login`, { email, password }).pipe(
      tap((res) => this.persistSession(res))
    );
  }

  loginAdmin(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.adminApiUrl}/login`, { email, password }).pipe(
      tap((res) => this.persistSession(res))
    );
  }

  register(userData: {
    fullName: string;
    email: string;
    userName?: string | null;
    phoneNumber: string;
    password: string;
    confirmPassword: string;
  }): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  registerOperator(operatorData: {
    companyName: string;
    contactPersonName: string;
    email: string;
    phoneNumber: string;
    password: string;
    confirmPassword: string;
  }): Observable<unknown> {
    return this.http.post(`${this.operatorApiUrl}/register`, operatorData);
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/profile`).pipe(
      tap((profile) => this.currentUserSubject.next(profile))
    );
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  getRole(): string | null {
    return this.currentUserSubject.value?.role ?? this.readStoredUser()?.role ?? null;
  }

  hasRole(expected: string | string[]): boolean {
    const role = this.getRole();
    if (!role) return false;
    return Array.isArray(expected) ? expected.includes(role) : role === expected;
  }

  logout(): void {
    const s = this.storage();
    if (!s) return;
    s.removeItem(this.tokenStorageKey);
    s.removeItem(this.userStorageKey);
    this.currentUserSubject.next(null);
  }

  setToken(token: string): void {
    this.storage()?.setItem(this.tokenStorageKey, token);
  }

  getToken(): string | null {
    return this.storage()?.getItem(this.tokenStorageKey) ?? null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  private persistSession(response: LoginResponse): void {
    this.setToken(response.accessToken);
    const user: User = {
      id: response.userId,
      fullName: response.fullName,
      email: response.email,
      role: (response.role as User['role']) ?? 'User'
    };
    this.storage()?.setItem(this.userStorageKey, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private readStoredUser(): User | null {
    const raw = this.storage()?.getItem(this.userStorageKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }

  /** Safe on SSR / Vite server: no `localStorage` access outside the browser. */
  private storage(): Storage | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      return globalThis.localStorage;
    } catch {
      return null;
    }
  }
}
