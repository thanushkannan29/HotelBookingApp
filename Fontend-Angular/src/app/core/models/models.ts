// ─── AUTH ─────────────────────────────────────────────────────────────────────
export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterUserDto {
  name: string;
  email: string;
  password: string;
}

export interface RegisterHotelAdminDto {
  name: string;
  email: string;
  password: string;
  hotelName: string;
  address: string;
  city: string;
  description: string;
  contactNumber: string;
}

export interface AuthResponseDto {
  token: string;
}

export interface JwtPayload {
  nameid: string;
  unique_name: string;
  role: string;
  HotelId?: string;
  exp: number;
}

export type UserRole = 'Guest' | 'Admin' | 'SuperAdmin';

export interface CurrentUser {
  userId: string;
  userName: string;
  role: UserRole;
  hotelId?: string;
}

// ─── CITY ─────────────────────────────────────────────────────────────────────
// F8A: New interface
export interface IndianCityDto {
  cityName: string;
  stateName: string;
  samplePin: string;
}

// ─── AMENITY ──────────────────────────────────────────────────────────────────
// F8A: New interface
export interface AmenityResponseDto {
  amenityId: string;
  name: string;
  category: string;
  iconName?: string;
  isActive: boolean;
}

// ─── HOTEL PUBLIC ─────────────────────────────────────────────────────────────
export interface HotelListItemDto {
  hotelId: string;
  name: string;
  city: string;
  imageUrl: string;
  averageRating: number;
  reviewCount: number;
  startingPrice?: number;
}

export interface HotelDetailsDto {
  hotelId: string;
  name: string;
  address: string;
  city: string;
  description: string;
  imageUrl: string;
  contactNumber: string;
  averageRating: number;
  reviewCount: number;
  amenities: string[];
  reviews: ReviewDto[];
  roomTypes: RoomTypePublicDto[];
}

export interface ReviewDto {
  userName: string;
  rating: number;
  comment: string;
  imageUrl?: string;
  createdDate: string;
}

// F8B: Added imageUrl
export interface RoomTypePublicDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenities: string[];
  imageUrl?: string;
}

// F8B: Added imageUrl
export interface RoomAvailabilityDto {
  roomTypeId: string;
  roomTypeName: string;
  pricePerNight: number;
  availableRooms: number;
  imageUrl?: string;
}

export interface SearchHotelRequestDto {
  city: string;
  checkIn: string;
  checkOut: string;
  pageNumber: number;
  pageSize: number;
}

export interface SearchHotelResponseDto {
  hotels: HotelListItemDto[];
  pageNumber: number;
  recordsCount: number;
}

// ─── HOTEL ADMIN ──────────────────────────────────────────────────────────────
// F8B: Added upiId
export interface UpdateHotelDto {
  name: string;
  address: string;
  city: string;
  description: string;
  contactNumber: string;
  imageUrl: string;
  upiId?: string;
}

// ─── HOTEL SUPERADMIN ─────────────────────────────────────────────────────────
export interface SuperAdminHotelListDto {
  hotelId: string;
  name: string;
  city: string;
  contactNumber: string;
  isActive: boolean;
  isBlockedBySuperAdmin: boolean;
  createdAt: string;
  totalReservations: number;
  totalRevenue: number;
}

export interface PagedSuperAdminHotelResponseDto {
  totalCount: number;
  hotels: SuperAdminHotelListDto[];
}

// ─── ROOM TYPE ────────────────────────────────────────────────────────────────
// F8B: Added imageUrl
export interface CreateRoomTypeDto {
  name: string;
  description: string;
  maxOccupancy: number;
  amenities: string;
  imageUrl?: string;
}

// F8B: Added imageUrl
export interface UpdateRoomTypeDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenities: string;
  imageUrl?: string;
}

// F8B: Added imageUrl
export interface RoomTypeListDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenities: string;
  isActive: boolean;
  roomCount: number;
  imageUrl?: string;
}

export interface CreateRoomTypeRateDto {
  roomTypeId: string;
  startDate: string;
  endDate: string;
  rate: number;
}

