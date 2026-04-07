import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import {
  provideHttpClient,
  withInterceptors,
  HttpClient
} from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { loadingInterceptor } from './loading.interceptor';
import { LoadingService } from '../services/loading.service';

const LOCAL_URL    = 'http://localhost:5000/api/data';
const EXTERNAL_URL = 'https://generativelanguage.googleapis.com/v1/models';

describe('loadingInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let loadingSpy: jasmine.SpyObj<LoadingService>;

  beforeEach(() => {
    loadingSpy = jasmine.createSpyObj('LoadingService', ['show', 'hide']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: LoadingService, useValue: loadingSpy },
      ]
    });

    http     = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── Local URL ─────────────────────────────────────────────────────────────

  it('should call show() before request to localhost', () => {
    http.get(LOCAL_URL).subscribe();
    httpMock.expectOne(LOCAL_URL).flush({});
    expect(loadingSpy.show).toHaveBeenCalledTimes(1);
  });

  it('should call hide() after request to localhost completes', () => {
    http.get(LOCAL_URL).subscribe();
    httpMock.expectOne(LOCAL_URL).flush({});
    expect(loadingSpy.hide).toHaveBeenCalledTimes(1);
  });

  it('should call hide() even when request to localhost errors', () => {
    http.get(LOCAL_URL).subscribe({ error: () => {} });
    httpMock.expectOne(LOCAL_URL).flush(null, { status: 500, statusText: 'Error' });
    expect(loadingSpy.hide).toHaveBeenCalledTimes(1);
  });

  // ── External URL bypass ───────────────────────────────────────────────────

  it('should NOT call show() for external URLs', () => {
    http.get(EXTERNAL_URL).subscribe();
    httpMock.expectOne(EXTERNAL_URL).flush({});
    expect(loadingSpy.show).not.toHaveBeenCalled();
  });

  it('should NOT call hide() for external URLs', () => {
    http.get(EXTERNAL_URL).subscribe();
    httpMock.expectOne(EXTERNAL_URL).flush({});
    expect(loadingSpy.hide).not.toHaveBeenCalled();
  });

  // ── 127.0.0.1 ─────────────────────────────────────────────────────────────

  it('should call show() for 127.0.0.1 URLs', () => {
    const url = 'http://127.0.0.1:5000/api/test';
    http.get(url).subscribe();
    httpMock.expectOne(url).flush({});
    expect(loadingSpy.show).toHaveBeenCalledTimes(1);
  });

  it('should call hide() for 127.0.0.1 URLs after completion', () => {
    const url = 'http://127.0.0.1:5000/api/test';
    http.get(url).subscribe();
    httpMock.expectOne(url).flush({});
    expect(loadingSpy.hide).toHaveBeenCalledTimes(1);
  });
});
