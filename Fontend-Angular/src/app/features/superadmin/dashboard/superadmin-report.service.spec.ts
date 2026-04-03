import { TestBed } from '@angular/core/testing';
import { SuperadminReportService } from './superadmin-report.service';
import { SuperAdminDashboardDto } from '../../../core/models/models';

const MOCK: SuperAdminDashboardDto = {
  totalHotels: 20, activeHotels: 15, blockedHotels: 2,
  totalUsers: 500, totalReservations: 1200, totalRevenue: 9500000, totalReviews: 350,
};

describe('SuperadminReportService', () => {
  let service: SuperadminReportService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SuperadminReportService);
  });

  it('should be created', () => expect(service).toBeTruthy());

  it('should be a singleton', () => {
    expect(TestBed.inject(SuperadminReportService)).toBe(TestBed.inject(SuperadminReportService));
  });

  describe('downloadReport', () => {
    it('should not throw with normal data', () =>
      expect(() => service.downloadReport(MOCK, 190000)).not.toThrow());

    it('should not throw when totalRevenue is 0', () =>
      expect(() => service.downloadReport({ ...MOCK, totalRevenue: 0 }, 0)).not.toThrow());

    it('should not throw when all hotel counts are 0', () =>
      expect(() => service.downloadReport({ ...MOCK, totalHotels: 0, activeHotels: 0, blockedHotels: 0 }, 0)).not.toThrow());

    it('should not throw when commission is 0', () =>
      expect(() => service.downloadReport(MOCK, 0)).not.toThrow());

    it('should not throw with large commission value', () =>
      expect(() => service.downloadReport(MOCK, 9999999)).not.toThrow());

    it('should not throw when all hotels are blocked', () =>
      expect(() => service.downloadReport({ ...MOCK, activeHotels: 0, blockedHotels: 20 }, 0)).not.toThrow());

    it('should not throw when all hotels are active', () =>
      expect(() => service.downloadReport({ ...MOCK, activeHotels: 20, blockedHotels: 0 }, 0)).not.toThrow());

    it('should not throw when all reservations are 0', () =>
      expect(() => service.downloadReport({ ...MOCK, totalReservations: 0 }, 0)).not.toThrow());

    it('should not throw with zero users', () =>
      expect(() => service.downloadReport({ ...MOCK, totalUsers: 0 }, 0)).not.toThrow());

    it('should not throw with zero reviews', () =>
      expect(() => service.downloadReport({ ...MOCK, totalReviews: 0 }, 0)).not.toThrow());

    it('should not throw with single hotel', () =>
      expect(() => service.downloadReport({ ...MOCK, totalHotels: 1, activeHotels: 1, blockedHotels: 0 }, 100)).not.toThrow());

    it('should not throw with maximum commission equal to revenue', () =>
      expect(() => service.downloadReport(MOCK, MOCK.totalRevenue)).not.toThrow());
  });
});