export interface UpdateRoomTypeRateDto {
  roomTypeRateId: string;
  startDate: string;
  endDate: string;
  rate: number;
}

export interface GetRateByDateRequestDto {
  roomTypeId: string;
  date: string;
}

// ─── ROOM ─────────────────────────────────────────────────────────────────────
export interface CreateRoomDto {
  roomNumber: string;
  floor: number;
  roomTypeId: string;
}

export interface UpdateRoomDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeId: string;
}

export interface RoomListResponseDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeId: string;
  roomTypeName: string;
  isActive: boolean;
}

// F8A: New interface
export interface RoomOccupancyDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeName: string;
  isOccupied: boolean;
  reservationCode?: string;
}

// ─── INVENTORY ────────────────────────────────────────────────────────────────
export interface CreateInventoryDto {
  roomTypeId: string;
  startDate: string;
  endDate: string;
  totalInventory: number;
}

export interface UpdateInventoryDto {
  roomTypeInventoryId: string;
  totalInventory: number;
}

export interface InventoryResponseDto {
  roomTypeInventoryId: string;
  date: string;
  totalInventory: number;
  reservedInventory: number;
  available: number;
}

// ─── RESERVATION ──────────────────────────────────────────────────────────────
export interface CreateReservationDto {
  hotelId: string;
  roomTypeId: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfRooms: number;
  selectedRoomIds?: string[];
}

export interface ReservationResponseDto {
  reservationCode: string;
  reservationId: string;
  totalAmount: number;
  status: string;
  totalRooms: number;
  rooms: RoomSummaryDto[];
}

export interface RoomSummaryDto {
  roomId: string;
  roomNumber: string;
  floor: number;
}

export interface ReservationDetailsDto {
  reservationCode: string;
  reservationId: string;
  hotelId: string;
  hotelName: string;
  roomTypeId: string;
  roomTypeName: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfRooms: number;
  totalAmount: number;
  status: string;
  isCheckedIn: boolean;
  createdDate: string;
  rooms: RoomSummaryDto[];
}

export interface PagedReservationResponseDto {
  totalCount: number;
  reservations: ReservationDetailsDto[];
}

export interface CancelReservationDto {
  reason: string;
}

export interface AvailableRoomDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeName: string;
}

// ─── TRANSACTION ──────────────────────────────────────────────────────────────
export interface CreatePaymentDto {
  reservationId: string;
  paymentMethod: number;
}

export interface RefundRequestDto {
  reason: string;
}

export interface TransactionResponseDto {
  transactionId: string;
  reservationId: string;
  amount: number;
  paymentMethod: number;
  status: number;
  transactionDate: string;
}

export interface PagedTransactionResponseDto {
  totalCount: number;
  transactions: TransactionResponseDto[];
}

// F8A: New interface
export interface PaymentIntentDto {
  upiId?: string;
  amount: number;
  paymentRef: string;
  hotelName: string;
}

export const PaymentMethod: Record<number, string> = {
  1: 'Credit Card',
  2: 'Debit Card',
  3: 'UPI',
  4: 'Net Banking',
  5: 'Wallet',
};

export const PaymentStatus: Record<number, string> = {
  1: 'Pending',
  2: 'Success',
  3: 'Failed',
  4: 'Refunded',
};

// ─── REVIEW ───────────────────────────────────────────────────────────────────
// F8B: Added reservationId
export interface CreateReviewDto {
  hotelId: string;
  reservationId: string;
  rating: number;
  comment: string;
  imageUrl?: string;
}

export interface UpdateReviewDto {
  rating: number;
  comment?: string;
  imageUrl?: string;
}

export interface ReviewResponseDto {
  reviewId: string;
  hotelId: string;
  userId: string;
  reservationId: string;
  reservationCode: string;
  rating: number;
  comment: string;
  imageUrl?: string;
  createdDate: string;
}

// F8B: Added reservationId and reservationCode
export interface MyReviewsResponseDto {
  reviewId: string;
  hotelId: string;
  hotelName: string;
  reservationId: string;
  reservationCode: string;
  rating: number;
  comment: string;
  imageUrl?: string;
  createdDate: string;
}

