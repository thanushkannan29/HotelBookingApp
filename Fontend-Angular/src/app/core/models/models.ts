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
  state: string;
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

// ─── AMENITY ──────────────────────────────────────────────────────────────────
// F8A: New interface
export interface AmenityResponseDto {
  amenityId: string;
  name: string;
  category: string;
  iconName?: string;
  isActive: boolean;
}

export interface CreateAmenityDto {
  name: string;
  category: string;
  iconName?: string;
}

export interface UpdateAmenityDto {
  amenityId: string;
  name: string;
  category: string;
  iconName?: string;
  isActive: boolean;
}

export interface PagedAmenityResponseDto {
  totalCount: number;
  amenities: AmenityResponseDto[];
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
  state: string;
  description: string;
  imageUrl: string;
  contactNumber: string;
  upiId?: string;
  averageRating: number;
  reviewCount: number;
  gstPercent: number;
  amenities: string[];
  reviews: ReviewDto[];
  roomTypes: RoomTypePublicDto[];
}

export interface ReviewDto {
  userName: string;
  userProfileImageUrl?: string; // guest profile pic
  rating: number;
  comment: string;
  imageUrl?: string;
  adminReply?: string; // hotel admin reply
  createdDate: string;
}

export interface AmenityPublicDto {
  amenityId: string;
  name: string;
  category: string;
  iconName?: string;
}

// F8B: Added imageUrl + amenityList
export interface RoomTypePublicDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenities: string[];
  amenityList: AmenityPublicDto[];
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
  city?: string;
  state?: string;
  checkIn: string;
  checkOut: string;
  pageNumber: number;
  pageSize: number;
  amenityIds?: string[];
  minPrice?: number;
  maxPrice?: number;
  roomType?: string;
  sortBy?: string; // 'price_asc' | 'price_desc' | 'rating'
}

export interface SearchHotelResponseDto {
  hotels: HotelListItemDto[];
  pageNumber: number;
  recordsCount: number;
  totalCount?: number;
}

// ─── HOTEL ADMIN ──────────────────────────────────────────────────────────────
// F8B: Added upiId
export interface UpdateHotelDto {
  name: string;
  address: string;
  city: string;
  state: string;
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
// F8B: Added imageUrl, amenityIds replaces free-text amenities
export interface CreateRoomTypeDto {
  name: string;
  description: string;
  maxOccupancy: number;
  amenityIds: string[];
  imageUrl?: string;
}

// F8B: Added imageUrl, amenityIds replaces free-text amenities
export interface UpdateRoomTypeDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenityIds: string[];
  imageUrl?: string;
}

// F8B: Added imageUrl
export interface RoomTypeListDto {
  roomTypeId: string;
  name: string;
  description: string;
  maxOccupancy: number;
  amenityList?: { amenityId: string; name: string; iconName?: string }[];
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
  promoCodeUsed?: string;
  walletAmountToUse?: number;
  payCancellationFee?: boolean;
}

export interface ReservationResponseDto {
  reservationCode: string;
  reservationId: string;
  totalAmount: number;
  gstPercent: number;
  gstAmount: number;
  discountPercent: number;
  discountAmount: number;
  walletAmountUsed: number;
  finalAmount: number;
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
  gstPercent: number;
  gstAmount: number;
  discountPercent: number;
  discountAmount: number;
  walletAmountUsed: number;
  finalAmount: number;
  promoCodeUsed?: string;
  status: string;
  isCheckedIn: boolean;
  createdDate: string;
  expiryTime?: string;
  upiId?: string;
  rooms: RoomSummaryDto[];
  cancellationFeePaid: boolean;
  cancellationFeeAmount: number;
  cancellationPolicyText: string;
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
  reservationCode: string;
  hotelName: string;
  guestName: string;
  amount: number;
  paymentMethod: number;
  status: number;
  transactionDate: string;
  transactionType: string; // 'Payment' | 'WalletRefund' | 'AutoRefund' | 'CommissionSent'
  description?: string;
}

