import { TestBed } from '@angular/core/testing';
import { ReportService } from './report.service';
import { AdminDashboardDto } from '../../../core/models/models';

const MOCK: AdminDashboardDto = {
  hotelId: 'h-001', hotelName: 'Grand Hotel', isActive: true,
  isBlockedBySuperAdmin: false, totalRooms: 50, activeRooms: 45,
  totalRoomTypes: 3, totalReservations: 120, pendingReservations: 10,
  activeReservations: 30, completedReservations: 70, cancelledReservations: 10,
  totalRevenue: 500000, averageRating: 4.2, totalReviews: 80,
};

describe('ReportService', () => {
  let service: ReportService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ReportService);
  });

  it('should be created', () => expect(service).toBeTruthy());

  it('should be a singleton', () => {
    expect(TestBed.inject(ReportService)).toBe(TestBed.inject(ReportService));
  });

  describe('downloadReport', () => {
    it('should not throw for a normal active hotel', () =>
      expect(() => service.downloadReport(MOCK)).not.toThrow());

    it('should not throw when hotel is blocked', () =>
      expect(() => service.downloadReport({ ...MOCK, isBlockedBySuperAdmin: true })).not.toThrow());

    it('should not throw when hotel is inactive', () =>
      expect(() => service.downloadReport({ ...MOCK, isActive: false })).not.toThrow());

    it('should not throw when totalReservations is 0', () =>
      expect(() => service.downloadReport({
        ...MOCK, totalReservations: 0, pendingReservations: 0,
        activeReservations: 0, completedReservations: 0, cancelledReservations: 0,
      })).not.toThrow());

    it('should not throw when averageRating is 0', () =>
      expect(() => service.downloadReport({ ...MOCK, averageRating: 0, totalReviews: 0 })).not.toThrow());

    it('should not throw when averageRating is 5.0', () =>
      expect(() => service.downloadReport({ ...MOCK, averageRating: 5.0 })).not.toThrow());

    it('should not throw with large revenue value', () =>
      expect(() => service.downloadReport({ ...MOCK, totalRevenue: 99999999 })).not.toThrow());

    it('should not throw when all rooms are inactive', () =>
      expect(() => service.downloadReport({ ...MOCK, activeRooms: 0 })).not.toThrow());

    it('should not throw with zero reviews', () =>
      expect(() => service.downloadReport({ ...MOCK, totalReviews: 0 })).not.toThrow());

    it('should not throw with single-character hotel name', () =>
      expect(() => service.downloadReport({ ...MOCK, hotelName: 'A' })).not.toThrow());

    it('should not throw with hotel name containing spaces', () =>
      expect(() => service.downloadReport({ ...MOCK, hotelName: 'My Grand Hotel' })).not.toThrow());

    it('should not throw with blocked and inactive hotel', () =>
      expect(() => service.downloadReport({ ...MOCK, isActive: false, isBlockedBySuperAdmin: true })).not.toThrow());
  });
});