export interface PagedReviewResponseDto {
  totalCount: number;
  reviews: ReviewResponseDto[];
}

export interface PagedMyReviewsResponseDto {
  totalCount: number;
  reviews: MyReviewsResponseDto[];
}

export interface GetHotelReviewsRequestDto {
  hotelId: string;
  page: number;
  pageSize: number;
}

// ─── REFUND REQUEST ───────────────────────────────────────────────────────────
// F8B: Added refundPaymentMethod, refundTransactionRef
export interface RefundRequestResponseDto {
  refundRequestId: string;
  reservationId: string;
  reservationCode: string;
  userId: string;
  guestName: string;
  reason: string;
  status: string;
  adminResponse?: string;
  refundAmount: number;
  refundPaymentMethod?: string;
  refundTransactionRef?: string;
  createdAt: string;
  processedAt?: string;
}

// F8B: Added refundPaymentMethod, refundTransactionRef
export interface ProcessRefundDto {
  adminResponse: string;
  refundPaymentMethod?: string;
  refundTransactionRef?: string;
}

export interface PagedRefundRequestResponseDto {
  totalCount: number;
  refundRequests: RefundRequestResponseDto[];
}

// ─── USER PROFILE ─────────────────────────────────────────────────────────────
export interface UserProfileResponseDto {
  userId: string;
  email: string;
  role: string;
  name: string;
  phoneNumber: string;
  address: string;
  state: string;
  city: string;
  pincode: string;
  profileImageUrl?: string;
  createdAt: string;
}

export interface UpdateUserProfileDto {
  name?: string;
  phoneNumber?: string;
  address?: string;
  state?: string;
  city?: string;
  pincode?: string;
  profileImageUrl?: string;
}

export interface BookingHistoryDto {
  reservationId: string;
  reservationCode: string;
  hotelName: string;
  checkInDate: string;
  checkOutDate: string;
  totalAmount: number;
  status: string;
  createdDate: string;
}

export interface PagedBookingHistoryDto {
  totalCount: number;
  bookings: BookingHistoryDto[];
}

export interface PaginationDto {
  page: number;
  pageSize: number;
}

// ─── DASHBOARD ────────────────────────────────────────────────────────────────
export interface AdminDashboardDto {
  hotelId: string;
  hotelName: string;
  isActive: boolean;
  isBlockedBySuperAdmin: boolean;
  totalRooms: number;
  activeRooms: number;
  totalRoomTypes: number;
  totalReservations: number;
  pendingReservations: number;
  activeReservations: number;
  completedReservations: number;
  cancelledReservations: number;
  totalRevenue: number;
  totalReviews: number;
  averageRating: number;
  pendingRefundRequests: number;
}

export interface GuestDashboardDto {
  totalBookings: number;
  activeBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  totalSpent: number;
  pendingRefunds: number;
}

export interface SuperAdminDashboardDto {
  totalHotels: number;
  activeHotels: number;
  blockedHotels: number;
  totalUsers: number;
  totalReservations: number;
  totalRevenue: number;
  totalReviews: number;
  pendingRefundRequests: number;
}

// ─── AUDIT LOG ────────────────────────────────────────────────────────────────
export interface AuditLogResponseDto {
  auditLogId: string;
  userId?: string;
  action: string;
  entityName: string;
  entityId?: string;
  changes: string;
  createdAt: string;
}

export interface PagedAuditLogResponseDto {
  totalCount: number;
  logs: AuditLogResponseDto[];
}

// ─── LOG ──────────────────────────────────────────────────────────────────────
export interface LogResponseDto {
  logId: string;
  message: string;
  exceptionType: string;
  stackTrace: string;
  statusCode: number;
  userName: string;
  role: string;
  userId?: string;
  controller: string;
  action: string;
  httpMethod: string;
  requestPath: string;
  createdAt: string;
}

export interface PagedLogResponseDto {
  totalCount: number;
  logs: LogResponseDto[];
}

// ─── API RESPONSE WRAPPER ─────────────────────────────────────────────────────
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  statusCode?: number;
}