import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject, Injector } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Resolves AuthService inside intercept() so HttpClient construction does not
 * depend on AuthService (avoids AuthService → HttpClient → interceptor → AuthService deadlock).
 */
@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private readonly injector = inject(Injector);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.injector.get(AuthService).getToken();

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request);
  }
}