export interface PagedTransactionResponseDto {
  totalCount: number;
  transactions: TransactionResponseDto[];
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
  userName: string;
  userProfileImageUrl?: string;
  reservationId: string;
  reservationCode: string;
  rating: number;
  comment: string;
  imageUrl?: string;
  adminReply?: string;
  createdDate: string;
  contributionPoints: number;
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
  contributionPoints: number;
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
  minRating?: number;
  maxRating?: number;
  sortDir?: string; // 'asc' | 'desc' | undefined (newest first)
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
  totalReviewPoints: number;
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

export interface PaginationDto {
  page: number;
  pageSize: number;
}

// ─── DASHBOARD ────────────────────────────────────────────────────────────────
export interface AdminDashboardDto {
  hotelId: string;
  hotelName: string;
  hotelImageUrl?: string;
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
}

export interface GuestDashboardDto {
  totalBookings: number;
  activeBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  totalSpent: number;
}

export interface SuperAdminDashboardDto {
  totalHotels: number;
  activeHotels: number;
  blockedHotels: number;
  totalUsers: number;
  totalReservations: number;
  totalRevenue: number;
  totalReviews: number;
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

// ─── WALLET ───────────────────────────────────────────────────────────────────
export interface WalletResponseDto {
  walletId: string;
  balance: number;
  updatedAt: string;
}

export interface WalletTransactionDto {
  walletTransactionId: string;
  amount: number;
  type: 'Credit' | 'Debit';
  description: string;
  createdAt: string;
}

export interface PagedWalletTransactionDto {
  totalCount: number;
  wallet: WalletResponseDto;
  transactions: WalletTransactionDto[];
}

export interface TopUpWalletDto {
  amount: number;
}

// ─── PROMO CODE ───────────────────────────────────────────────────────────────
export interface PromoCodeResponseDto {
  promoCodeId: string;
  code: string;
  hotelName: string;
  hotelId: string;
  discountPercent: number;
  expiryDate: string;
  isUsed: boolean;
  status: 'Active' | 'Used' | 'Expired';
}

export interface PagedPromoCodeResponseDto {
  totalCount: number;
  promoCodes: PromoCodeResponseDto[];
}

export interface ValidatePromoCodeDto {
  code: string;
  hotelId: string;
  totalAmount: number;
}

export interface PromoCodeValidationResultDto {
  isValid: boolean;
  discountPercent: number;
  discountAmount: number;
  message: string;
}

// ─── AMENITY REQUEST ──────────────────────────────────────────────────────────
export interface CreateAmenityRequestDto {
  amenityName: string;
  category: string;
  iconName?: string;
}

export interface AmenityRequestResponseDto {
  amenityRequestId: string;
  amenityName: string;
  category: string;
  iconName?: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  superAdminNote?: string;
  adminName: string;
  hotelName: string;
  createdAt: string;
  processedAt?: string;
}

export interface PagedAmenityRequestResponseDto {
  totalCount: number;
  requests: AmenityRequestResponseDto[];
}

// ─── SUPER ADMIN REVENUE ──────────────────────────────────────────────────────
export interface SuperAdminRevenueDto {
  superAdminRevenueId: string;
  reservationCode: string;
  hotelName: string;
  reservationAmount: number;
  commissionAmount: number;
  superAdminUpiId: string;
  createdAt: string;
}

export interface PagedRevenueResponseDto {
  totalCount: number;
  items: SuperAdminRevenueDto[];
}

export interface RevenueSummaryDto {
  totalCommissionEarned: number;
}

// ─── QR PAYMENT ───────────────────────────────────────────────────────────────
export interface QrPaymentResponseDto {
  upiId: string;
  amount: number;
  qrCodeBase64: string;
  hotelName: string;
}

// ─── SUPPORT REQUEST ──────────────────────────────────────────────────────────
export interface PublicSupportRequestDto {
  name: string;
  email: string;
  subject: string;
  message: string;
  category: string;
}

export interface GuestSupportRequestDto {
  subject: string;
  message: string;
  category: string;
  reservationCode?: string;
  hotelId?: string;
}

export interface AdminSupportRequestDto {
  subject: string;
  message: string;
  category: string;
}

export interface SupportRequestResponseDto {
  supportRequestId: string;
  subject: string;
  message: string;
  category: string;
  status: 'Open' | 'InProgress' | 'Resolved';
  adminResponse?: string;
  submitterRole: string;
  submitterName: string;
  submitterEmail: string;
  reservationCode?: string;
  hotelName?: string;
  createdAt: string;
  respondedAt?: string;
}

export interface PagedSupportRequestResponseDto {
  totalCount: number;
  requests: SupportRequestResponseDto[];
}

export interface RespondSupportRequestDto {
  response: string;
  status: 'InProgress' | 'Resolved';
}

// ─── ROOM TYPE RATE ───────────────────────────────────────────────────────────
export interface RoomTypeRateDto {
  roomTypeRateId: string;
  roomTypeId: string;
  startDate: string;
  endDate: string;
  rate: number;
}
